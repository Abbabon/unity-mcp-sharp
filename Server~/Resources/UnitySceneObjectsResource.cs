using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnitySceneObjectsResource(ILogger<UnitySceneObjectsResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnitySceneObjectsResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("Complete GameObject hierarchy of the currently active Unity scene with parent-child relationships and active/inactive states. Useful for understanding scene structure. Subscribe to receive notifications when GameObjects are created, destroyed, or hierarchy changes.")]
public async Task<string> UnitySceneObjects()
{
_logger.LogInformation("Fetching Unity scene objects resource...");

try
{
var response = await _webSocketService.SendRequestAsync<SceneObjectsResponse>("unity.listSceneObjects", null);
if (response?.Objects != null && response.Objects.Count > 0)
{
return string.Join("\n", response.Objects.Select(obj =>
$"{new string(' ', obj.Depth * 2)}{(obj.IsActive ? "✓" : "✗")} {obj.Name}"));
}
return "No GameObjects in scene or scene is empty.";
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
