using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class ExitPlayModeTool(ILogger<ExitPlayModeTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<ExitPlayModeTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Exit Unity play mode (stop running the game/scene). Equivalent to pressing the Stop button in Unity Editor. All changes made during play mode will be reverted, and any GameObjects created will be destroyed.")]
    [return: Description("Confirmation message that play mode was exited")]
    public async Task<string> UnityExitPlayModeAsync()
    {
        _logger.LogInformation("Exiting Unity play mode");

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.exitPlayMode", null);

        return "Unity exited play mode. All play mode changes have been reverted.";
    }
}
