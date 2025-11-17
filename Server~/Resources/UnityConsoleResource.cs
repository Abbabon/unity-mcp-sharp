using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnityConsoleResource(ILogger<UnityConsoleResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnityConsoleResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("Recent console logs from Unity Editor including error messages, warnings, and debug logs. Useful for debugging runtime issues and monitoring Unity's output. Subscribe to this resource to receive notifications when new logs appear.")]
public async Task<string> UnityConsoleLogs()
{
_logger.LogInformation("Fetching Unity console logs resource...");

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
