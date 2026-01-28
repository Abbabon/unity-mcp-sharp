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
    [Description("Stop play mode. All play mode changes will be reverted.")]
    [return: Description("Confirmation that play mode stopped")]
    public async Task<string> UnityExitPlayModeAsync()
    {
        _logger.LogInformation("Exiting Unity play mode");

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.exitPlayMode", null);

        return "Unity exited play mode. All play mode changes have been reverted.";
    }
}
