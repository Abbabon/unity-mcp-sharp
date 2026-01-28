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
    [Description("Close a scene (only when multiple scenes are open).")]
    [return: Description("Confirmation of closed scene")]
    public async Task<string> UnityCloseSceneAsync(
        [Description("Scene name or path to close")] string sceneIdentifier)
    {
        _logger.LogInformation("Closing scene: {SceneIdentifier}", sceneIdentifier);

        var parameters = new
        {
            sceneIdentifier
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.closeScene", parameters);

        return $"Scene '{sceneIdentifier}' closed";
    }
}
