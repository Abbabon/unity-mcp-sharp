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
    [Description("Bring the Unity Editor window to the foreground. This is useful when Unity is minimized or behind other windows and you need to ensure operations complete without timeout. Note: Most MCP operations automatically bring Unity to foreground when the 'Auto Bring to Foreground' setting is enabled in Unity's MCP Configuration. Use this tool explicitly if auto-focus is disabled or you need to ensure Unity is visible before a series of operations.")]
    [return: Description("Confirmation message indicating the foreground request was sent")]
    public async Task<string> UnityBringEditorToForegroundAsync()
    {
        _logger.LogInformation("Requesting Unity Editor to come to foreground");

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.bringToForeground", null);

        return "Unity Editor foreground request sent. The editor window should now be in focus (if auto-focus is enabled in Unity's MCP Configuration).";
    }
}
