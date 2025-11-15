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
    [Description("Force Unity to recompile all C# scripts. Use this after making code changes or when experiencing compilation issues. Unity will reload assemblies and report any compilation errors.")]
    public async Task UnityTriggerScriptCompilationAsync()
    {
        _logger.LogInformation("Triggering Unity script compilation...");
        await _webSocketService.BroadcastNotificationAsync("unity.triggerCompilation", null);
    }
}
