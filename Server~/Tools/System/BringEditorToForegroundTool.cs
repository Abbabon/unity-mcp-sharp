using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class BringEditorToForegroundTool(ILogger<BringEditorToForegroundTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<BringEditorToForegroundTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Bring Unity Editor window to foreground. Usually automatic; use if auto-focus is disabled.")]
    [return: Description("Confirmation that foreground request was sent")]
    public async Task<string> UnityBringEditorToForegroundAsync()
    {
        _logger.LogInformation("Requesting Unity Editor to come to foreground");

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.bringToForeground", null);

        return "Unity Editor foreground request sent. The editor window should now be in focus (if auto-focus is enabled in Unity's MCP Configuration).";
    }
}
