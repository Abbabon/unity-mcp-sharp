using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Models;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class DeleteGameObjectTool(ILogger<DeleteGameObjectTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<DeleteGameObjectTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Delete a GameObject from the scene by name. The GameObject and all its children will be permanently destroyed. Use unity_find_game_object first to verify the object exists, or unity_list_scene_objects to see all objects in the scene.")]
    [return: Description("Confirmation message indicating the GameObject was deleted, or error if not found")]
    public async Task<string> UnityDeleteGameObjectAsync(
        [Description("Name of the GameObject to delete")] string name)
    {
        _logger.LogInformation("Deleting GameObject: {Name}", name);

        try
        {
            var parameters = new { name };

            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<OperationResponse>("unity.deleteGameObject", parameters);

            if (response != null)
            {
                if (response.Success)
                {
                    return response.Message;
                }
                else
                {
                    return $"Failed to delete GameObject: {response.Message}";
                }
            }

            return $"Failed to delete GameObject '{name}': No response from Unity Editor";
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
