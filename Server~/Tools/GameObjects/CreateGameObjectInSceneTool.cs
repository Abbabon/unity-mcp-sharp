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
    [Description("Create a GameObject in a specific scene (loads scene if needed).")]
    [return: Description("Confirmation with name, scene, and position")]
    public async Task<string> UnityCreateGameObjectInSceneAsync(
        [Description("Scene path (e.g., 'Scenes/Level1.unity')")] string scenePath,
        [Description("Name for the new GameObject")] string name,
        [Description("X position (default: 0)")] float x = 0,
        [Description("Y position (default: 0)")] float y = 0,
        [Description("Z position (default: 0)")] float z = 0,
        [Description("Components to add, comma-separated")] string? components = null,
        [Description("Parent GameObject name (empty for root)")] string? parent = null)
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
