using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class GetCompilationStatusTool(ILogger<GetCompilationStatusTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<GetCompilationStatusTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Check if Unity is currently compiling scripts. Returns whether compilation is in progress and if the last compilation succeeded or failed. Useful before running play mode or making additional code changes. If compilation failed, use unity_get_console_logs to see the errors. Use unity_trigger_script_compilation to force recompilation if needed.")]
    [return: Description("Current compilation status: whether Unity is compiling and if last compilation succeeded")]
    public async Task<string> UnityGetCompilationStatusAsync()
    {
        _logger.LogInformation("Requesting compilation status from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<CompilationStatusResponse>("unity.getCompilationStatus", null);
            if (response != null)
            {
                var status = response.IsCompiling ? "Compiling..." : "Idle";
                var lastResult = response.LastCompilationSucceeded ? "succeeded" : "failed";
                return $"Compilation Status: {status}\nLast Compilation: {lastResult}";
            }
            return "Unable to retrieve compilation status.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
