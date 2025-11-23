using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class FindGameObjectTool(ILogger<FindGameObjectTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<FindGameObjectTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Find a GameObject by name, tag, or path and return detailed information about it including position, rotation, scale, active state, and all attached components. Use unity_list_scene_objects first to see all available GameObjects if you don't know the exact name. Use unity_add_component_to_object to add components to the found GameObject.")]
    [return: Description("GameObject information including transform, active state, and components")]
    public async Task<string> UnityFindGameObjectAsync(
        [Description("Name of the GameObject to find")] string name,
        [Description("Search by: 'name' (default), 'tag', or 'path'. Path format: 'Parent/Child/Object'")] string searchBy = "name")
    {
        _logger.LogInformation("Finding GameObject: {Name} by {SearchBy}", name, searchBy);

        try
        {
            var parameters = new
            {
                name,
                searchBy
            };

            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<GameObjectInfoResponse>("unity.findGameObject", parameters);
            if (response != null)
            {
                var info = $"Name: {response.Name}\n" +
                          $"Path: {response.Path}\n" +
                          $"Active: {response.IsActive}\n" +
                          $"Position: ({response.Position.X:F2}, {response.Position.Y:F2}, {response.Position.Z:F2})\n" +
                          $"Rotation: ({response.Rotation.X:F2}, {response.Rotation.Y:F2}, {response.Rotation.Z:F2})\n" +
                          $"Scale: ({response.Scale.X:F2}, {response.Scale.Y:F2}, {response.Scale.Z:F2})\n" +
                          $"Components: {string.Join(", ", response.Components)}";
                return info;
            }
            return "GameObject not found.";
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
