namespace UnityMcpServer.Services;

/// <summary>
/// Provides access to the current MCP session context using AsyncLocal storage.
/// This allows tools to know which MCP client session invoked them.
/// </summary>
public static class McpSessionContext
{
    private static readonly AsyncLocal<string?> _currentSessionId = new();

    /// <summary>
    /// Gets or sets the current MCP session ID for this async context
    /// </summary>
    public static string? CurrentSessionId
    {
        get => _currentSessionId.Value;
        set => _currentSessionId.Value = value;
    }

    /// <summary>
    /// Execute an action within a specific MCP session context
    /// </summary>
    public static async Task<T> ExecuteInSessionAsync<T>(string sessionId, Func<Task<T>> action)
    {
        var previousSessionId = CurrentSessionId;
        try
        {
            CurrentSessionId = sessionId;
            return await action();
        }
        finally
        {
            CurrentSessionId = previousSessionId;
        }
    }

    /// <summary>
    /// Execute an action within a specific MCP session context
    /// </summary>
    public static async Task ExecuteInSessionAsync(string sessionId, Func<Task> action)
    {
        var previousSessionId = CurrentSessionId;
        try
        {
            CurrentSessionId = sessionId;
            await action();
        }
        finally
        {
            CurrentSessionId = previousSessionId;
        }
    }
}
