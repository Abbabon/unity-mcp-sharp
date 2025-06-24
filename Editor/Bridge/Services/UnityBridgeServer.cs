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
using UnityMCPSharp.Orchestrator.Models;
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

        /// <summary>
        /// Static constructor that gets called when Unity loads due to InitializeOnLoad attribute
        /// </summary>
        static UnityBridgeServer()
        {
            // // Initialize the singleton instance when Unity loads
            // // This ensures the bridge is available as soon as Unity starts
            // EditorApplication.quitting += Instance.StopServer;
            //
            // // Auto-restart server after domain reload
            // if (UnityMcpSharpSettings.Instance.AutoStartServer)
            // {
            //     Instance.StartServer();
            // }
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
        public async Task StartServer()
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

                UnityMcpSharpLogger.LogInfo($"WebSocket server started on port {UnityMcpSharpSettings.Instance.Port}");
                
                
                var startOptions = new OrchestratorStartOptions();
                var result = await Task.Run(() => DockerContainerManager.RunStartAndReturnExitCode(startOptions)); // OK if no Unity API is used
                if (result.IsSuccess)
                {
                    UnityMcpSharpLogger.LogInfo($"WebSocket server and MCP server started on port {UnityMcpSharpSettings.Instance.Port}");
                }
                else
                {
                    UnityMcpSharpLogger.LogError($"WebSocket server started but MCP server failed to start. Error: {result.ErrorMessage}");
                }
                
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to start WebSocket server: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Stop the WebSocket server
        /// </summary>
        public async Task StopServer()
        {
            if (!IsListening) return;
            
            try
            {
                _webSocketServer?.Stop();
                UnityMcpSharpLogger.LogInfo("WebSocket server stopped");
                
                var stopOptions = new OrchestratorStopOptions();
                var result = await Task.Run(() => DockerContainerManager.RunStopAndReturnExitCode(stopOptions));
                if (result.IsSuccess)
                {
                    UnityMcpSharpLogger.LogInfo("MCP server stopped successfully");
                }
                else
                {
                    UnityMcpSharpLogger.LogError($"Failed to stop MCP server. Error: {result.ErrorMessage}");
                }
                
                
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
