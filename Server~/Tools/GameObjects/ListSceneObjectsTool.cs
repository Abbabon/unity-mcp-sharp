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
    [Description("Get the complete GameObject hierarchy of the currently active Unity scene. Returns all GameObjects with their parent-child relationships, showing which objects are active or inactive. Useful for understanding scene structure.")]
    [return: Description("Hierarchical list of all GameObjects in the active scene with their active/inactive state")]
    public async Task<string> UnityListSceneObjectsAsync()
    {
        _logger.LogInformation("Requesting scene objects from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<SceneObjectsResponse>("unity.listSceneObjects", null);
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
