using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Assets;

[McpServerToolType]
public class RefreshAssetsTool(ILogger<RefreshAssetsTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<RefreshAssetsTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Refresh Unity Asset Database to detect file changes. Use this after batch file operations or when changes aren't detected automatically. Triggers Unity to reimport assets and recompile scripts. Note: This can take a few seconds for large projects. Use unity_get_compilation_status after to check if recompilation is complete.")]
    [return: Description("Confirmation message that asset refresh was initiated")]
    public async Task<string> UnityRefreshAssetsAsync()
    {
        _logger.LogInformation("Refreshing Unity Asset Database");

        await _webSocketService.BroadcastNotificationAsync("unity.refreshAssets", null);

        return "Unity Asset Database refresh initiated. Unity will now reimport assets and recompile scripts.";
    }
}
