using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnityCompilationResource(ILogger<UnityCompilationResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnityCompilationResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("Current compilation status indicating whether Unity is compiling scripts and if the last compilation succeeded or failed. Useful before entering play mode or making additional code changes. Subscribe to receive notifications when compilation completes.")]
public async Task<string> UnityCompilationStatus()
{
_logger.LogInformation("Fetching Unity compilation status resource...");

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
