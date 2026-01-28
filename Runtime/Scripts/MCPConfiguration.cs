using UnityEngine;

namespace UnityMCPSharp
{
    /// <summary>
    /// Tool profile determines which MCP tools are exposed to LLM clients.
    /// Use Minimal to reduce token usage, Full for all capabilities.
    /// </summary>
    public enum ToolProfile
    {
        /// <summary>12 core tools for basic workflows (~1k tokens)</summary>
        Minimal,
        /// <summary>20 commonly used tools (~2k tokens) - DEFAULT</summary>
        Standard,
        /// <summary>All 28 tools including advanced features (~3k tokens)</summary>
        Full
    }
    /// <summary>
    /// Configuration settings for Unity MCP Server integration
    /// </summary>
    [CreateAssetMenu(fileName = "MCPConfiguration", menuName = "Unity MCP/Configuration", order = 1)]
    public class MCPConfiguration : ScriptableObject
    {
        [Header("Server Settings")]
        [Tooltip("Port for the MCP server (used for Docker container). Must be 1024-65535.")]
        [Range(1024, 65535)]
        public int serverPort = 3727;

        [Tooltip("WebSocket URL of the MCP server")]
        public string serverUrl = "ws://localhost:3727/ws";

        [Tooltip("HTTP URL of the MCP server")]
        public string httpUrl = "http://localhost:3727";

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

        [Header("Background Behavior")]
        [Tooltip("Timeout for MCP operations in seconds. Increase this if Unity is often unfocused/minimized when using MCP tools. Operations are queued and will complete when Unity regains focus.")]
        [Range(10, 120)]
        public int operationTimeout = 30;

        [Tooltip("Automatically bring Unity Editor to foreground when MCP operations require it. This uses platform-specific APIs (SetForegroundWindow on Windows, NSApplication.activate on macOS) to focus the Unity window, ensuring operations complete without timeout.")]
        public bool autoBringToForeground = true;

        [Header("Tool Profile")]
        [Tooltip("Controls which MCP tools are exposed to LLM clients. Minimal (12 tools) reduces token usage, Standard (20 tools) covers common workflows, Full (28 tools) enables all capabilities.")]
        public ToolProfile toolProfile = ToolProfile.Standard;

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
            serverPort = 3727;
            serverUrl = "ws://localhost:3727/ws";
            httpUrl = "http://localhost:3727";
            containerName = "unity-mcp-server";
            dockerImage = "ghcr.io/abbabon/unity-mcp-server:latest";
            autoConnect = true;
            autoStartContainer = true;
            retryAttempts = 3;
            retryDelay = 5;
            operationTimeout = 30;
            autoBringToForeground = true;
            toolProfile = ToolProfile.Standard;
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
