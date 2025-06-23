using Newtonsoft.Json.Linq;

namespace Editor.Bridge.Interfaces
{
    /// <summary>
    /// Interface for the console logs service
    /// </summary>
    public interface IConsoleLogsService
    {
        /// <summary>
        /// Get logs as a JSON object with pagination support
        /// </summary>
        /// <param name="logType">Filter by log type (empty for all)</param>
        /// <param name="offset">Starting index (0-based)</param>
        /// <param name="limit">Maximum number of logs to return (default: 100)</param>
        /// <param name="includeStackTrace">Whether to include stack trace in logs (default: true)</param>
        /// <returns>JObject containing logs array and pagination info</returns>
        JObject GetLogsAsJson(string logType = "", int offset = 0, int limit = 100, bool includeStackTrace = true);
        
        /// <summary>
        /// Start listening for logs
        /// </summary>
        void StartListening();
        
        /// <summary>
        /// Stop listening for logs
        /// </summary>
        void StopListening();
        
        /// <summary>
        /// Manually clean up old log entries, keeping only the most recent ones
        /// </summary>
        /// <param name="keepCount">Number of recent entries to keep (default: 500)</param>
        void CleanupOldLogs(int keepCount = 500);
        
        /// <summary>
        /// Get current log count
        /// </summary>
        /// <returns>Number of stored log entries</returns>
        int GetLogCount();
    }
}