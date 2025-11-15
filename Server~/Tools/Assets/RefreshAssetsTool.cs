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
    [Description("Refresh Unity Asset Database to detect file changes. Use this after batch file operations or when changes aren't detected automatically. Triggers Unity to reimport assets and recompile scripts.")]
    public async Task UnityRefreshAssetsAsync()
    {
        _logger.LogInformation("Refreshing Unity Asset Database");

        await _webSocketService.BroadcastNotificationAsync("unity.refreshAssets", null);
    }
}
