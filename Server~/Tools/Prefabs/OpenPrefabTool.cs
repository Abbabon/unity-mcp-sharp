using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Prefabs;

[McpServerToolType]
public class OpenPrefabTool(ILogger<OpenPrefabTool> logger, UnityWebSocketService webSocketService)
{
private readonly ILogger<OpenPrefabTool> _logger = logger;
private readonly UnityWebSocketService _webSocketService = webSocketService;

[McpServerTool]
[Description("Open a prefab in Prefab Mode for editing (isolation or context mode).")]
[return: Description("Confirmation with prefab path")]
public async Task<string> UnityOpenPrefabAsync(
[Description("Prefab path (e.g., 'Prefabs/Character.prefab')")] string prefabPath,
[Description("Open in context mode instead of isolation")] bool inContext = false,
CancellationToken cancellationToken = default)
{
_logger.LogInformation("Opening prefab in Prefab Mode: {PrefabPath}", prefabPath);

var parameters = new
{
prefabPath,
inContext
};

await _webSocketService.SendToCurrentSessionEditorAsync("unity.openPrefab", parameters);

var modeInfo = inContext ? " in Context mode" : " in isolation mode";
return $"Prefab '{prefabPath}' opened in Prefab Mode{modeInfo}. Use unity_save_prefab to persist changes and unity_close_prefab_stage when done.";
}
}
