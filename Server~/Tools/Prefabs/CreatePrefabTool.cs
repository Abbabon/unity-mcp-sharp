using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Prefabs;

[McpServerToolType]
public class CreatePrefabTool(ILogger<CreatePrefabTool> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<CreatePrefabTool> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerTool]
[Description("Create a prefab asset from an existing GameObject in the scene. The GameObject must exist in the current scene. The prefab will be created in the specified Assets folder path. Use unity_find_game_object first to verify the GameObject exists, and unity_get_prefab_info after creation to verify the prefab was created successfully.")]
[return: Description("Confirmation message with prefab path and source GameObject name")]
public async Task<string> UnityCreatePrefabAsync(
[Description("Name of the GameObject in the scene to convert to prefab")] string gameObjectName,
[Description("Path within Assets folder where prefab should be created (e.g., 'Prefabs', 'Prefabs/Characters'). The folder will be created if it doesn't exist.")] string assetFolderPath,
[Description("Name for the prefab file (without .prefab extension). If not specified, uses the GameObject name.")] string? prefabName = null,
[Description("If true, creates a prefab variant instead of a regular prefab. Requires the source to be a prefab instance.")] bool createVariant = false,
CancellationToken cancellationToken = default)
{
_logger.LogInformation("Creating prefab from GameObject: {Name} to {Path}", gameObjectName, assetFolderPath);

var finalPrefabName = prefabName ?? gameObjectName;

var parameters = new
{
gameObjectName,
assetFolderPath,
prefabName = finalPrefabName,
createVariant
};

await _webSocketService.SendToCurrentSessionEditorAsync("unity.createPrefab", parameters);

var variantInfo = createVariant ? " (variant)" : "";
return $"Prefab '{finalPrefabName}.prefab'{variantInfo} created from GameObject '{gameObjectName}' at Assets/{assetFolderPath}/{finalPrefabName}.prefab";
}
}
