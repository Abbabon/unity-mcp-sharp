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
    [Description("Find a GameObject and get its transform, components, and active state.")]
    [return: Description("GameObject details: position, rotation, scale, components")]
    public async Task<string> UnityFindGameObjectAsync(
        [Description("Name, tag, or path to search for")] string name,
        [Description("'name', 'tag', or 'path'")] string searchBy = "name")
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
