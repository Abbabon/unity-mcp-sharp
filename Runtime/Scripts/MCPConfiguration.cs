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
        [Tooltip("Enable verbose logging")]
        public bool verboseLogging = false;

        [Tooltip("Maximum number of console logs to buffer")]
        [Range(50, 1000)]
        public int maxLogBuffer = 500;

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
                        Debug.Log("[MCPConfiguration] No configuration found. Creating default configuration file...");
                        _instance = CreateInstance<MCPConfiguration>();

                        var path = "Assets/Resources";
                        if (!System.IO.Directory.Exists(path))
                        {
                            System.IO.Directory.CreateDirectory(path);
                        }

                        var assetPath = $"{path}/MCPConfiguration.asset";
                        UnityEditor.AssetDatabase.CreateAsset(_instance, assetPath);
                        UnityEditor.AssetDatabase.SaveAssets();
                        Debug.Log($"[MCPConfiguration] Created default configuration at {assetPath}");
#else
                        // Runtime fallback (shouldn't happen in normal use)
                        Debug.LogWarning("[MCPConfiguration] No configuration found. Using in-memory defaults.");
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
                    Debug.LogWarning($"[MCPConfiguration] Config file already exists at {assetPath}. Loading existing instead.");
                    _instance = UnityEditor.AssetDatabase.LoadAssetAtPath<MCPConfiguration>(assetPath);
                }
                else
                {
                    UnityEditor.AssetDatabase.CreateAsset(this, assetPath);
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log($"[MCPConfiguration] Created and saved to {assetPath}");
                }
            }
            else
            {
                // Save existing asset
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log($"[MCPConfiguration] Configuration saved");
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
            verboseLogging = false;
            maxLogBuffer = 500;

#if UNITY_EDITOR
            if (!IsInMemory())
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssets();
            }
#endif
            Debug.Log("[MCPConfiguration] Reset to default values");
        }
    }
}
