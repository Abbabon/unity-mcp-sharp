using UnityEngine;

namespace UnityMCPSharp
{
    /// <summary>
    /// Centralized logging for MCP components.
    /// Respects MCPConfiguration.enableMcpLogs setting.
    /// Caches configuration reference for performance in hot paths.
    /// </summary>
    public static class MCPLogger
    {
        private static MCPConfiguration _cachedConfig;
        private static MCPConfiguration Config => _cachedConfig ??= MCPConfiguration.Instance;

        /// <summary>
        /// Invalidate cached configuration. Call this when configuration changes.
        /// </summary>
        public static void InvalidateCache() => _cachedConfig = null;

        /// <summary>
        /// Log an informational message (only if MCP logs are enabled)
        /// </summary>
        public static void Log(string message)
        {
            if (Config.enableMcpLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Log a warning message (only if MCP logs are enabled)
        /// </summary>
        public static void LogWarning(string message)
        {
            if (Config.enableMcpLogs)
            {
                Debug.LogWarning(message);
            }
        }

        /// <summary>
        /// Log an error message (always shown - errors should never be suppressed)
        /// </summary>
        public static void LogError(string message)
        {
            // Errors are always logged regardless of setting
            Debug.LogError(message);
        }

        /// <summary>
        /// Log a verbose/debug message (only if both MCP logs AND verbose logging are enabled)
        /// </summary>
        public static void LogVerbose(string message)
        {
            var config = Config;
            if (config.enableMcpLogs && config.verboseLogging)
            {
                Debug.Log(message);
            }
        }
    }
}
