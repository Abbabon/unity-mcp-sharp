using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class EnterPlayModeTool(ILogger<EnterPlayModeTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<EnterPlayModeTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Enter Unity play mode (start running the game/scene). Equivalent to pressing the Play button in Unity Editor. IMPORTANT: Changes made to GameObjects in play mode are NOT saved unless explicitly copied. Any GameObjects created in play mode will be destroyed when exiting. Use unity_get_play_mode_state to check current state, and unity_exit_play_mode when done testing.")]
    [return: Description("Confirmation message that play mode was entered")]
    public async Task<string> UnityEnterPlayModeAsync()
    {
        _logger.LogInformation("Entering Unity play mode");

        await _webSocketService.BroadcastNotificationAsync("unity.enterPlayMode", null);

        return "Unity entered play mode. Remember: changes made in play mode are NOT saved!";
    }
}
