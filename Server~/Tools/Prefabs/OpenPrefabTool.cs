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
[Description("Open a prefab asset in Prefab Mode (isolation mode) for editing. This allows you to edit the prefab structure without affecting the scene. The prefab will be opened in the Unity Editor's Prefab Stage. Use unity_save_prefab to persist changes and unity_close_prefab_stage when done editing. IMPORTANT: Only one prefab can be open in Prefab Mode at a time - close the current prefab before opening another.")]
[return: Description("Confirmation message with prefab path and stage information")]
public async Task<string> UnityOpenPrefabAsync(
[Description("Path to the prefab asset relative to Assets folder (e.g., 'Prefabs/Character.prefab' or 'Prefabs/Enemy')")] string prefabPath,
[Description("If true, opens in Context mode (shows the prefab instance in its scene context). If false, opens in isolation mode (default: false)")] bool inContext = false,
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
