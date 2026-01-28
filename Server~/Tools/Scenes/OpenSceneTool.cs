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
    [Description("Open a scene by path. Use additive=true to keep other scenes open.")]
    [return: Description("Confirmation with scene path and mode")]
    public async Task<string> UnityOpenSceneAsync(
        [Description("Scene path (e.g., 'Scenes/Level1.unity')")] string scenePath,
        [Description("Keep other scenes open if true")] bool additive = false)
    {
        _logger.LogInformation("Opening scene: {ScenePath} (additive: {Additive})", scenePath, additive);

        var parameters = new
        {
            scenePath,
            additive
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.openScene", parameters);

        var mode = additive ? "additively (keeping other scenes open)" : "in single mode (closing other scenes)";
        return $"Scene '{scenePath}' opened {mode}";
    }
}
