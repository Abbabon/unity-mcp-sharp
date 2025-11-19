using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnityActiveSceneResource(ILogger<UnityActiveSceneResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnityActiveSceneResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("Information about the currently active Unity scene including scene name, path, isDirty status, and GameObject count. The active scene is where new GameObjects are created by default. Subscribe to receive notifications when the active scene changes.")]
public async Task<string> UnityActiveScene()
{
_logger.LogInformation("Fetching Unity active scene resource...");

try
{
var response = await _webSocketService.SendRequestAsync<ActiveSceneResponse>("unity.getActiveScene", null);
if (response != null)
{
return $"Scene Name: {response.Name ?? "Unknown"}\n" +
$"Scene Path: {response.Path ?? "N/A"}\n" +
$"Is Dirty: {response.IsDirty}\n" +
$"GameObject Count: {response.RootCount}";
}
return "Unable to retrieve active scene info.";
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
