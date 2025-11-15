using System.Collections.Generic;
using System;

using UnityEngine;
using UnityMCPSharp;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to retrieve console logs from the Unity Editor.
    /// </summary>
    public static class GetConsoleLogsHandler
    {
        public static void Handle(string requestId, object parameters, MCPClient client, Queue<string> consoleLogBuffer, MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Get Console Logs", config.maxOperationLogEntries, config.verboseLogging, null);

                var logEntries = new List<LogEntry>();
                foreach (var log in consoleLogBuffer)
                {
                    // Parse the log entry format: [Type] Message
                    var parts = log.Split(new[] { "] " }, 2, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        var type = parts[0].TrimStart('[');
                        var message = parts[1];
                        logEntries.Add(new LogEntry { type = type, message = message });
                    }
                }

                var response = new { logs = logEntries };
                _ = client.SendResponseAsync(requestId, response);

                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetConsoleLogsHandler] Error: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
