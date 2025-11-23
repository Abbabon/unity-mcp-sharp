using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class AddComponentTool(ILogger<AddComponentTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<AddComponentTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Add a component to an existing GameObject in the scene. Can add Unity built-in components or custom MonoBehaviour scripts. Use unity_find_game_object first to verify the GameObject exists and see its current components.")]
    [return: Description("Confirmation message with GameObject name and component added")]
    public async Task<string> UnityAddComponentToObjectAsync(
        [Description("Name of the GameObject to add the component to")] string gameObjectName,
        [Description("Component type name (e.g., 'Rigidbody', 'BoxCollider', or your custom script name)")] string componentType)
    {
        _logger.LogInformation("Adding component {ComponentType} to GameObject {GameObjectName}", componentType, gameObjectName);

        var parameters = new
        {
            gameObjectName,
            componentType
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.addComponent", parameters);

        return $"Component '{componentType}' added to GameObject '{gameObjectName}'";
    }
}
