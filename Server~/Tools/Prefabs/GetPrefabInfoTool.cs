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
[Description("Get detailed information about a GameObject's prefab status and relationships. Returns whether the GameObject is a prefab asset, prefab instance, variant, or regular GameObject. Also provides the asset path for prefabs and information about modifications. Use this to understand prefab relationships before using unity_open_prefab or unity_save_prefab.")]
[return: Description("JSON object with prefab information including isPrefabAsset, isPrefabInstance, isPrefabVariant, assetPath, isModified, and prefabInstanceStatus")]
public async Task<string> UnityGetPrefabInfoAsync(
[Description("Name of the GameObject to query (can be a scene GameObject or prefab asset path like 'Assets/Prefabs/Character.prefab')")] string gameObjectNameOrPath,
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
