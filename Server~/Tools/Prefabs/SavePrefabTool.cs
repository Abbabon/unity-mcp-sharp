using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Prefabs;

[McpServerToolType]
public class SavePrefabTool(ILogger<SavePrefabTool> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<SavePrefabTool> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerTool]
[Description("Save changes to current prefab in Prefab Mode or apply instance overrides.")]
[return: Description("Confirmation of save")]
public async Task<string> UnitySavePrefabAsync(
[Description("Prefab path to save (empty = current Prefab Mode)")] string? prefabPath = null,
CancellationToken cancellationToken = default)
{
_logger.LogInformation("Saving prefab: {PrefabPath}", prefabPath ?? "current prefab stage");

var parameters = new
{
prefabPath
};

await _webSocketService.SendToCurrentSessionEditorAsync("unity.savePrefab", parameters);

if (prefabPath != null)
{
return $"Prefab '{prefabPath}' saved successfully";
}
else
{
return "Currently open prefab in Prefab Mode saved successfully";
}
}
}
