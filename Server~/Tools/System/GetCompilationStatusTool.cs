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
    [Description("Check if Unity is compiling scripts and whether last compilation succeeded.")]
    [return: Description("Compilation status: compiling state and last result")]
    public async Task<string> UnityGetCompilationStatusAsync()
    {
        _logger.LogInformation("Requesting compilation status from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<CompilationStatusResponse>("unity.getCompilationStatus", null);
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
