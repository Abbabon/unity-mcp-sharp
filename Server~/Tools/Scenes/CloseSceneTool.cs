using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Scenes;

[McpServerToolType]
public class CloseSceneTool(ILogger<CloseSceneTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<CloseSceneTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Close a specific Unity scene. Only works when multiple scenes are open. Cannot close the last open scene. Scene must be identified by its path or name.")]
    public async Task UnityCloseSceneAsync(
        [Description("Name or path of the scene to close")] string sceneIdentifier)
    {
        _logger.LogInformation("Closing scene: {SceneIdentifier}", sceneIdentifier);

        var parameters = new
        {
            sceneIdentifier
        };

        await _webSocketService.BroadcastNotificationAsync("unity.closeScene", parameters);
    }
}
