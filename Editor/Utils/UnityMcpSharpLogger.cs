using UnityEngine;

namespace Editor.Utils
{
    /// <summary>
    /// Special logger to use inside the MCP Unity Editor project
    /// </summary>
    public static class UnityMcpSharpLogger
    {
        private const string LogPrefix = "[Unity MCP Sharp] ";
        
        /// <summary>
        /// Log an info message if info logs are enabled
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogInfo(string message)
        {
            Debug.Log($"{LogPrefix}{message}");
        }
        
        /// <summary>
        /// Log a warning message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{LogPrefix}{message}");
        }
        
        /// <summary>
        /// Log an error message
        /// </summary>
        /// <param name="message">Message to log</param>
        public static void LogError(string message)
        {
            Debug.LogError($"{LogPrefix}{message}");
        }
    }
}