using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Scenes;

[McpServerToolType]
public class SaveSceneTool(ILogger<SaveSceneTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<SaveSceneTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Save the currently active scene or a specific scene by path. Can save just the specified scene or all open scenes. Returns success/failure status.")]
    public async Task UnitySaveSceneAsync(
        [Description("Path to specific scene to save, or null/empty to save active scene")] string? scenePath = null,
        [Description("If true, saves all currently open scenes. If false (default), saves only the specified/active scene.")] bool saveAll = false)
    {
        _logger.LogInformation("Saving scene: {ScenePath} (saveAll: {SaveAll})", scenePath ?? "active", saveAll);

        var parameters = new
        {
            scenePath,
            saveAll
        };

        await _webSocketService.BroadcastNotificationAsync("unity.saveScene", parameters);
    }
}
