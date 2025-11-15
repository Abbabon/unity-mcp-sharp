using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class RunMenuItemTool(ILogger<RunMenuItemTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<RunMenuItemTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Execute any Unity Editor menu item by its menu path (e.g., 'Edit/Undo', 'Assets/Refresh', 'GameObject/Create Empty'). This allows running any Unity menu command programmatically.")]
    [return: Description("Confirmation that the menu item was executed")]
    public async Task<string> UnityRunMenuItemAsync(
        [Description("The full menu path of the item to execute (e.g., 'Edit/Undo', 'Assets/Refresh', 'GameObject/Create Empty')")]
        string menuPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing Unity menu item: {MenuPath}", menuPath);

        await _webSocketService.BroadcastNotificationAsync("unity.runMenuItem", new
        {
            menuPath
        });

        return $"Menu item '{menuPath}' execution requested";
    }
}
