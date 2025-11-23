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
[Description("Save changes made to a prefab that is currently open in Prefab Mode. This persists modifications to the prefab asset. If no prefab is currently open, this will attempt to save prefab instances back to their source prefab assets. IMPORTANT: Always call this after making changes in Prefab Mode to ensure changes are not lost.")]
[return: Description("Confirmation message indicating what was saved")]
public async Task<string> UnitySavePrefabAsync(
[Description("Optional: Specific prefab asset path to save (e.g., 'Assets/Prefabs/Character.prefab'). If not specified, saves the currently open prefab in Prefab Mode.")] string? prefabPath = null,
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
