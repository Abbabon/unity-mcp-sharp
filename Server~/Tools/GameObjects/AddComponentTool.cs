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
    [Description("Add a component to an existing GameObject (built-in or custom script).")]
    [return: Description("Confirmation of component added")]
    public async Task<string> UnityAddComponentToObjectAsync(
        [Description("Target GameObject name")] string gameObjectName,
        [Description("Component type (e.g., 'Rigidbody', 'BoxCollider')")] string componentType)
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
