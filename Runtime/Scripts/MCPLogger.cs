using UnityEngine;

namespace UnityMCPSharp
{
    /// <summary>
    /// Centralized logging for MCP components.
    /// Respects MCPConfiguration.enableMcpLogs setting.
    /// </summary>
    public static class MCPLogger
    {
        /// <summary>
        /// Log an informational message (only if MCP logs are enabled)
        /// </summary>
        public static void Log(string message)
        {
            if (MCPConfiguration.Instance.enableMcpLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// Log a warning message (only if MCP logs are enabled)
        /// </summary>
        public static void LogWarning(string message)
        {
            if (MCPConfiguration.Instance.enableMcpLogs)
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
            var config = MCPConfiguration.Instance;
            if (config.enableMcpLogs && config.verboseLogging)
            {
                Debug.Log(message);
            }
        }
    }
}
