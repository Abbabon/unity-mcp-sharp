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
    [Description("Refresh Asset Database to detect file changes and trigger reimport.")]
    [return: Description("Confirmation that refresh started")]
    public async Task<string> UnityRefreshAssetsAsync()
    {
        _logger.LogInformation("Refreshing Unity Asset Database");

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.refreshAssets", null);

        return "Unity Asset Database refresh initiated. Unity will now reimport assets and recompile scripts.";
    }
}
