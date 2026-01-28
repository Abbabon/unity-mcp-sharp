using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Prefabs;

[McpServerToolType]
public class InstantiatePrefabTool(ILogger<InstantiatePrefabTool> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<InstantiatePrefabTool> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerTool]
[Description("Spawn a prefab instance into the active scene with transform options.")]
[return: Description("Confirmation with instance details")]
public async Task<string> UnityInstantiatePrefabAsync(
[Description("Prefab path (e.g., 'Prefabs/Character.prefab')")] string prefabPath,
[Description("X position (default: 0)")] float x = 0,
[Description("Y position (default: 0)")] float y = 0,
[Description("Z position (default: 0)")] float z = 0,
[Description("X rotation in degrees (default: 0)")] float rotationX = 0,
[Description("Y rotation in degrees (default: 0)")] float rotationY = 0,
[Description("Z rotation in degrees (default: 0)")] float rotationZ = 0,
[Description("X scale (default: 1)")] float scaleX = 1,
[Description("Y scale (default: 1)")] float scaleY = 1,
[Description("Z scale (default: 1)")] float scaleZ = 1,
[Description("Parent GameObject name (empty for root)")] string? parent = null,
[Description("Custom instance name (defaults to prefab name)")] string? instanceName = null,
CancellationToken cancellationToken = default)
{
_logger.LogInformation("Instantiating prefab: {PrefabPath} at ({X}, {Y}, {Z})", prefabPath, x, y, z);

var parameters = new
{
prefabPath,
position = new { x, y, z },
rotation = new { x = rotationX, y = rotationY, z = rotationZ },
scale = new { x = scaleX, y = scaleY, z = scaleZ },
parent,
instanceName
};

await _webSocketService.SendToCurrentSessionEditorAsync("unity.instantiatePrefab", parameters);

var parentInfo = parent != null ? $" as child of '{parent}'" : " at root level";
var nameInfo = instanceName != null ? $" named '{instanceName}'" : "";
return $"Prefab instance spawned from '{prefabPath}' at position ({x}, {y}, {z}){nameInfo}{parentInfo}";
}
}
