using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityMCPSharp;
using UnityMCPSharp.Editor.Models;
using UnityMCPSharp.Editor.Handlers.System;
using UnityMCPSharp.Editor.Handlers.GameObjects;
using UnityMCPSharp.Editor.Handlers.Assets;
using UnityMCPSharp.Editor.Handlers.Scenes;

namespace UnityMCPSharp.Editor
{
    /// <summary>
    /// Unity Editor integration for MCP Server - handles requests from server and forwards Unity events
    /// </summary>
    [InitializeOnLoad]
    public static class MCPEditorIntegration
    {
        private static MCPServerManager _serverManager;
        private static MCPClient _client;
        private static MCPConfiguration _config;
        private static Queue<string> _consoleLogBuffer;
        private static bool _isCompiling;

        static MCPEditorIntegration()
        {
            EditorApplication.delayCall += Initialize;
            EditorApplication.quitting += Cleanup;

            // Clean up before assembly reload (recompilation)
            CompilationPipeline.compilationStarted += (obj) => CleanupBeforeReload();
        }

        private static void CleanupBeforeReload()
        {
            if (_client != null && _client.IsConnected)
            {
                Debug.Log("[MCPEditorIntegration] Disconnecting before assembly reload");
                _ = _client.DisconnectAsync();
            }
        }

        private static async void Initialize()
        {
            _config = MCPConfiguration.Instance;
            _consoleLogBuffer = new Queue<string>();

            // Initialize operation tracker
            MCPOperationTracker.Initialize();

            // Get singleton instance (shared with Dashboard and survives recompilation)
            _serverManager = MCPServerManager.GetInstance(_config);
            _client = _serverManager.GetClient();

            // Enable auto-reconnect with configured settings
            _client.AutoReconnect = true;

            // IMPORTANT: After domain reload, event delegates are cleared even though the client object survives
            // We need to disconnect and reconnect to ensure the new event subscriptions take effect
            bool wasConnected = _client.IsConnected;

            // Unsubscribe first to avoid duplicate subscriptions
            _client.OnNotificationReceived -= HandleNotification;
            _client.OnRequestReceived -= HandleRequest;
            _client.OnConnected -= OnConnected;

            // Subscribe to client events
            _client.OnNotificationReceived += HandleNotification;
            _client.OnRequestReceived += HandleRequest;
            _client.OnConnected += OnConnected;

            // If client was connected, disconnect and reconnect to activate new event subscriptions
            if (wasConnected)
            {
                Debug.Log("[MCPEditorIntegration] Reconnecting to activate event subscriptions");
                await _client.DisconnectAsync();
                await System.Threading.Tasks.Task.Delay(500);
            }

            // Subscribe to Unity events
            Application.logMessageReceived += OnLogMessageReceived;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorSceneManager.sceneLoaded += OnSceneLoaded;
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChanged;

            // Process main thread queue every frame
            EditorApplication.update -= ProcessMainThreadQueue;
            EditorApplication.update += ProcessMainThreadQueue;

            Debug.Log("[MCPEditorIntegration] Initialized");

            // Auto-start and auto-connect if configured
            bool serverReady = false;

            if (_config.autoStartContainer)
            {
                var isRunning = await _serverManager.IsContainerRunningAsync();
                if (!isRunning)
                {
                    Debug.Log("[MCPEditorIntegration] Auto-starting MCP server container...");
                    serverReady = await _serverManager.StartServerAsync();

                    if (!serverReady)
                    {
                        Debug.LogWarning("[MCPEditorIntegration] Auto-start failed. Server will not be available.");
                    }
                }
                else
                {
                    Debug.Log("[MCPEditorIntegration] Server container already running");
                    serverReady = true;
                }
            }
            else
            {
                // If auto-start is disabled, check if server is running before auto-connect
                serverReady = await _serverManager.IsContainerRunningAsync();
            }

            if (_config.autoConnect && serverReady)
            {
                Debug.Log("[MCPEditorIntegration] Auto-connecting to MCP server...");
                // Wait a bit for server to be ready
                await System.Threading.Tasks.Task.Delay(2000);

                // Try to connect with retry logic
                bool connected = false;
                for (int attempt = 1; attempt <= _config.retryAttempts; attempt++)
                {
                    connected = await _client.ConnectAsync();
                    if (connected)
                    {
                        Debug.Log($"[MCPEditorIntegration] Auto-connect succeeded on attempt {attempt}");
                        break;
                    }

                    if (attempt < _config.retryAttempts)
                    {
                        Debug.LogWarning($"[MCPEditorIntegration] Auto-connect attempt {attempt} failed, retrying in {_config.retryDelay} seconds...");
                        await System.Threading.Tasks.Task.Delay(_config.retryDelay * 1000);
                    }
                }

                if (!connected)
                {
                    Debug.LogError($"[MCPEditorIntegration] Auto-connect failed after {_config.retryAttempts} attempts. You can manually connect from the MCP Dashboard.");
                }
            }
            else if (_config.autoConnect && !serverReady)
            {
                Debug.LogWarning("[MCPEditorIntegration] Auto-connect skipped: server is not running. Enable auto-start container or start the server manually.");
            }
        }

