using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class ListEditorsTool(ILogger<ListEditorsTool> logger, EditorSessionManager sessionManager)
{
    private readonly ILogger<ListEditorsTool> _logger = logger;
    private readonly EditorSessionManager _sessionManager = sessionManager;

    [McpServerTool]
    [Description("List all connected Unity Editor instances with project, scene, and connection info.")]
    [return: Description("Connected editors with IDs, project names, scenes, and selection status")]
    public async Task<string> UnityListEditorsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing Unity Editor instances");

        var editors = _sessionManager.GetAllEditors();
        var sessionId = McpSessionContext.CurrentSessionId;
        var selectedEditorId = sessionId != null ? _sessionManager.GetSelectedEditorForSession(sessionId) : null;

        if (editors.Count == 0)
        {
            return "No Unity Editor instances are currently connected. Please ensure Unity is running with the MCP package installed and connected to this server.";
        }

        var result = new StringBuilder();
        result.AppendLine($"Connected Unity Editors ({editors.Count}):");
        result.AppendLine();

        foreach (var editor in editors)
        {
            var isSelected = editor.ConnectionId == selectedEditorId;
            var marker = isSelected ? " [SELECTED]" : "";

            result.AppendLine($"Connection ID: {editor.ConnectionId}{marker}");
            result.AppendLine($"  Project: {editor.ProjectName}");
            result.AppendLine($"  Scene: {editor.ActiveScene} ({editor.ScenePath})");
            result.AppendLine($"  Machine: {editor.MachineName}");
            result.AppendLine($"  Unity Version: {editor.UnityVersion}");
            result.AppendLine($"  Platform: {editor.Platform}");
            result.AppendLine($"  Process ID: {editor.ProcessId}");
            result.AppendLine($"  Playing: {editor.IsPlaying}");
            result.AppendLine($"  Connected: {editor.ConnectedAt:yyyy-MM-dd HH:mm:ss} UTC");
            result.AppendLine($"  Data Path: {editor.DataPath}");
            result.AppendLine();
        }

        if (selectedEditorId == null && editors.Count > 1)
        {
            result.AppendLine("No editor is currently selected for this session.");
            result.AppendLine("Use unity_select_editor with a Connection ID to choose which editor to use.");
        }
        else if (selectedEditorId == null && editors.Count == 1)
        {
            result.AppendLine("Only one editor is connected - it will be auto-selected when you use any Unity tool.");
        }

        return await Task.FromResult(result.ToString());
    }
}
