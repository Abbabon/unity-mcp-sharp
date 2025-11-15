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
    [Description("Force Unity to recompile all C# scripts. Use this after making code changes or when experiencing compilation issues. Unity will reload assemblies and report any compilation errors. Note: Unity will temporarily disconnect during compilation. Use unity_get_compilation_status after to check if compilation succeeded, or unity_get_console_logs to see any errors.")]
    [return: Description("Confirmation message that compilation was triggered")]
    public async Task<string> UnityTriggerScriptCompilationAsync()
    {
        _logger.LogInformation("Triggering Unity script compilation...");
        await _webSocketService.BroadcastNotificationAsync("unity.triggerCompilation", null);

        return "Unity script compilation triggered. Unity will reload assemblies (temporary disconnect expected).";
    }
}
