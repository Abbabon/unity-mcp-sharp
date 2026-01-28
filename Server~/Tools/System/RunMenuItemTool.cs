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
    [Description("Execute any Unity menu item by path (e.g., 'Edit/Undo', 'Assets/Refresh').")]
    [return: Description("Confirmation that menu item was triggered")]
    public async Task<string> UnityRunMenuItemAsync(
        [Description("Menu path like 'Edit/Undo' or 'GameObject/Create Empty'")]
        string menuPath,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing Unity menu item: {MenuPath}", menuPath);

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.runMenuItem", new
        {
            menuPath
        });

        return $"Menu item '{menuPath}' execution requested";
    }
}
