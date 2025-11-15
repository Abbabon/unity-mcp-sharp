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
    [Description("Add a component to an existing GameObject in the scene. Can add Unity built-in components or custom MonoBehaviour scripts.")]
    public async Task UnityAddComponentToObjectAsync(
        [Description("Name of the GameObject to add the component to")] string gameObjectName,
        [Description("Component type name (e.g., 'Rigidbody', 'BoxCollider', or your custom script name)")] string componentType)
    {
        _logger.LogInformation("Adding component {ComponentType} to GameObject {GameObjectName}", componentType, gameObjectName);

        var parameters = new
        {
            gameObjectName,
            componentType
        };

        await _webSocketService.BroadcastNotificationAsync("unity.addComponent", parameters);
    }
}
