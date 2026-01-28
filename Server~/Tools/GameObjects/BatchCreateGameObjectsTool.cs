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
    [Description("Create multiple GameObjects in one operation for efficiency.")]
    [return: Description("Confirmation of batch creation")]
    public async Task<string> UnityBatchCreateGameObjectsAsync(
        [Description("JSON array with name, position, components, parent per object")] string gameObjectsJson)
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
