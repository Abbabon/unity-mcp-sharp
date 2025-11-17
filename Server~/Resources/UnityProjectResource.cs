using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Resources;

[McpServerResourceType]
public class UnityProjectResource(ILogger<UnityProjectResource> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<UnityProjectResource> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerResource]
[Description("Unity project metadata including project name, Unity version, currently active scene name and path, data directory path, and whether the Editor is in play mode or paused. Useful for understanding the project environment context.")]
public async Task<string> UnityProjectInfo()
{
_logger.LogInformation("Fetching Unity project info resource...");

try
{
var response = await _webSocketService.SendRequestAsync<ProjectInfoResponse>("unity.getProjectInfo", null);
if (response != null)
{
return $"Project Name: {response.ProjectName}\n" +
$"Unity Version: {response.UnityVersion}\n" +
$"Active Scene: {response.ActiveScene}\n" +
$"Scene Path: {response.ScenePath}\n" +
$"Data Path: {response.DataPath}\n" +
$"Is Playing: {response.IsPlaying}\n" +
$"Is Paused: {response.IsPaused}";
}
return "Unable to retrieve project info.";
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
