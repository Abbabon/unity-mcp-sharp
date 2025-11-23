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
    [Description("Close a specific Unity scene. Only works when multiple scenes are open. Cannot close the last open scene. Scene must be identified by its path or name. Use unity_get_active_scene first to see which scenes are currently loaded.")]
    [return: Description("Confirmation message with the scene identifier that was closed")]
    public async Task<string> UnityCloseSceneAsync(
        [Description("Name or path of the scene to close")] string sceneIdentifier)
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
