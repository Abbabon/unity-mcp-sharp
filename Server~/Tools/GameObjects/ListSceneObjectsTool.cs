using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class ListSceneObjectsTool(ILogger<ListSceneObjectsTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<ListSceneObjectsTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("List all GameObjects in the active scene with hierarchy and active state.")]
    [return: Description("Hierarchical list of GameObjects")]
    public async Task<string> UnityListSceneObjectsAsync()
    {
        _logger.LogInformation("Requesting scene objects from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<SceneObjectsResponse>("unity.listSceneObjects", null);
            if (response?.Objects != null && response.Objects.Count > 0)
            {
                return string.Join("\n", response.Objects.Select(obj =>
                    $"{new string(' ', obj.Depth * 2)}{(obj.IsActive ? "✓" : "✗")} {obj.Name}"));
            }
            return "No GameObjects in scene or scene is empty.";
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
