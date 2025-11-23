using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class SelectEditorTool(ILogger<SelectEditorTool> logger, EditorSessionManager sessionManager)
{
    private readonly ILogger<SelectEditorTool> _logger = logger;
    private readonly EditorSessionManager _sessionManager = sessionManager;

    [McpServerTool]
    [Description("Select which Unity Editor instance to use for this MCP session. When multiple Unity Editors are connected, you must select one before using other unity_* tools. The selection persists across compilation reconnects. Use unity_list_editors to see available editors and their connection IDs.")]
    [return: Description("Confirmation message with the selected editor's display name")]
    public async Task<string> UnitySelectEditorAsync(
        [Description("Connection ID of the Unity Editor to select (get this from unity_list_editors)")] string connectionId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Selecting Unity Editor: {ConnectionId}", connectionId);

        var sessionId = McpSessionContext.CurrentSessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            throw new InvalidOperationException("No MCP session context available. This should not happen - please report this issue.");
        }

        var editor = _sessionManager.GetEditor(connectionId);
        if (editor == null)
        {
            var availableCount = _sessionManager.GetEditorCount();
            throw new InvalidOperationException($"Unity Editor '{connectionId}' not found. There are {availableCount} editor(s) connected. Use unity_list_editors to see available editors.");
        }

        var success = _sessionManager.SelectEditorForSession(sessionId, connectionId);
        if (!success)
        {
            throw new InvalidOperationException($"Failed to select Unity Editor '{connectionId}'");
        }

        return await Task.FromResult($"Selected Unity Editor: {editor.DisplayName} (ID: {connectionId}). All subsequent unity_* tools in this session will target this editor.");
    }
}
