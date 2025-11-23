using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using UnityMcpServer.Models;

namespace UnityMcpServer.Services;

/// <summary>
/// Manages the relationship between MCP sessions and Unity Editor instances.
/// Tracks which Unity editor each MCP client is using.
/// </summary>
public class EditorSessionManager
{
    private readonly ILogger<EditorSessionManager> _logger;

    // Maps connectionId → EditorMetadata
    private readonly ConcurrentDictionary<string, EditorMetadata> _editors = new();

    // Maps MCP sessionId → selected Unity editor connectionId
    private readonly ConcurrentDictionary<string, string> _sessionEditors = new();

    // Event raised when a new editor connects (so MCP clients can be notified)
    public event Action<EditorMetadata>? EditorConnected;

    // Event raised when an editor disconnects
    public event Action<EditorMetadata>? EditorDisconnected;

    public EditorSessionManager(ILogger<EditorSessionManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Register a new Unity Editor connection
    /// </summary>
    public void RegisterEditor(string connectionId, EditorMetadata metadata)
    {
        metadata.ConnectionId = connectionId;
        metadata.ConnectedAt = DateTime.UtcNow;

        _editors.TryAdd(connectionId, metadata);
        _logger.LogInformation(
            "Unity Editor registered: {ConnectionId} - {DisplayName}",
            connectionId,
            metadata.DisplayName);

        EditorConnected?.Invoke(metadata);
    }

    /// <summary>
    /// Unregister a Unity Editor connection
    /// </summary>
    public void UnregisterEditor(string connectionId)
    {
        if (_editors.TryRemove(connectionId, out var metadata))
        {
            _logger.LogInformation(
                "Unity Editor unregistered: {ConnectionId} - {DisplayName}",
                connectionId,
                metadata.DisplayName);

            // Clear any sessions using this editor
            var sessionsToRemove = _sessionEditors
                .Where(kvp => kvp.Value == connectionId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var sessionId in sessionsToRemove)
            {
                _sessionEditors.TryRemove(sessionId, out _);
                _logger.LogWarning(
                    "Cleared editor selection for session {SessionId} (editor disconnected)",
                    sessionId);
            }

            EditorDisconnected?.Invoke(metadata);
        }
    }

    /// <summary>
    /// Update editor metadata (e.g., when scene changes)
    /// </summary>
    public void UpdateEditor(string connectionId, EditorMetadata metadata)
    {
        metadata.ConnectionId = connectionId;

        // Use atomic AddOrUpdate pattern for thread safety
        _editors.AddOrUpdate(
            connectionId,
            // Add function (shouldn't be called if editor exists)
            metadata,
            // Update function - preserve original connection time
            (key, existing) =>
            {
                metadata.ConnectedAt = existing.ConnectedAt;
                return metadata;
            });

        _logger.LogInformation(
            "Unity Editor metadata updated: {ConnectionId} - {DisplayName}",
            connectionId,
            metadata.DisplayName);
    }

    /// <summary>
    /// Get all connected Unity Editors
    /// </summary>
    public IReadOnlyCollection<EditorMetadata> GetAllEditors()
    {
        return _editors.Values.ToList();
    }

    /// <summary>
    /// Get editor metadata by connection ID
    /// </summary>
    public EditorMetadata? GetEditor(string connectionId)
    {
        _editors.TryGetValue(connectionId, out var metadata);
        return metadata;
    }

    /// <summary>
    /// Select which Unity editor an MCP session should use
    /// </summary>
    public bool SelectEditorForSession(string sessionId, string editorConnectionId)
    {
        if (!_editors.ContainsKey(editorConnectionId))
        {
            _logger.LogWarning(
                "Cannot select editor {EditorId} for session {SessionId}: editor not found",
                editorConnectionId,
                sessionId);
            return false;
        }

        _sessionEditors[sessionId] = editorConnectionId;
        _logger.LogInformation(
            "Session {SessionId} now using editor {EditorId}",
            sessionId,
            editorConnectionId);
        return true;
    }

    /// <summary>
    /// Get the selected Unity editor for an MCP session
    /// </summary>
    public string? GetSelectedEditorForSession(string sessionId)
    {
        _sessionEditors.TryGetValue(sessionId, out var editorId);
        return editorId;
    }

    /// <summary>
    /// Get the selected Unity editor for the current MCP session (using AsyncLocal context)
    /// </summary>
    public string? GetSelectedEditorForCurrentSession()
    {
        var sessionId = McpSessionContext.CurrentSessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        return GetSelectedEditorForSession(sessionId);
    }

    /// <summary>
    /// Smart auto-selection:
    /// - If only one editor exists, auto-select it for this session
    /// - Returns selected editor ID or null if selection required
    /// </summary>
    public string? GetOrAutoSelectEditor(string sessionId)
    {
        // Check if session already has a selection
        if (_sessionEditors.TryGetValue(sessionId, out var selectedId))
        {
            // Verify the editor is still connected
            if (_editors.ContainsKey(selectedId))
            {
                return selectedId;
            }
            else
            {
                // Editor disconnected, clear selection
                _sessionEditors.TryRemove(sessionId, out _);
            }
        }

        // Auto-select if only one editor exists
        var editors = _editors.Keys.ToList();
        if (editors.Count == 1)
        {
            var autoSelectedId = editors[0];

            // Verify editor still exists before selecting (race condition protection)
            if (_editors.ContainsKey(autoSelectedId))
            {
                _sessionEditors[sessionId] = autoSelectedId;
                _logger.LogInformation(
                    "Auto-selected editor {EditorId} for session {SessionId} (only one editor available)",
                    autoSelectedId,
                    sessionId);
                return autoSelectedId;
            }
            else
            {
                _logger.LogWarning(
                    "Editor {EditorId} disconnected during auto-selection for session {SessionId}",
                    autoSelectedId,
                    sessionId);
                return null;
            }
        }

        // Multiple editors or no editors - require explicit selection
        return null;
    }

    /// <summary>
    /// Clear the editor selection for an MCP session
    /// </summary>
    public void ClearSessionEditor(string sessionId)
    {
        if (_sessionEditors.TryRemove(sessionId, out var editorId))
        {
            _logger.LogInformation(
                "Cleared editor selection for session {SessionId} (was: {EditorId})",
                sessionId,
                editorId);
        }
    }

    /// <summary>
    /// Get count of connected editors
    /// </summary>
    public int GetEditorCount() => _editors.Count;

    /// <summary>
    /// Check if a specific editor is connected
    /// </summary>
    public bool IsEditorConnected(string connectionId) => _editors.ContainsKey(connectionId);
}
