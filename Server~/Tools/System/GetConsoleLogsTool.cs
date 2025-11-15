using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class GetConsoleLogsTool(ILogger<GetConsoleLogsTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<GetConsoleLogsTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Get recent console logs from Unity Editor. Returns error messages, warnings, and debug logs from the Unity Console window. Useful for debugging runtime issues and monitoring Unity's output.")]
    [return: Description("Recent console logs from Unity Editor including errors, warnings, and info messages")]
    public async Task<string> UnityGetConsoleLogsAsync()
    {
        _logger.LogInformation("Requesting console logs from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<ConsoleLogsResponse>("unity.getConsoleLogs", null);
            if (response?.Logs != null && response.Logs.Count > 0)
            {
                return string.Join("\n", response.Logs.Select(log =>
                    $"[{log.Type}] {log.Message}" + (string.IsNullOrEmpty(log.StackTrace) ? "" : $"\n{log.StackTrace}")));
            }
            return "No console logs available.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
