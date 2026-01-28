using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class TriggerCompilationTool(ILogger<TriggerCompilationTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<TriggerCompilationTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Force recompilation of all C# scripts. Unity may briefly disconnect during reload.")]
    [return: Description("Confirmation that compilation was triggered")]
    public async Task<string> UnityTriggerScriptCompilationAsync()
    {
        _logger.LogInformation("Triggering Unity script compilation...");
        await _webSocketService.SendToCurrentSessionEditorAsync("unity.triggerCompilation", null);

        return "Unity script compilation triggered. Unity will reload assemblies (temporary disconnect expected).";
    }
}
