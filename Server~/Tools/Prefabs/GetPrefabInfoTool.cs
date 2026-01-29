using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Prefabs;

[McpServerToolType]
public class GetPrefabInfoTool(ILogger<GetPrefabInfoTool> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<GetPrefabInfoTool> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerTool]
[Description("Get prefab status for a GameObject (asset, instance, variant, modifications).")]
[return: Description("JSON with prefab info")]
public async Task<string> UnityGetPrefabInfoAsync(
[Description("GameObject name or asset path")] string gameObjectNameOrPath,
CancellationToken cancellationToken = default)
{
_logger.LogInformation("Getting prefab info for: {Name}", gameObjectNameOrPath);

var parameters = new
{
gameObjectNameOrPath
};

// This requires a request-response pattern since we need data back
var result = await _webSocketService.SendRequestToCurrentSessionEditorAsync<object>(
"unity.getPrefabInfo",
parameters,
timeoutSeconds: 10);

if (result == null)
{
return $"No prefab information found for '{gameObjectNameOrPath}'";
}

// Return the JSON result as-is
return JsonSerializer.Serialize(result, new JsonSerializerOptions
{
WriteIndented = true,
PropertyNamingPolicy = JsonNamingPolicy.CamelCase
});
}
}
