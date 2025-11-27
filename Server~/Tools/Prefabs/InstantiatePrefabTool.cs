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
[Description("Instantiate (spawn) a prefab into the currently active scene. The prefab must exist in the Assets folder. You can specify the position, rotation, scale, and parent for the new instance. Use unity_list_scene_objects after instantiation to verify the instance was created, or unity_find_game_object to get details about the spawned instance.")]
[return: Description("Confirmation message with instance name, position, and prefab source path")]
public async Task<string> UnityInstantiatePrefabAsync(
[Description("Path to the prefab asset relative to Assets folder (e.g., 'Prefabs/Character.prefab' or 'Prefabs/Enemy')")] string prefabPath,
[Description("X position in world space where the prefab should be spawned (default: 0)")] float x = 0,
[Description("Y position in world space where the prefab should be spawned (default: 0)")] float y = 0,
[Description("Z position in world space where the prefab should be spawned (default: 0)")] float z = 0,
[Description("X rotation in Euler angles (default: 0)")] float rotationX = 0,
[Description("Y rotation in Euler angles (default: 0)")] float rotationY = 0,
[Description("Z rotation in Euler angles (default: 0)")] float rotationZ = 0,
[Description("X scale multiplier (default: 1)")] float scaleX = 1,
[Description("Y scale multiplier (default: 1)")] float scaleY = 1,
[Description("Z scale multiplier (default: 1)")] float scaleZ = 1,
[Description("Name of parent GameObject in hierarchy (optional, leave empty for root level)")] string? parent = null,
[Description("Custom name for the instantiated object (optional, defaults to prefab name)")] string? instanceName = null,
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
