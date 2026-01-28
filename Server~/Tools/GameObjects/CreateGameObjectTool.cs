using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class CreateGameObjectTool(ILogger<CreateGameObjectTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<CreateGameObjectTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Create a GameObject in the active scene with optional position, components, and parent.")]
    [return: Description("Confirmation with name and position")]
    public async Task<string> UnityCreateGameObjectAsync(
        [Description("Name for the new GameObject")] string name,
        [Description("X position (default: 0)")] float x = 0,
        [Description("Y position (default: 0)")] float y = 0,
        [Description("Z position (default: 0)")] float z = 0,
        [Description("Components to add, comma-separated")] string? components = null,
        [Description("Parent GameObject name (empty for root)")] string? parent = null)
    {
        _logger.LogInformation("Creating GameObject: {Name}", name);

        var parameters = new
        {
            name,
            position = new { x, y, z },
            components = components?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            parent
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.createGameObject", parameters);

        var componentInfo = components != null ? $" with components [{components}]" : "";
        var parentInfo = parent != null ? $" as child of '{parent}'" : " at root level";
        return $"GameObject '{name}' created at position ({x}, {y}, {z}){componentInfo}{parentInfo}";
    }
}
