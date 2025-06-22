using ModelContextProtocol.Server;
using System.ComponentModel;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
var app = builder.Build();

app.MapMcp();

// Get port from configuration
var serverPort = app.Configuration.GetValue<int>("ServerSettings:Port");

// Allow overriding from environment variable
var portEnv = Environment.GetEnvironmentVariable("SERVER_PORT");
if (!string.IsNullOrEmpty(portEnv) && int.TryParse(portEnv, out int envPort))
{
    serverPort = envPort;
}

app.Run($"http://0.0.0.0:{serverPort}");

[McpServerToolType]
public static class EchoTool
{
    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message) => $"hello {message}";
}
