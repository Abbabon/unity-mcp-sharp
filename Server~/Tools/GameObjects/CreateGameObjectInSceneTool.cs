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
    [Description("Create a GameObject in a specific scene (not necessarily the active one). Requires the scene to be loaded. If scene is not loaded, it will be opened additively first. Use unity_list_scenes to find available scenes, then unity_set_active_scene if you want to make that scene active.")]
    [return: Description("Confirmation message with GameObject name, scene path, and position")]
    public async Task<string> UnityCreateGameObjectInSceneAsync(
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

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.createGameObjectInScene", parameters);

        var componentInfo = components != null ? $" with components [{components}]" : "";
        return $"GameObject '{name}' created in scene '{scenePath}' at position ({x}, {y}, {z}){componentInfo}";
    }
}
