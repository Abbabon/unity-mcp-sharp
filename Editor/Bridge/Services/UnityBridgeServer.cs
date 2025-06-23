using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Editor.Bridge.Models;
using Editor.Bridge.Tools;
using Editor.Utils;
using McpUnity.Resources;
using UnityEditor;
using WebSocketSharp.Server;

namespace Editor.Bridge.Services
{
    /// <summary>
    /// MCP Unity Server to communicate Node.js MCP server.
    /// Uses WebSockets to communicate with Node.js.
    /// </summary>
    [InitializeOnLoad]
    public class UnityBridgeServer
    {
        private static UnityBridgeServer _instance;
        
        private readonly Dictionary<string, McpToolBase> _tools = new Dictionary<string, McpToolBase>();
        private readonly Dictionary<string, McpResourceBase> _resources = new Dictionary<string, McpResourceBase>();
        
        private WebSocketServer _webSocketServer;
        private CancellationTokenSource _cts;
        private TestRunnerService _testRunnerService;
        private ConsoleLogsService _consoleLogsService;
        private Process _nodeProcess;

        /// <summary>
        /// Static constructor that gets called when Unity loads due to InitializeOnLoad attribute
        /// </summary>
        static UnityBridgeServer()
        {
            // Initialize the singleton instance when Unity loads
            // This ensures the bridge is available as soon as Unity starts
            EditorApplication.quitting += Instance.StopServer;

            // Auto-restart server after domain reload
            if (UnityMcpSharpSettings.Instance.AutoStartServer)
            {
                Instance.StartServer();
            }
        }
        
        /// <summary>
        /// Singleton instance accessor
        /// </summary>
        public static UnityBridgeServer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnityBridgeServer();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Current Listening state
        /// </summary>
        public bool IsListening => _webSocketServer?.IsListening ?? false;

