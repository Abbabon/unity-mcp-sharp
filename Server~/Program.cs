using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

var builder = WebApplication.CreateBuilder(args);

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

// Add WebSocket service for Unity Editor communication
builder.Services.AddSingleton<UnityWebSocketService>();

// Note: Tool classes are automatically discovered via WithToolsFromAssembly below
// All classes with [McpServerToolType] attribute will be registered

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
    .WithToolsFromAssembly(typeof(Program).Assembly);

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseCors();

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
    version = "0.3.0",
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
