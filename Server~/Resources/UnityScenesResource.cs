using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnityScenesResource(ILogger<UnityScenesResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnityScenesResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("All Unity scene files (.unity) in the project with paths relative to the project root. Useful for discovering available scenes before opening them.")]
public async Task<string> UnityScenes()
{
_logger.LogInformation("Fetching Unity scenes list resource...");

try
{
var response = await _webSocketService.SendRequestAsync<SceneListResponse>("unity.listScenes", null);
if (response?.Scenes != null && response.Scenes.Count > 0)
{
return string.Join("\n", response.Scenes);
}
return "No scenes found in project.";
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
