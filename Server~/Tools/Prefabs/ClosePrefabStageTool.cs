using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Prefabs;

[McpServerToolType]
public class ClosePrefabStageTool(ILogger<ClosePrefabStageTool> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<ClosePrefabStageTool> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerTool]
[Description("Close Prefab Mode and return to scene editing.")]
[return: Description("Confirmation of close")]
public async Task<string> UnityClosePrefabStageAsync(
[Description("Save before closing (default: true)")] bool saveBeforeClosing = true,
CancellationToken cancellationToken = default)
{
_logger.LogInformation("Closing Prefab Stage (save: {Save})", saveBeforeClosing);

var parameters = new
{
saveBeforeClosing
};

await _webSocketService.SendToCurrentSessionEditorAsync("unity.closePrefabStage", parameters);

var saveInfo = saveBeforeClosing ? " (saved)" : " (unsaved changes discarded)";
return $"Prefab Stage closed{saveInfo}. Returned to scene editing mode.";
}
}
