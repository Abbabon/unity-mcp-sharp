using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Scenes;

[McpServerToolType]
public class OpenSceneTool(ILogger<OpenSceneTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<OpenSceneTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Open a Unity scene by path. Can open additively (keeping current scenes open) or single mode (closing all other scenes). Scene path should be relative to Assets folder. Use unity_list_scenes to find available scenes, and unity_get_active_scene after opening to verify which scene is now active.")]
    [return: Description("Confirmation message with scene path and mode (single or additive)")]
    public async Task<string> UnityOpenSceneAsync(
        [Description("Path to scene file relative to Assets folder (e.g., 'Scenes/Level1.unity')")] string scenePath,
        [Description("If true, opens scene additively without closing current scenes. If false (default), closes all other scenes first.")] bool additive = false)
    {
        _logger.LogInformation("Opening scene: {ScenePath} (additive: {Additive})", scenePath, additive);

        var parameters = new
        {
            scenePath,
            additive
        };

        await _webSocketService.BroadcastNotificationAsync("unity.openScene", parameters);

        var mode = additive ? "additively (keeping other scenes open)" : "in single mode (closing other scenes)";
        return $"Scene '{scenePath}' opened {mode}";
    }
}
