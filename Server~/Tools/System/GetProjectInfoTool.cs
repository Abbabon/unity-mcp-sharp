using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class GetProjectInfoTool(ILogger<GetProjectInfoTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<GetProjectInfoTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Get metadata about the Unity project including project name, Unity version, currently active scene name and path, data directory path, and whether the Editor is in play mode or paused. Useful for context about the project environment. This is a good first tool to call when starting work on a project to understand the environment.")]
    [return: Description("Project information: name, Unity version, active scene, paths, and editor state")]
    public async Task<string> UnityGetProjectInfoAsync()
    {
        _logger.LogInformation("Requesting project info from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<ProjectInfoResponse>("unity.getProjectInfo", null);
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
