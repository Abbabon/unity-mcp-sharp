using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class BatchCreateGameObjectsTool(ILogger<BatchCreateGameObjectsTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<BatchCreateGameObjectsTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Create multiple GameObjects in a single operation. More efficient than creating them one by one. Each GameObject can have its own position, components, and parent. Use unity_list_scene_objects after to verify all objects were created successfully.")]
    [return: Description("Confirmation message indicating batch creation was initiated")]
    public async Task<string> UnityBatchCreateGameObjectsAsync(
        [Description("JSON array of GameObject specifications. Each should have: name, position {x,y,z}, components (comma-separated), parent")] string gameObjectsJson)
    {
        _logger.LogInformation("Batch creating GameObjects");

        var parameters = new
        {
            gameObjectsJson
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.batchCreateGameObjects", parameters);

        return $"Batch GameObject creation initiated. Use unity_list_scene_objects to verify.";
    }
}
