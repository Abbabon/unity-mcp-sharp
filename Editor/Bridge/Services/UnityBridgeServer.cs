using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Editor.Bridge.Models;
using Editor.Bridge.Tools;
using Editor.Utils;
using McpUnity.Resources;
using UnityEditor;
using WebSocketSharp.Server;
using UnityMCPSharp.Orchestrator;
using UnityMCPSharp.Orchestrator.Options;

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
        private TestRunnerService _testRunnerService;
        private ConsoleLogsService _consoleLogsService;
        
        private bool _isDockerServerRunning;
        private int _currentPort;

        /// <summary>
        /// Static constructor that gets called when Unity loads due to InitializeOnLoad attribute
        /// </summary>
        static UnityBridgeServer()
        {
            // Initialize the singleton instance when Unity loads
            // This ensures the bridge is available as soon as Unity starts
            EditorApplication.quitting += async () => await Instance.ShutdownAsync();
            
            // Auto-restart server after domain reload
            if (UnityMcpSharpSettings.Instance.AutoStartServer)
            {
                _ = Instance.StartServerAsync(); // Fire and forget
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
        /// Current Listening state of WebSocket server
        /// </summary>
        public bool IsWebSocketListening => _webSocketServer?.IsListening ?? false;
        
        /// <summary>
        /// Current running state of Docker server
        /// </summary>
        public bool IsDockerServerRunning => _isDockerServerRunning;

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
            _currentPort = UnityMcpSharpSettings.Instance.Port;
            _isDockerServerRunning = false;
        }
        
        /// <summary>
        /// Start the server components asynchronously
        /// </summary>
        public async Task StartServerAsync()
        {
            try
            {
                // Start the WebSocket server first (independent of Docker)
                await StartWebSocketServerAsync();
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to start WebSocket server: {ex.Message}");
                // Continue to try starting Docker even if WebSocket fails
            }

            try
            {
                // Start the Docker container server separately
                await StartDockerServerAsync();
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to start Docker server: {ex.Message}");
                // WebSocket server will remain running even if Docker fails
            }
        }
        
        /// <summary>
        /// Start the WebSocket server if it's not already running
        /// </summary>
        private async Task StartWebSocketServerAsync()
        {
            // If we already have a WebSocket server running on the correct port, don't restart it
            if (_webSocketServer != null && _webSocketServer.IsListening && _currentPort == UnityMcpSharpSettings.Instance.Port)
            {
                UnityMcpSharpLogger.LogInfo($"WebSocket server already running on port {_currentPort}");
                return;
            }
            
            // If we have a server but it's on the wrong port or not listening, stop it first
            if (_webSocketServer != null)
            {
                try
                {
                    _webSocketServer.Stop();
                }
                catch (Exception ex)
                {
                    UnityMcpSharpLogger.LogWarning($"Error stopping previous WebSocket server: {ex.Message}");
                }
            }

            // Create a new WebSocket server
            _currentPort = UnityMcpSharpSettings.Instance.Port;
            _webSocketServer = new WebSocketServer($"ws://0.0.0.0:{_currentPort}");
            
            // Add the MCP service endpoint with a handler that references this server
            _webSocketServer.AddWebSocketService("/McpUnity", () => new UnityBridgeSocketHandler(this));
            
            // Start the server
            _webSocketServer.Start();
            
            UnityMcpSharpLogger.LogInfo($"WebSocket server started on port {_currentPort}");

            // Give the WebSocket server a moment to initialize
            await Task.Delay(100);
        }
        
        /// <summary>
        /// Start the Docker server asynchronously
        /// </summary>
        private async Task StartDockerServerAsync()
        {
            if (_isDockerServerRunning)
            {
                UnityMcpSharpLogger.LogInfo("Docker server already running");
                return;
            }

            UnityMcpSharpLogger.LogInfo("Starting MCP server...");
            
            // Create options for starting the server
            var startOptions = new OrchestratorStartOptions(); //TODO: populate with settings
            
            // Start container in background task to avoid freezing the UI
            var result = await Task.Run(() => DockerContainerManager.RunStartAndReturnExitCode(startOptions));
            
            if (result.IsSuccess)
            {
                _isDockerServerRunning = true;
                UnityMcpSharpLogger.LogInfo($"MCP server started successfully on port {_currentPort}");
            }
            else
            {
                _isDockerServerRunning = false;
                UnityMcpSharpLogger.LogError($"Failed to start MCP server. Error: {result.ErrorMessage}");
            }
        }
        
        /// <summary>
        /// Shutdown all server components asynchronously
        /// </summary>
        public async Task ShutdownAsync()
        {
            var webSocketTask = StopWebSocketServerAsync();
            var dockerTask = StopDockerServerAsync();
            
            // Wait for both tasks to complete, but don't let one failure stop the other
            await Task.WhenAll(
                HandleTaskSafely(webSocketTask, "WebSocket server shutdown"), 
                HandleTaskSafely(dockerTask, "Docker server shutdown")
            );
        }
        
        /// <summary>
        /// Helper method to safely handle tasks that might throw exceptions
        /// </summary>
        private async Task HandleTaskSafely(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Error during {operationName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        private async Task StopWebSocketServerAsync()
        {
            if (!IsWebSocketListening) return;
            
            try
            {
                _webSocketServer?.Stop();
                UnityMcpSharpLogger.LogInfo("WebSocket server stopped");
                
                // Give WebSocket time to shut down properly
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Error stopping WebSocket server: {ex.Message}");
                throw; // Re-throw to be caught by HandleTaskSafely
            }
        }
        
        /// <summary>
        /// Stop the Docker server asynchronously
        /// </summary>
        private async Task StopDockerServerAsync()
        {
            if (!_isDockerServerRunning) return;
            
            UnityMcpSharpLogger.LogInfo("Stopping MCP server...");
            
            try
            {
                // Create options for stopping the server
                var stopOptions = new OrchestratorStopOptions();
                
                // Stop container in background task to avoid freezing the UI
                var result = await Task.Run(() => DockerContainerManager.RunStopAndReturnExitCode(stopOptions));
                
                if (result.IsSuccess)
                {
                    _isDockerServerRunning = false;
                    UnityMcpSharpLogger.LogInfo("MCP server stopped successfully");
                }
                else
                {
                    UnityMcpSharpLogger.LogError($"Failed to stop MCP server. Error: {result.ErrorMessage}");
                    throw new Exception(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Error stopping Docker server: {ex.Message}");
                throw; // Re-throw to be caught by HandleTaskSafely
            }
        }
        
        // Compatibility methods for existing menu items
        public async Task StartServer() => await StartServerAsync();
        public async Task StopServer() => await ShutdownAsync();
        
        [MenuItem("Tools/MCP Unity/Start Server")]
        private static void MenuStartServer()
        {
            _ = Instance.StartServerAsync();
        }

        [MenuItem("Tools/MCP Unity/Stop Server")]
        private static void MenuStopServer()
        {
            _ = Instance.ShutdownAsync();
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
        /// Register all available tools
        /// </summary>
        private void RegisterTools()
        {
            //TODO: reflection: 
            // Register MenuItemTool
            var menuItemTool = new MenuItemTool();
            _tools.Add(menuItemTool.Name, menuItemTool);
            
            // // Register other tools as needed
        }
        
        /// <summary>
        /// Register all available resources
        /// </summary>
        private void RegisterResources()
        {
            // Register GetMenuItemsResource
            var getMenuItemsResource = new GetMenuItemsResource();
            _resources.Add(getMenuItemsResource.Name, getMenuItemsResource);
            
            // // Register other resources as needed
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
