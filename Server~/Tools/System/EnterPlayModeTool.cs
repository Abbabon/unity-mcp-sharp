using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class EnterPlayModeTool(ILogger<EnterPlayModeTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<EnterPlayModeTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Enter Unity play mode (start running the game/scene). Equivalent to pressing the Play button in Unity Editor.")]
    public async Task UnityEnterPlayModeAsync()
    {
        _logger.LogInformation("Entering Unity play mode");

        await _webSocketService.BroadcastNotificationAsync("unity.enterPlayMode", null);
    }
}