        private static void Cleanup()
        {
            Debug.Log("[MCPEditorIntegration] Shutting down - cleaning up resources...");

            // Gracefully disconnect client before Unity closes
            if (_client != null && _client.IsConnected)
            {
                Debug.Log("[MCPEditorIntegration] Disconnecting from MCP server...");
                _ = _client.DisconnectAsync();
            }

            // Unsubscribe from events to prevent memory leaks
            if (_client != null)
            {
                _client.OnNotificationReceived -= HandleNotification;
                _client.OnRequestReceived -= HandleRequest;
                _client.OnConnected -= OnConnected;
            }

            Application.logMessageReceived -= OnLogMessageReceived;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorSceneManager.sceneLoaded -= OnSceneLoaded;
            EditorSceneManager.activeSceneChangedInEditMode -= OnActiveSceneChanged;
            EditorApplication.update -= ProcessMainThreadQueue;

            Debug.Log("[MCPEditorIntegration] Cleanup complete");
        }

        private static void ProcessMainThreadQueue()
        {
            // Process any queued callbacks from the WebSocket background thread
            _client?.ProcessMainThreadQueue();
        }

        private static void OnConnected()
        {
            Debug.Log("[MCPEditorIntegration] Connected to MCP server");

            // Send initial project info
            _ = SendProjectInfoAsync();
        }

        private static void HandleNotification(string method, object parameters)
        {
            Debug.Log($"[MCPEditorIntegration] Received notification: {method}");

            // Route to appropriate handler based on method name
            switch (method)
            {
                case "unity.triggerCompilation":
                    TriggerCompilationHandler.Handle(_config);
                    break;

                case "unity.runMenuItem":
                    RunMenuItemHandler.Handle(parameters, _config);
                    break;

                case "unity.createGameObject":
                    CreateGameObjectHandler.Handle(parameters, _config);
                    break;

                case "unity.createScript":
                    CreateScriptHandler.Handle(parameters, _config);
                    break;

                case "unity.addComponent":
                    AddComponentHandler.Handle(parameters, _config);
                    break;

                case "unity.enterPlayMode":
                    EnterPlayModeHandler.Handle(_config);
                    break;

                case "unity.exitPlayMode":
                    ExitPlayModeHandler.Handle(_config);
                    break;

                case "unity.refreshAssets":
                    RefreshAssetsHandler.Handle(_config);
                    break;

                case "unity.createAsset":
                    CreateAssetHandler.Handle(parameters, _config);
                    break;

                case "unity.batchCreateGameObjects":
                    BatchCreateGameObjectsHandler.Handle(parameters, _config);
                    break;

                case "unity.openScene":
                    OpenSceneHandler.Handle(parameters, _config);
                    break;

                case "unity.closeScene":
                    CloseSceneHandler.Handle(parameters, _config);
                    break;

                case "unity.setActiveScene":
                    SetActiveSceneHandler.Handle(parameters, _config);
                    break;

                case "unity.saveScene":
                    SaveSceneHandler.Handle(parameters, _config);
                    break;

                case "unity.createGameObjectInScene":
                    CreateGameObjectInSceneHandler.Handle(parameters, _config);
                    break;

                default:
                    Debug.LogWarning($"[MCPEditorIntegration] Unknown notification method: {method}");
                    break;
            }
        }

        private static void HandleRequest(string requestId, string method, object parameters)
        {
            if (_config.verboseLogging)
            {
                Debug.Log($"[MCPEditorIntegration] Received request {requestId}: {method}");
            }

            // Route to appropriate handler based on method name
            switch (method)
            {
                case "unity.getConsoleLogs":
                    GetConsoleLogsHandler.Handle(requestId, null, _client, _consoleLogBuffer, _config);
                    break;

                case "unity.getCompilationStatus":
                    GetCompilationStatusHandler.Handle(requestId, _client, _isCompiling, _config);
                    break;

                case "unity.listSceneObjects":
                    ListSceneObjectsHandler.Handle(requestId, null, _client, _config);
                    break;

                case "unity.getProjectInfo":
                    GetProjectInfoHandler.Handle(requestId, _client, _config);
                    break;

                case "unity.getPlayModeState":
                    GetPlayModeStateHandler.Handle(requestId, _client, _config);
                    break;

                case "unity.findGameObject":
                    FindGameObjectHandler.Handle(requestId, parameters, _client, _config);
                    break;

                case "unity.listScenes":
                    ListScenesHandler.Handle(requestId, _client, _config);
                    break;

                case "unity.getActiveScene":
                    GetActiveSceneHandler.Handle(requestId, _client, _config);
                    break;

                case "unity.setComponentField":
                    SetComponentFieldHandler.Handle(requestId, parameters, _client, _config);
                    break;

                default:
                    Debug.LogWarning($"[MCPEditorIntegration] Unknown request method: {method}");
                    break;
            }
        }

