using UnityEngine;

namespace UnityMCPSharp
{
    /// <summary>
    /// Configuration settings for Unity MCP Server integration
    /// </summary>
    [CreateAssetMenu(fileName = "MCPConfiguration", menuName = "Unity MCP/Configuration", order = 1)]
    public class MCPConfiguration : ScriptableObject
    {
        [Header("Server Settings")]
        [Tooltip("WebSocket URL of the MCP server")]
        public string serverUrl = "ws://localhost:8080/ws";

        [Tooltip("HTTP URL of the MCP server")]
        public string httpUrl = "http://localhost:8080";

        [Tooltip("Docker container name")]
        public string containerName = "unity-mcp-server";

        [Tooltip("Docker image name")]
        public string dockerImage = "ghcr.io/abbabon/unity-mcp-server:latest";

        [Header("Connection Settings")]
        [Tooltip("Automatically connect to server when Unity starts")]
        public bool autoConnect = true;

        [Tooltip("Automatically start Docker container if not running")]
        public bool autoStartContainer = true;

        [Tooltip("Connection retry attempts")]
        [Range(0, 10)]
        public int retryAttempts = 3;

        [Tooltip("Delay between retry attempts (seconds)")]
        [Range(1, 30)]
        public int retryDelay = 5;

        [Header("Logging")]
        [Tooltip("Enable MCP logs in Unity console (connection, protocol, operations)")]
        public bool enableMcpLogs = true;

        [Tooltip("Enable verbose/debug logging (more detailed output)")]
        public bool verboseLogging = false;

        [Tooltip("Maximum number of console logs to buffer")]
        [Range(50, 1000)]
        public int maxLogBuffer = 500;

        [Header("Visual Feedback")]
        [Tooltip("Show visual feedback when MCP operations are in progress")]
        public bool showVisualFeedback = true;

        [Tooltip("Background color tint when MCP is active")]
        public Color feedbackColor = new Color(0.3f, 0.5f, 0.7f, 0.1f);

        [Tooltip("Show recent operations log in Dashboard")]
        public bool showOperationLog = true;

        [Tooltip("Maximum number of operations to display in log")]
        [Range(5, 50)]
        public int maxOperationLogEntries = 20;

        [Tooltip("Show MCP status overlay in Scene View")]
        public bool showSceneViewOverlay = true;

        private static MCPConfiguration _instance;

        public static MCPConfiguration Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<MCPConfiguration>("MCPConfiguration");
                    if (_instance == null)
                    {
#if UNITY_EDITOR
                        // Auto-create config file on first run
                        MCPLogger.Log("[MCPConfiguration] No configuration found. Creating default configuration file...");
                        _instance = CreateInstance<MCPConfiguration>();

                        var path = "Assets/Resources";
                        if (!System.IO.Directory.Exists(path))
                        {
                            System.IO.Directory.CreateDirectory(path);
                        }

                        var assetPath = $"{path}/MCPConfiguration.asset";
                        UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                        UnityEditor.AssetDatabase.SaveAssets();
                        MCPLogger.Log($"[MCPConfiguration] Created default configuration at {assetPath}");
#else
                        // Runtime fallback (shouldn't happen in normal use)
                        MCPLogger.LogWarning("[MCPConfiguration] No configuration found. Using in-memory defaults.");
                        _instance = CreateInstance<MCPConfiguration>();
#endif
                    }
                }
                return _instance;
            }
        }

        public bool IsInMemory()
        {
#if UNITY_EDITOR
            return string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(this));
#else
            return false;
#endif
        }

        public void SaveToResources()
        {
#if UNITY_EDITOR
            if (IsInMemory())
            {
                // Create new asset file for in-memory instance
                var path = "Assets/Resources";
                if (!System.IO.Directory.Exists(path))
                {
                    System.IO.Directory.CreateDirectory(path);
                }

                var assetPath = $"{path}/MCPConfiguration.asset";

                // Check if file already exists
                if (System.IO.File.Exists(assetPath))
                {
                    MCPLogger.LogWarning($"[MCPConfiguration] Config file already exists at {assetPath}. Loading existing instead.");
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<MCPConfiguration>(assetPath);
                }
                else
                {
                    UnityEditor.AssetDatabase.CreateAsset(this, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    MCPLogger.Log($"[MCPConfiguration] Created and saved to {assetPath}");
                }
            }
            else
            {
                // Save existing asset
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                MCPLogger.Log($"[MCPConfiguration] Configuration saved");
            }
#endif
        }

        public void ResetToDefaults()
        {
            serverUrl = "ws://localhost:8080/ws";
            httpUrl = "http://localhost:8080";
            containerName = "unity-mcp-server";
            dockerImage = "ghcr.io/abbabon/unity-mcp-server:latest";
            autoConnect = true;
            autoStartContainer = true;
            retryAttempts = 3;
            retryDelay = 5;
            enableMcpLogs = true;
            verboseLogging = false;
            maxLogBuffer = 500;
            showVisualFeedback = true;
            feedbackColor = new Color(0.3f, 0.5f, 0.7f, 0.1f);
            showOperationLog = true;
            maxOperationLogEntries = 20;
            showSceneViewOverlay = true;

#if UNITY_EDITOR
            if (!IsInMemory())
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
            MCPLogger.Log("[MCPConfiguration] Reset to default values");
        }
    }
}
