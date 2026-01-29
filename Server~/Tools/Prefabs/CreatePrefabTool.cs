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
[Description("Create a prefab asset from an existing scene GameObject.")]
[return: Description("Confirmation with prefab path")]
public async Task<string> UnityCreatePrefabAsync(
[Description("Source GameObject name")] string gameObjectName,
[Description("Folder path (e.g., 'Prefabs/Characters')")] string assetFolderPath,
[Description("Prefab filename (defaults to GameObject name)")] string? prefabName = null,
[Description("Create variant instead of regular prefab")] bool createVariant = false,
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
