using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Scenes;

[McpServerToolType]
public class SetActiveSceneTool(ILogger<SetActiveSceneTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<SetActiveSceneTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Set which scene should be the active scene. The active scene is where new GameObjects are created. Only works when multiple scenes are open. Scene must be already loaded. Use unity_get_active_scene after to verify the change.")]
    [return: Description("Confirmation message with the scene identifier that is now active")]
    public async Task<string> UnitySetActiveSceneAsync(
        [Description("Name or path of the scene to make active")] string sceneIdentifier)
    {
        _logger.LogInformation("Setting active scene: {SceneIdentifier}", sceneIdentifier);

        var parameters = new
        {
            sceneIdentifier
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.setActiveScene", parameters);

        return $"Scene '{sceneIdentifier}' is now the active scene. New GameObjects will be created here.";
    }
}
