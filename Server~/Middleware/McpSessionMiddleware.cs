using UnityMcpServer.Services;

namespace UnityMcpServer.Middleware;

/// <summary>
/// Middleware that captures MCP session context and makes it available to tools via AsyncLocal.
/// Each HTTP connection is treated as a unique session.
/// </summary>
public class McpSessionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<McpSessionMiddleware> _logger;

    public McpSessionMiddleware(RequestDelegate next, ILogger<McpSessionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate a session ID from the connection
        // Use connection ID if available, otherwise use a combination of remote IP and a counter
        var connectionId = context.Connection.Id;
        var sessionId = $"mcp-session-{connectionId}";

        // Set the session context for this async call chain
        McpSessionContext.CurrentSessionId = sessionId;

        _logger.LogDebug("MCP session context set: {SessionId}", sessionId);

        try
        {
            await _next(context);
        }
        finally
        {
            // Clear the session context after the request completes
            McpSessionContext.CurrentSessionId = null;
        }
    }
}

/// <summary>
/// Extension methods for registering the McpSessionMiddleware
/// </summary>
public static class McpSessionMiddlewareExtensions
{
    public static IApplicationBuilder UseMcpSessionContext(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<McpSessionMiddleware>();
    }
}