        private static async System.Threading.Tasks.Task SendProjectInfoAsync()
        {
            var info = new
            {
                projectName = Application.productName,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                activeScene = SceneManager.GetActiveScene().name,
                scenePath = SceneManager.GetActiveScene().path,
                dataPath = Application.dataPath,
                isPlaying = EditorApplication.isPlaying,
                isPaused = EditorApplication.isPaused
            };

            await _client.SendNotificationAsync("unity.projectInfo", info);
        }

        // Unity event handlers

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            var logEntry = $"[{type}] {condition}";

            // Add to buffer
            _consoleLogBuffer.Enqueue(logEntry);

            // Keep buffer size limited
            while (_consoleLogBuffer.Count > _config.maxLogBuffer)
            {
                _consoleLogBuffer.Dequeue();
            }

            // Send to server if connected (for real-time monitoring)
            if (_client.IsConnected)
            {
                _ = _client.SendNotificationAsync("unity.logMessage", new
                {
                    type = type.ToString(),
                    message = condition,
                    stackTrace = stackTrace
                });

                // Notify MCP that console logs resource has been updated
                _ = _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://console/logs"
                });
            }
        }

        private static void OnCompilationStarted(object obj)
        {
            _isCompiling = true;
            Debug.Log("[MCPEditorIntegration] Compilation started");

            if (_client.IsConnected)
            {
                _ = _client.SendNotificationAsync("unity.compilationStarted", null);
            }
        }

        private static void OnCompilationFinished(object obj)
        {
            _isCompiling = false;
            var succeeded = !EditorUtility.scriptCompilationFailed;

            Debug.Log($"[MCPEditorIntegration] Compilation finished: {(succeeded ? "Success" : "Failed")}");

            _ = HandleCompilationFinishedAsync(succeeded);
        }

        private static async System.Threading.Tasks.Task HandleCompilationFinishedAsync(bool succeeded)
        {
            // Try to reconnect if disconnected during compilation
            if (!_client.IsConnected && _config.autoConnect)
            {
                Debug.Log("[MCPEditorIntegration] Reconnecting after compilation...");
                var connected = await _client.ConnectAsync();
                if (!connected)
                {
                    Debug.LogWarning("[MCPEditorIntegration] Failed to reconnect after compilation");
                    return;
                }
            }

            // Send compilation finished notification
            if (_client.IsConnected)
            {
                await _client.SendNotificationAsync("unity.compilationFinished", new
                {
                    succeeded = succeeded
                });

                // Notify MCP that compilation status resource has been updated
                await _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://compilation/status"
                });
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Debug.Log($"[MCPEditorIntegration] Play mode state changed: {state}");

            // Notify that play mode resource has been updated
            if (_client != null && _client.IsConnected)
            {
                _ = _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://editor/playmode"
                });
            }
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[MCPEditorIntegration] Scene loaded: {scene.name} (mode: {mode})");

            // Notify that scene resources have been updated
            if (_client != null && _client.IsConnected)
            {
                _ = _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://scenes/active"
                });
                _ = _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://scenes/active/objects"
                });
            }
        }

        private static void OnActiveSceneChanged(Scene previousScene, Scene newScene)
        {
            Debug.Log($"[MCPEditorIntegration] Active scene changed: {previousScene.name} -> {newScene.name}");

            // Notify that scene resources have been updated
            if (_client != null && _client.IsConnected)
            {
                _ = _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://scenes/active"
                });
                _ = _client.SendNotificationAsync("unity.resourceUpdated", new
                {
                    uri = "unity://scenes/active/objects"
                });
            }
        }

        // Helper method used by ListSceneObjectsHandler
        public static void BuildHierarchy(Transform transform, List<SceneObject> sceneObjects, int depth)
        {
            sceneObjects.Add(new SceneObject
            {
                name = transform.name,
                isActive = transform.gameObject.activeInHierarchy,
                depth = depth
            });

            foreach (Transform child in transform)
            {
                BuildHierarchy(child, sceneObjects, depth + 1);
            }
        }
    }
}
