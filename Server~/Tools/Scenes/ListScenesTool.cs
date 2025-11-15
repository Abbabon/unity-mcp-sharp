using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.Scenes;

[McpServerToolType]
public class ListScenesTool(ILogger<ListScenesTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<ListScenesTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("List all Unity scene files (.unity) in the project. Returns scene paths relative to the project root. Useful for discovering available scenes before opening them.")]
    [return: Description("List of all scene file paths in the project")]
    public async Task<string> UnityListScenesAsync()
    {
        _logger.LogInformation("Listing all scenes in project");

        try
        {
            var response = await _webSocketService.SendRequestAsync<SceneListResponse>("unity.listScenes", null);
            if (response?.Scenes != null && response.Scenes.Count > 0)
            {
                return string.Join("\n", response.Scenes);
            }
            return "No scenes found in project.";
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
