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
    [Description("Start play mode to run the game. WARNING: Changes made in play mode are NOT saved.")]
    [return: Description("Confirmation that play mode started")]
    public async Task<string> UnityEnterPlayModeAsync()
    {
        _logger.LogInformation("Entering Unity play mode");

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.enterPlayMode", null);

        return "Unity entered play mode. Remember: changes made in play mode are NOT saved!";
    }
}
