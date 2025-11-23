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
[Description("Close the currently open Prefab Mode (Prefab Stage) and return to scene editing. If there are unsaved changes, they will be lost unless you called unity_save_prefab first. This is required to exit prefab editing mode and return to normal scene editing. You must close the current prefab before opening another one.")]
[return: Description("Confirmation message indicating the Prefab Stage was closed")]
public async Task<string> UnityClosePrefabStageAsync(
[Description("If true, saves the prefab before closing. If false, discards unsaved changes (default: true)")] bool saveBeforeClosing = true,
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
