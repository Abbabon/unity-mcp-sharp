using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnityPlayModeResource(ILogger<UnityPlayModeResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnityPlayModeResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("Current play mode state of Unity Editor indicating whether Unity is Playing, Paused, or Stopped. Subscribe to receive notifications when play mode state changes (entering/exiting play mode or pausing).")]
public async Task<string> UnityPlayModeState()
{
_logger.LogInformation("Fetching Unity play mode state resource...");

try
{
var response = await _webSocketService.SendRequestAsync<PlayModeStateResponse>("unity.getPlayModeState", null);
if (response != null)
{
return $"Play Mode State: {response.State ?? "Unknown"}";
}
return "Unable to retrieve play mode state.";
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
