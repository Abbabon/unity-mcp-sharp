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
    [Description("Exit Unity play mode (stop running the game/scene). Equivalent to pressing the Stop button in Unity Editor.")]
    public async Task UnityExitPlayModeAsync()
    {
        _logger.LogInformation("Exiting Unity play mode");

        await _webSocketService.BroadcastNotificationAsync("unity.exitPlayMode", null);
    }
}
