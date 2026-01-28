using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using UnityMcpServer.Middleware;
using UnityMcpServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure port from UNITY_MCP_ASPPORT environment variable (default: 3727)
var port = Environment.GetEnvironmentVariable("UNITY_MCP_ASPPORT") ?? "3727";
builder.WebHost.UseUrls($"http://+:{port}");

// Configure logging - send to stderr to keep stdout clean with timestamps
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.TimestampFormat = "[HH:mm:ss.fff] ";
    options.SingleLine = true;
});

// Add CORS for Unity Editor communication
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Editor Session Manager for multi-editor support
builder.Services.AddSingleton<EditorSessionManager>();

// Add WebSocket service for Unity Editor communication
builder.Services.AddSingleton<UnityWebSocketService>();

// Add Resource Subscription service for managing MCP resource subscriptions
builder.Services.AddSingleton<ResourceSubscriptionService>();

// Note: Tool and Resource classes are automatically discovered via WithToolsFromAssembly and WithResourcesFromAssembly
// All classes with [McpServerToolType] and [McpServerResourceType] attributes will be registered

// Add MCP server (creates HTTP endpoints via MapMcp)
builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        // Never expire sessions - they live forever until explicitly closed
        options.IdleTimeout = Timeout.InfiniteTimeSpan;
        // Allow more idle sessions
        options.MaxIdleSessionCount = 1000;
    })
    .WithToolsFromAssembly(typeof(Program).Assembly)
    .WithResourcesFromAssembly(typeof(Program).Assembly)
    // Filter tools based on connected Unity editor's tool profile preference
    .AddListToolsFilter(next => async (context, cancellationToken) =>
    {
        var result = await next(context, cancellationToken);

        // Get the tool profile from the current session's Unity editor
        var sessionManager = context.Services!.GetRequiredService<EditorSessionManager>();
        var profile = sessionManager.GetCurrentSessionEditorProfile();
        var enabledTools = ToolProfileService.GetToolsForProfile(profile);

        // Filter tools if a profile filter is active (null means allow all)
        if (enabledTools != null && result.Tools != null)
        {
            result = new ListToolsResult
            {
                Tools = result.Tools.Where(t => enabledTools.Contains(t.Name)).ToList(),
                NextCursor = result.NextCursor
            };
        }

        return result;
    });

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();

// Add MCP session context middleware (must be before MapMcp)
app.UseMcpSessionContext();

// Map MCP endpoints with /mcp prefix (Streamable HTTP protocol)
app.MapMcp("/mcp");

// Enable WebSockets
app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// WebSocket endpoint for Unity Editor
app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        var webSocketService = context.RequestServices.GetRequiredService<UnityWebSocketService>();
        await webSocketService.HandleWebSocketAsync(context);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("WebSocket connection required");
    }
});

// Health check endpoint
app.MapHealthChecks("/health");

// Root endpoint with server info
app.MapGet("/", () => new
{
    name = "Unity MCP Server",
    version = "0.5.0",
    transports = "http + websocket",
    endpoints = new
    {
        mcp = "/mcp (HTTP - Streamable HTTP protocol for Claude Code, Cursor, etc.)",
        websocket = "/ws (WebSocket - for Unity Editor)",
        health = "/health"
    },
    status = "running",
    message = "Unity-managed server: Unity starts container, LLMs connect via HTTP"
});

// Start server
await app.RunAsync();
