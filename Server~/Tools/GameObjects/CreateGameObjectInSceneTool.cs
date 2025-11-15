using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class CreateGameObjectInSceneTool(ILogger<CreateGameObjectInSceneTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<CreateGameObjectInSceneTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Create a GameObject in a specific scene (not necessarily the active one). Requires the scene to be loaded. If scene is not loaded, it will be opened additively first.")]
    public async Task UnityCreateGameObjectInSceneAsync(
        [Description("Path to the scene where the GameObject should be created (e.g., 'Scenes/Level1.unity')")] string scenePath,
        [Description("Name of the GameObject to create")] string name,
        [Description("X position in world space (default: 0)")] float x = 0,
        [Description("Y position in world space (default: 0)")] float y = 0,
        [Description("Z position in world space (default: 0)")] float z = 0,
        [Description("Comma-separated list of Unity components to add (e.g., 'Rigidbody,BoxCollider,AudioSource')")] string? components = null,
        [Description("Name of parent GameObject in hierarchy (optional, leave empty for root level)")] string? parent = null)
    {
        _logger.LogInformation("Creating GameObject '{Name}' in scene '{ScenePath}'", name, scenePath);

        var parameters = new
        {
            scenePath,
            name,
            position = new { x, y, z },
            components = components?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            parent
        };

        await _webSocketService.BroadcastNotificationAsync("unity.createGameObjectInScene", parameters);
    }
}