        /// <summary>
        /// Dictionary of connected clients with this server
        /// </summary>
        public Dictionary<string, string> Clients { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private UnityBridgeServer()
        {
            InitializeServices();
            RegisterResources();
            RegisterTools();
        }
        
        /// <summary>
        /// Start the WebSocket Server to communicate with Node.js
        /// </summary>
        public void StartServer()
        {
            if (IsListening) return;

            try
            {
                // Create a new WebSocket server
                _webSocketServer = new WebSocketServer($"ws://0.0.0.0:{UnityMcpSharpSettings.Instance.Port}");
                // Add the MCP service endpoint with a handler that references this server
                _webSocketServer.AddWebSocketService("/McpUnity", () => new UnityBridgeSocketHandler(this));
                // Start the server
                _webSocketServer.Start();
                
                StartDockerServer();
                
                UnityMcpSharpLogger.LogInfo($"WebSocket server started on port {UnityMcpSharpSettings.Instance.Port}");
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to start WebSocket server: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        public void StopServer()
        {
            if (!IsListening) return;
            
            try
            {
                _webSocketServer?.Stop();
                StopDockerServer();
                UnityMcpSharpLogger.LogInfo("WebSocket server stopped");
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Error stopping WebSocket server: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Try to get a tool by name
        /// </summary>
        public bool TryGetTool(string name, out McpToolBase tool)
        {
            return _tools.TryGetValue(name, out tool);
        }
        
        /// <summary>
        /// Try to get a resource by name
        /// </summary>
        public bool TryGetResource(string name, out McpResourceBase resource)
        {
            return _resources.TryGetValue(name, out resource);
        }
        
        /// <summary>
        /// Build the Docker image and start the container if necessary.
        /// </summary>
        public void StartDockerServer()
        {
            var serverPath = McpUtils.GetServerPath();
            if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
            {
                UnityMcpSharpLogger.LogError($"Server path not found or invalid: {serverPath}. Cannot start Docker container.");
                return;
            }

            // McpUtils.BuildDockerImage(serverPath);
            McpUtils.StartDockerContainer(serverPath, McpUtils.DockerContainerName, UnityMcpSharpSettings.Instance.Port);
        }

        /// <summary>
        /// Stop the Docker container if it is running.
        /// </summary>
        public void StopDockerServer()
        {
            var serverPath = McpUtils.GetServerPath();
            McpUtils.StopDockerContainer(serverPath, McpUtils.DockerContainerName);
        }

        /// <summary>
        /// Start the Node.js server locally using the installed Node runtime.
        /// </summary>
        public void StartLocalNodeServer()
        {
            string serverPath = McpUtils.GetServerPath();
            if (string.IsNullOrEmpty(serverPath) || !Directory.Exists(serverPath))
            {
                UnityMcpSharpLogger.LogError($"Server path not found or invalid: {serverPath}. Cannot start Node.js server.");
                return;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "node",
                Arguments = Path.Combine(serverPath, "build", "index.js"),
                WorkingDirectory = serverPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                _nodeProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                _nodeProcess.OutputDataReceived += OnNodeProcessOutputDataReceived;
                _nodeProcess.ErrorDataReceived += OnNodeProcessErrorDataReceived;
                _nodeProcess.Exited += OnNodeProcessExited;

                if (!_nodeProcess.Start())
                {
                    UnityMcpSharpLogger.LogError("Failed to start Node.js process.");
                    return;
                }
                _nodeProcess.BeginOutputReadLine();
                _nodeProcess.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to start Node.js server: {ex.Message}");
            }
        }

        private void OnNodeProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                UnityMcpSharpLogger.LogInfo($"[Node.js] {e.Data}");   
            }
        }

        private void OnNodeProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                UnityMcpSharpLogger.LogError($"[Node.js] {e.Data}");   
            }
        }

        private void OnNodeProcessExited(object sender, EventArgs e)
        {
            if (_nodeProcess != null)
            {
                UnityMcpSharpLogger.LogInfo($"Node.js process exited with code {_nodeProcess.ExitCode}");
            }
        }

        [MenuItem("Tools/MCP Unity/Start Docker Server")]
        private static void MenuStartDockerServer()
        {
            Instance.StartDockerServer();
        }

        /// <summary>
        /// Stop the locally running Node.js server.
        /// </summary>
        public void StopLocalNodeServer()
        {
            if (_nodeProcess != null && !_nodeProcess.HasExited)
            {
                try
                {
                    _nodeProcess.Kill();
                    _nodeProcess.WaitForExit();
                }
                catch (Exception ex)
                {
                    UnityMcpSharpLogger.LogError($"Error stopping Node.js server: {ex.Message}");
                }
            }
            _nodeProcess = null;
        }

        [MenuItem("Tools/MCP Unity/Stop Docker Server")]
        private static void MenuStopDockerServer()
        {
            Instance.StopDockerServer();
        }
        
        /// <summary>
        /// Register all available tools
        /// </summary>
        private void RegisterTools()
        {
            //TODO: reflection: 
            // Register MenuItemTool
            var menuItemTool = new MenuItemTool();
            _tools.Add(menuItemTool.Name, menuItemTool);
            
            //
            // // Register SelectGameObjectTool
            // SelectGameObjectTool selectGameObjectTool = new SelectGameObjectTool();
            // _tools.Add(selectGameObjectTool.Name, selectGameObjectTool);
            //
            // // Register UpdateGameObjectTool
            // UpdateGameObjectTool updateGameObjectTool = new UpdateGameObjectTool();
            // _tools.Add(updateGameObjectTool.Name, updateGameObjectTool);
            //
            // // Register PackageManagerTool
            // AddPackageTool addPackageTool = new AddPackageTool();
            // _tools.Add(addPackageTool.Name, addPackageTool);
            //
            // // Register RunTestsTool
            // RunTestsTool runTestsTool = new RunTestsTool(_testRunnerService);
            // _tools.Add(runTestsTool.Name, runTestsTool);
            //
            // // Register SendConsoleLogTool
            // SendConsoleLogTool sendConsoleLogTool = new SendConsoleLogTool();
            // _tools.Add(sendConsoleLogTool.Name, sendConsoleLogTool);
            //
            // // Register UpdateComponentTool
            // UpdateComponentTool updateComponentTool = new UpdateComponentTool();
            // _tools.Add(updateComponentTool.Name, updateComponentTool);
            //
            // // Register AddAssetToSceneTool
            // AddAssetToSceneTool addAssetToSceneTool = new AddAssetToSceneTool();
            // _tools.Add(addAssetToSceneTool.Name, addAssetToSceneTool);
        }
        
        /// <summary>
        /// Register all available resources
        /// </summary>
        private void RegisterResources()
        {
            // Register GetMenuItemsResource
            var getMenuItemsResource = new GetMenuItemsResource();
            _resources.Add(getMenuItemsResource.Name, getMenuItemsResource);
            
            // // Register GetConsoleLogsResource
            // GetConsoleLogsResource getConsoleLogsResource = new GetConsoleLogsResource(_consoleLogsService);
            // _resources.Add(getConsoleLogsResource.Name, getConsoleLogsResource);
            //
            // // Register GetScenesHierarchyResource
            // GetScenesHierarchyResource getScenesHierarchyResource = new GetScenesHierarchyResource();
            // _resources.Add(getScenesHierarchyResource.Name, getScenesHierarchyResource);
            //
            // // Register GetPackagesResource
            // GetPackagesResource getPackagesResource = new GetPackagesResource();
            // _resources.Add(getPackagesResource.Name, getPackagesResource);
            //
            // // Register GetAssetsResource
            // GetAssetsResource getAssetsResource = new GetAssetsResource();
            // _resources.Add(getAssetsResource.Name, getAssetsResource);
            //
            // // Register GetTestsResource
            // GetTestsResource getTestsResource = new GetTestsResource(_testRunnerService);
            // _resources.Add(getTestsResource.Name, getTestsResource);
            //
            // // Register GetGameObjectResource
            // GetGameObjectResource getGameObjectResource = new GetGameObjectResource();
            // _resources.Add(getGameObjectResource.Name, getGameObjectResource);
        }
        
        /// <summary>
        /// Initialize services used by the server
        /// </summary>
        private void InitializeServices()
        {
            // Initialize the test runner service
            _testRunnerService = new TestRunnerService();
            
            // Initialize the console logs service
            _consoleLogsService = new ConsoleLogsService();
        }
    }
}
