using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityMCPSharp;

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
            if (_config.verboseLogging)
            {
                Debug.Log($"[MCPEditorIntegration] Received notification: {method}");
            }

            // Route to appropriate handler based on method name
            switch (method)
            {
                case "unity.triggerCompilation":
                    HandleTriggerCompilation(parameters);
                    break;

                case "unity.createGameObject":
                    HandleCreateGameObject(parameters);
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
                    HandleGetConsoleLogs(requestId, parameters);
                    break;

                case "unity.getCompilationStatus":
                    HandleGetCompilationStatus(requestId);
                    break;

                case "unity.listSceneObjects":
                    HandleListSceneObjects(requestId, parameters);
                    break;

                case "unity.getProjectInfo":
                    HandleGetProjectInfo(requestId);
                    break;

                default:
                    Debug.LogWarning($"[MCPEditorIntegration] Unknown request method: {method}");
                    break;
            }
        }

        // Handlers for each MCP tool request

        private static void HandleGetConsoleLogs(string requestId, object parameters)
        {
            try
            {
                var logEntries = new List<LogEntry>();
                foreach (var log in _consoleLogBuffer)
                {
                    // Parse the log entry format: [Type] Message
                    var parts = log.Split(new[] { "] " }, 2, System.StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        var type = parts[0].TrimStart('[');
                        var message = parts[1];
                        logEntries.Add(new LogEntry { type = type, message = message });
                    }
                }

                var response = new { logs = logEntries };
                _ = _client.SendResponseAsync(requestId, response);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error in HandleGetConsoleLogs: {ex.Message}");
            }
        }

        [System.Serializable]
        private class LogEntry
        {
            [Newtonsoft.Json.JsonProperty("type")]
            public string type;
            [Newtonsoft.Json.JsonProperty("message")]
            public string message;
            [Newtonsoft.Json.JsonProperty("stackTrace")]
            public string stackTrace;
        }

        private static void HandleTriggerCompilation(object parameters)
        {
            EditorApplication.delayCall += () =>
            {
                Debug.Log("[MCPEditorIntegration] Triggering script compilation...");
                CompilationPipeline.RequestScriptCompilation();
            };
        }

        private static void HandleGetCompilationStatus(string requestId)
        {
            try
            {
                var response = new
                {
                    isCompiling = _isCompiling,
                    lastCompilationSucceeded = !EditorUtility.scriptCompilationFailed
                };

                _ = _client.SendResponseAsync(requestId, response);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error in HandleGetCompilationStatus: {ex.Message}");
            }
        }

        private static void HandleCreateGameObject(object parameters)
        {
            // Parse parameters (this is simplified - in production you'd use proper JSON deserialization)
            EditorApplication.delayCall += () =>
            {
                try
                {
                    // For now, create a simple GameObject with the received parameters
                    var json = JsonUtility.ToJson(parameters);
                    var data = JsonUtility.FromJson<CreateGameObjectData>(json);

                    var go = new GameObject(data.name);

                    if (data.position != null)
                    {
                        go.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
                    }

                    // Add components if specified
                    if (data.components != null)
                    {
                        foreach (var componentName in data.components)
                        {
                            var componentType = System.Type.GetType($"UnityEngine.{componentName}, UnityEngine");
                            if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                            {
                                go.AddComponent(componentType);
                            }
                            else
                            {
                                Debug.LogWarning($"[MCPEditorIntegration] Unknown component type: {componentName}");
                            }
                        }
                    }

                    // Set parent if specified
                    if (!string.IsNullOrEmpty(data.parent))
                    {
                        var parentObj = GameObject.Find(data.parent);
                        if (parentObj != null)
                        {
                            go.transform.SetParent(parentObj.transform);
                        }
                    }

                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);

                    Debug.Log($"[MCPEditorIntegration] Created GameObject: {data.name}");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[MCPEditorIntegration] Error creating GameObject: {ex.Message}");
                }
            };
        }

        private static void HandleListSceneObjects(string requestId, object parameters)
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                var sceneObjects = new List<SceneObject>();
                foreach (var root in rootObjects)
                {
                    BuildHierarchy(root.transform, sceneObjects, 0);
                }

                var response = new { objects = sceneObjects };
                _ = _client.SendResponseAsync(requestId, response);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error in HandleListSceneObjects: {ex.Message}");
            }
        }

        private static void BuildHierarchy(Transform transform, List<SceneObject> sceneObjects, int depth)
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

        [System.Serializable]
        private class SceneObject
        {
            [Newtonsoft.Json.JsonProperty("name")]
            public string name;
            [Newtonsoft.Json.JsonProperty("isActive")]
            public bool isActive;
            [Newtonsoft.Json.JsonProperty("depth")]
            public int depth;
        }

        private static void HandleGetProjectInfo(string requestId)
        {
            try
            {
                var response = new
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

                _ = _client.SendResponseAsync(requestId, response);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error in HandleGetProjectInfo: {ex.Message}");
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
                var logType = type.ToString();
                var logMessage = condition;
                var logStackTrace = stackTrace;
                EditorApplication.delayCall += () =>
                {
                    _ = _client.SendNotificationAsync("unity.logMessage", new
                    {
                        type = logType,
                        message = logMessage,
                        stackTrace = logStackTrace
                    });
                };
            }
        }

        private static void OnCompilationStarted(object obj)
        {
            _isCompiling = true;
            Debug.Log("[MCPEditorIntegration] Compilation started");

            if (_client.IsConnected)
            {
                EditorApplication.delayCall += () =>
                {
                    _ = _client.SendNotificationAsync("unity.compilationStarted", null);
                };
            }
        }

        private static void OnCompilationFinished(object obj)
        {
            _isCompiling = false;
            var succeeded = !EditorUtility.scriptCompilationFailed;

            Debug.Log($"[MCPEditorIntegration] Compilation finished: {(succeeded ? "Success" : "Failed")}");

            EditorApplication.delayCall += () =>
            {
                _ = HandleCompilationFinishedAsync(succeeded);
            };
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
            }
        }

        [System.Serializable]
        private class CreateGameObjectData
        {
            [Newtonsoft.Json.JsonProperty("name")]
            public string name;
            [Newtonsoft.Json.JsonProperty("position")]
            public PositionData position;
            [Newtonsoft.Json.JsonProperty("components")]
            public string[] components;
            [Newtonsoft.Json.JsonProperty("parent")]
            public string parent;
        }

        [System.Serializable]
        private class PositionData
        {
            [Newtonsoft.Json.JsonProperty("x")]
            public float x;
            [Newtonsoft.Json.JsonProperty("y")]
            public float y;
            [Newtonsoft.Json.JsonProperty("z")]
            public float z;
        }
    }
}
