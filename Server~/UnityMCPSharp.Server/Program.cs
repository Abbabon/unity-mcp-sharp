namespace UnityMCPSharp.Server;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Connect to Unity with timeout handling using the singleton instance
            try
            {
                var connected = await UnityBridgeClient.Instance.ConnectAsync();
                Console.WriteLine
                (connected ? 
                    "Successfully connected to Unity Editor" : 
                    "Connection to Unity Editor failed");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Warning: Connection to Unity Editor timed out. Server will start without Unity connection.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to connect to Unity Editor: {ex.Message}. Server will start without Unity connection.");
            }

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddMcpServer()
                .WithHttpTransport()
                .WithToolsFromAssembly()
                .WithResourcesFromAssembly();
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

            Console.WriteLine($"Starting MCP server on port {serverPort}");
            await app.RunAsync($"http://0.0.0.0:{serverPort}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Fatal error: {ex}");
            Environment.Exit(1);
        }
    }
}