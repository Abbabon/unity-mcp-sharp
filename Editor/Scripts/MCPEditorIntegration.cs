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

        // Operation tracking for visual feedback
        public class OperationInfo
        {
            public string Operation { get; set; }
            public System.DateTime Timestamp { get; set; }
            public string Status { get; set; } // "in_progress", "completed", "failed"
        }

        private static List<OperationInfo> _recentOperations = new List<OperationInfo>();
        private static string _currentOperation = null;

        public static List<OperationInfo> RecentOperations => _recentOperations;
        public static string CurrentOperation => _currentOperation;
        public static bool IsOperationInProgress => !string.IsNullOrEmpty(_currentOperation);

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
            Debug.Log($"[MCPEditorIntegration] Received notification: {method}");

            // Route to appropriate handler based on method name
            switch (method)
            {
                case "unity.triggerCompilation":
                    HandleTriggerCompilation(parameters);
                    break;

                case "unity.createGameObject":
                    Debug.Log("[MCPEditorIntegration] Routing to HandleCreateGameObject");
                    HandleCreateGameObject(parameters);
                    break;

                case "unity.createScript":
                    HandleCreateScript(parameters);
                    break;

                case "unity.addComponent":
                    HandleAddComponent(parameters);
                    break;

                case "unity.enterPlayMode":
                    HandleEnterPlayMode();
                    break;

                case "unity.exitPlayMode":
                    HandleExitPlayMode();
                    break;

                case "unity.refreshAssets":
                    HandleRefreshAssets();
                    break;

                case "unity.batchCreateGameObjects":
                    HandleBatchCreateGameObjects(parameters);
                    break;

                case "unity.openScene":
                    HandleOpenScene(parameters);
                    break;

                case "unity.closeScene":
                    HandleCloseScene(parameters);
                    break;

                case "unity.setActiveScene":
                    HandleSetActiveScene(parameters);
                    break;

                case "unity.saveScene":
                    HandleSaveScene(parameters);
                    break;

                case "unity.createGameObjectInScene":
                    HandleCreateGameObjectInScene(parameters);
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

                case "unity.getPlayModeState":
                    HandleGetPlayModeState(requestId);
                    break;

                case "unity.findGameObject":
                    HandleFindGameObject(requestId, parameters);
                    break;

                case "unity.listScenes":
                    HandleListScenes(requestId);
                    break;

                case "unity.getActiveScene":
                    HandleGetActiveScene(requestId);
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
            try
            {
                Debug.Log("[MCPEditorIntegration] Triggering script compilation...");
                CompilationPipeline.RequestScriptCompilation();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error triggering compilation: {ex.Message}");
            }
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
            StartOperation($"Create GameObject");
            try
            {
                // Parse parameters using Newtonsoft.Json for proper deserialization
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                Debug.Log($"[MCPEditorIntegration] HandleCreateGameObject received JSON: {json}");
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGameObjectData>(json);
                Debug.Log($"[MCPEditorIntegration] Parsed name: '{data.name}', components: {data.components?.Length ?? 0}");

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
                CompleteOperation(true);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error creating GameObject: {ex.Message}");
                CompleteOperation(false);
            }
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
                _ = _client.SendNotificationAsync("unity.logMessage", new
                {
                    type = type.ToString(),
                    message = condition,
                    stackTrace = stackTrace
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
            }
        }

        private static void HandleCreateScript(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateScriptData>(json);

                // Ensure folder path exists
                var fullFolderPath = System.IO.Path.Combine(Application.dataPath, data.folderPath);
                if (!System.IO.Directory.Exists(fullFolderPath))
                {
                    System.IO.Directory.CreateDirectory(fullFolderPath);
                }

                // Create script file
                var scriptFileName = $"{data.scriptName}.cs";
                var scriptPath = System.IO.Path.Combine(fullFolderPath, scriptFileName);

                System.IO.File.WriteAllText(scriptPath, data.scriptContent);

                // Refresh asset database to trigger compilation
                AssetDatabase.Refresh();

                Debug.Log($"[MCPEditorIntegration] Created script: {scriptPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error creating script: {ex.Message}");
            }
        }

        private static void HandleAddComponent(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<AddComponentData>(json);

                var go = GameObject.Find(data.gameObjectName);
                if (go == null)
                {
                    Debug.LogError($"[MCPEditorIntegration] GameObject not found: {data.gameObjectName}");
                    return;
                }

                // Try to find the component type
                // First try UnityEngine namespace
                var componentType = System.Type.GetType($"UnityEngine.{data.componentType}, UnityEngine");

                // If not found, try without namespace (for custom scripts)
                if (componentType == null)
                {
                    componentType = System.Type.GetType(data.componentType);
                }

                // If still not found, search all assemblies
                if (componentType == null)
                {
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        componentType = assembly.GetType(data.componentType);
                        if (componentType != null)
                            break;
                    }
                }

                if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                {
                    go.AddComponent(componentType);
                    Debug.Log($"[MCPEditorIntegration] Added component {data.componentType} to {data.gameObjectName}");
                }
                else
                {
                    Debug.LogError($"[MCPEditorIntegration] Component type not found: {data.componentType}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error adding component: {ex.Message}");
            }
        }

        private static void HandleEnterPlayMode()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = true;
                    Debug.Log("[MCPEditorIntegration] Entering play mode");
                }
                else
                {
                    Debug.LogWarning("[MCPEditorIntegration] Already in play mode");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error entering play mode: {ex.Message}");
            }
        }

        private static void HandleExitPlayMode()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    Debug.Log("[MCPEditorIntegration] Exiting play mode");
                }
                else
                {
                    Debug.LogWarning("[MCPEditorIntegration] Already stopped");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error exiting play mode: {ex.Message}");
            }
        }

        private static void HandleGetPlayModeState(string requestId)
        {
            try
            {
                string state;
                if (EditorApplication.isPlaying && EditorApplication.isPaused)
                {
                    state = "Paused";
                }
                else if (EditorApplication.isPlaying)
                {
                    state = "Playing";
                }
                else
                {
                    state = "Stopped";
                }

                var response = new { state };
                _ = _client.SendResponseAsync(requestId, response);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error in HandleGetPlayModeState: {ex.Message}");
            }
        }

        // ========== NEW UTILITY HANDLERS ==========

        private static void HandleRefreshAssets()
        {
            try
            {
                Debug.Log("[MCPEditorIntegration] Refreshing Asset Database");
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error refreshing assets: {ex.Message}");
            }
        }

        private static void HandleBatchCreateGameObjects(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<BatchCreateData>(json);

                var gameObjects = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGameObjectData[]>(data.gameObjectsJson);

                foreach (var goData in gameObjects)
                {
                    var go = new GameObject(goData.name);

                    if (goData.position != null)
                    {
                        go.transform.position = new Vector3(goData.position.x, goData.position.y, goData.position.z);
                    }

                    if (goData.components != null)
                    {
                        foreach (var componentName in goData.components)
                        {
                            var componentType = System.Type.GetType($"UnityEngine.{componentName}, UnityEngine");
                            if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                            {
                                go.AddComponent(componentType);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(goData.parent))
                    {
                        var parentObj = GameObject.Find(goData.parent);
                        if (parentObj != null)
                        {
                            go.transform.SetParent(parentObj.transform);
                        }
                    }
                }

                Debug.Log($"[MCPEditorIntegration] Batch created {gameObjects.Length} GameObjects");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error batch creating GameObjects: {ex.Message}");
            }
        }

        private static void HandleFindGameObject(string requestId, object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<FindGameObjectData>(json);

                GameObject go = null;

                switch (data.searchBy.ToLower())
                {
                    case "tag":
                        go = GameObject.FindWithTag(data.name);
                        break;
                    case "path":
                        go = GameObject.Find(data.name);
                        break;
                    case "name":
                    default:
                        go = GameObject.Find(data.name);
                        break;
                }

                if (go != null)
                {
                    var components = new List<string>();
                    foreach (var comp in go.GetComponents<Component>())
                    {
                        components.Add(comp.GetType().Name);
                    }

                    // Get full path in hierarchy
                    var path = go.name;
                    var parent = go.transform.parent;
                    while (parent != null)
                    {
                        path = parent.name + "/" + path;
                        parent = parent.parent;
                    }

                    var response = new
                    {
                        name = go.name,
                        path = path,
                        isActive = go.activeInHierarchy,
                        position = new { x = go.transform.position.x, y = go.transform.position.y, z = go.transform.position.z },
                        rotation = new { x = go.transform.eulerAngles.x, y = go.transform.eulerAngles.y, z = go.transform.eulerAngles.z },
                        scale = new { x = go.transform.localScale.x, y = go.transform.localScale.y, z = go.transform.localScale.z },
                        components = components
                    };

                    _ = _client.SendResponseAsync(requestId, response);
                }
                else
                {
                    Debug.LogWarning($"[MCPEditorIntegration] GameObject not found: {data.name}");
                    _ = _client.SendResponseAsync(requestId, null);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error finding GameObject: {ex.Message}");
            }
        }

        // ========== SCENE MANAGEMENT HANDLERS ==========

        private static void HandleListScenes(string requestId)
        {
            try
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                var scenes = new List<string>();

                foreach (var guid in sceneGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    scenes.Add(path);
                }

                var response = new { scenes };
                _ = _client.SendResponseAsync(requestId, response);

                Debug.Log($"[MCPEditorIntegration] Found {scenes.Count} scenes");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error listing scenes: {ex.Message}");
            }
        }

        private static void HandleOpenScene(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenSceneData>(json);

                var mode = data.additive ? UnityEditor.SceneManagement.OpenSceneMode.Additive : UnityEditor.SceneManagement.OpenSceneMode.Single;

                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(data.scenePath, mode);

                Debug.Log($"[MCPEditorIntegration] Opened scene: {data.scenePath} (additive: {data.additive})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error opening scene: {ex.Message}");
            }
        }

        private static void HandleCloseScene(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CloseSceneData>(json);

                // Find scene by name or path
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.name == data.sceneIdentifier || scene.path == data.sceneIdentifier)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                        Debug.Log($"[MCPEditorIntegration] Closed scene: {data.sceneIdentifier}");
                        return;
                    }
                }

                Debug.LogWarning($"[MCPEditorIntegration] Scene not found: {data.sceneIdentifier}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error closing scene: {ex.Message}");
            }
        }

        private static void HandleGetActiveScene(string requestId)
        {
            try
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

                var response = new
                {
                    name = scene.name,
                    path = scene.path,
                    isDirty = scene.isDirty,
                    rootCount = scene.rootCount,
                    isLoaded = scene.isLoaded
                };

                _ = _client.SendResponseAsync(requestId, response);

                Debug.Log($"[MCPEditorIntegration] Active scene: {scene.name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error getting active scene: {ex.Message}");
            }
        }

        private static void HandleSetActiveScene(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SetActiveSceneData>(json);

                // Find scene by name or path
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.name == data.sceneIdentifier || scene.path == data.sceneIdentifier)
                    {
                        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
                        Debug.Log($"[MCPEditorIntegration] Set active scene: {data.sceneIdentifier}");
                        return;
                    }
                }

                Debug.LogWarning($"[MCPEditorIntegration] Scene not found: {data.sceneIdentifier}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error setting active scene: {ex.Message}");
            }
        }

        private static void HandleSaveScene(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveSceneData>(json);

                if (data.saveAll)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[MCPEditorIntegration] Saved all open scenes");
                }
                else if (!string.IsNullOrEmpty(data.scenePath))
                {
                    // Find and save specific scene
                    for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                    {
                        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                        if (scene.path == data.scenePath)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                            Debug.Log($"[MCPEditorIntegration] Saved scene: {data.scenePath}");
                            return;
                        }
                    }
                    Debug.LogWarning($"[MCPEditorIntegration] Scene not found: {data.scenePath}");
                }
                else
                {
                    // Save active scene
                    var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                    Debug.Log($"[MCPEditorIntegration] Saved active scene: {activeScene.name}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error saving scene: {ex.Message}");
            }
        }

        private static void HandleCreateGameObjectInScene(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGameObjectInSceneData>(json);

                // Find or load the target scene
                Scene targetScene = default;
                bool sceneFound = false;

                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (scene.path == data.scenePath)
                    {
                        targetScene = scene;
                        sceneFound = true;
                        break;
                    }
                }

                // If scene not loaded, open it additively
                if (!sceneFound)
                {
                    targetScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(data.scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    Debug.Log($"[MCPEditorIntegration] Loaded scene additively: {data.scenePath}");
                }

                // Temporarily set as active scene to create GameObject in it
                var previousActiveScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(targetScene);

                // Create GameObject
                var go = new GameObject(data.name);

                if (data.position != null)
                {
                    go.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
                }

                if (data.components != null)
                {
                    foreach (var componentName in data.components)
                    {
                        var componentType = System.Type.GetType($"UnityEngine.{componentName}, UnityEngine");
                        if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                        {
                            go.AddComponent(componentType);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(data.parent))
                {
                    var parentObj = GameObject.Find(data.parent);
                    if (parentObj != null)
                    {
                        go.transform.SetParent(parentObj.transform);
                    }
                }

                // Restore previous active scene
                UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousActiveScene);

                Debug.Log($"[MCPEditorIntegration] Created GameObject '{data.name}' in scene '{data.scenePath}'");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MCPEditorIntegration] Error creating GameObject in scene: {ex.Message}");
            }
        }

        // ========== DATA CLASSES ==========

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
        private class CreateScriptData
        {
            [Newtonsoft.Json.JsonProperty("scriptName")]
            public string scriptName;
            [Newtonsoft.Json.JsonProperty("folderPath")]
            public string folderPath;
            [Newtonsoft.Json.JsonProperty("scriptContent")]
            public string scriptContent;
        }

        [System.Serializable]
        private class AddComponentData
        {
            [Newtonsoft.Json.JsonProperty("gameObjectName")]
            public string gameObjectName;
            [Newtonsoft.Json.JsonProperty("componentType")]
            public string componentType;
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

        [System.Serializable]
        private class BatchCreateData
        {
            [Newtonsoft.Json.JsonProperty("gameObjectsJson")]
            public string gameObjectsJson;
        }

        [System.Serializable]
        private class FindGameObjectData
        {
            [Newtonsoft.Json.JsonProperty("name")]
            public string name;
            [Newtonsoft.Json.JsonProperty("searchBy")]
            public string searchBy;
        }

        [System.Serializable]
        private class OpenSceneData
        {
            [Newtonsoft.Json.JsonProperty("scenePath")]
            public string scenePath;
            [Newtonsoft.Json.JsonProperty("additive")]
            public bool additive;
        }

        [System.Serializable]
        private class CloseSceneData
        {
            [Newtonsoft.Json.JsonProperty("sceneIdentifier")]
            public string sceneIdentifier;
        }

        [System.Serializable]
        private class SetActiveSceneData
        {
            [Newtonsoft.Json.JsonProperty("sceneIdentifier")]
            public string sceneIdentifier;
        }

        [System.Serializable]
        private class SaveSceneData
        {
            [Newtonsoft.Json.JsonProperty("scenePath")]
            public string scenePath;
            [Newtonsoft.Json.JsonProperty("saveAll")]
            public bool saveAll;
        }

        [System.Serializable]
        private class CreateGameObjectInSceneData
        {
            [Newtonsoft.Json.JsonProperty("scenePath")]
            public string scenePath;
            [Newtonsoft.Json.JsonProperty("name")]
            public string name;
            [Newtonsoft.Json.JsonProperty("position")]
            public PositionData position;
            [Newtonsoft.Json.JsonProperty("components")]
            public string[] components;
            [Newtonsoft.Json.JsonProperty("parent")]
            public string parent;
        }

        // ========== OPERATION TRACKING HELPERS ==========

        private static void StartOperation(string operationName)
        {
            _currentOperation = operationName;
            var op = new OperationInfo
            {
                Operation = operationName,
                Timestamp = System.DateTime.Now,
                Status = "in_progress"
            };
            _recentOperations.Insert(0, op);

            // Trim to max entries
            if (_recentOperations.Count > _config.maxOperationLogEntries)
            {
                _recentOperations.RemoveAt(_recentOperations.Count - 1);
            }

            if (_config.verboseLogging)
            {
                Debug.Log($"[MCP Operation] Started: {operationName}");
            }
        }

        private static void CompleteOperation(bool success = true)
        {
            if (!string.IsNullOrEmpty(_currentOperation) && _recentOperations.Count > 0)
            {
                _recentOperations[0].Status = success ? "completed" : "failed";
                if (_config.verboseLogging)
                {
                    Debug.Log($"[MCP Operation] {(success ? "Completed" : "Failed")}: {_currentOperation}");
                }
            }
            _currentOperation = null;
        }
    }
}
