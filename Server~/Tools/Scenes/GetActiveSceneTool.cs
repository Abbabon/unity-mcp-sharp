using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.Scenes;

[McpServerToolType]
public class GetActiveSceneTool(ILogger<GetActiveSceneTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<GetActiveSceneTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Get info about the active scene: name, path, dirty state, object count.")]
    [return: Description("Active scene details")]
    public async Task<string> UnityGetActiveSceneAsync()
    {
        _logger.LogInformation("Getting active scene info");

        try
        {
            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<ActiveSceneResponse>("unity.getActiveScene", null);
            if (response != null)
            {
                return $"Scene Name: {response.Name}\n" +
                       $"Scene Path: {response.Path}\n" +
                       $"Is Dirty: {response.IsDirty}\n" +
                       $"Root GameObject Count: {response.RootCount}\n" +
                       $"Is Loaded: {response.IsLoaded}";
            }
            return "Unable to get active scene.";
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
