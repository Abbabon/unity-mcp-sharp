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
    [Description("Set which loaded scene is active (where new GameObjects are created).")]
    [return: Description("Confirmation of new active scene")]
    public async Task<string> UnitySetActiveSceneAsync(
        [Description("Scene name or path to make active")] string sceneIdentifier)
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
