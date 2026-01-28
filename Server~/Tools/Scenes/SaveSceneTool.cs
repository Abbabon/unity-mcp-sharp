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
    [Description("Save the active scene or all open scenes. Always save after making changes.")]
    [return: Description("Confirmation of saved scene(s)")]
    public async Task<string> UnitySaveSceneAsync(
        [Description("Scene path, or empty for active scene")] string? scenePath = null,
        [Description("Save all open scenes if true")] bool saveAll = false)
    {
        _logger.LogInformation("Saving scene: {ScenePath} (saveAll: {SaveAll})", scenePath ?? "active", saveAll);

        var parameters = new
        {
            scenePath,
            saveAll
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.saveScene", parameters);

        if (saveAll)
            return "All open scenes saved";
        else if (scenePath != null)
            return $"Scene '{scenePath}' saved";
        else
            return "Active scene saved";
    }
}
