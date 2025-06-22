using CommandLine;
using UnityMcpSharp.ContainerManager;

namespace UnityMCPSharp.Orchestrator;

[Verb("start", HelpText = "Start the Unity MCP Sharp container")]
public class StartOptions
{
    [Option('n', "name", Required = false, Default = "unity-mcp-sharp-server", HelpText = "Name of the container")]
    public string ContainerName { get; set; } = "unity-mcp-sharp-server";
    
    [Option('i', "image", Required = false, Default = "ghcr.io/abbabon/unity-mcp-sharp:latest", HelpText = "Docker image to use")]
    public string ImageName { get; set; } = "ghcr.io/abbabon/unity-mcp-sharp:latest";
    
    [Option('p', "port", Required = false, Default = 3001, HelpText = "Port to expose the server on")]
    public int ServerPort { get; set; } = 3001;
}

[Verb("stop", HelpText = "Stop the Unity MCP Sharp container")]
public class StopOptions
{
    [Option('n', "name", Required = false, Default = "unity-mcp-sharp-server", HelpText = "Name of the container")]
    public string ContainerName { get; set; } = "unity-mcp-sharp-server";
}

public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("Unity MCP Sharp Container Orchestrator");
        
        return Parser.Default.ParseArguments<StartOptions, StopOptions>(args)
            .MapResult(
                (StartOptions opts) => RunStartAndReturnExitCode(opts),
                (StopOptions opts) => RunStopAndReturnExitCode(opts),
                errs => 1);
    }

    private static int RunStartAndReturnExitCode(StartOptions opts)
    {
        try
        {
            Console.WriteLine($"Starting container '{opts.ContainerName}' using image '{opts.ImageName}' on port {opts.ServerPort}");
            
            var result = DockerContainerManager.StartContainerAsync(
                opts.ContainerName, 
                opts.ImageName,
                opts.ServerPort,
                message => Console.WriteLine(message)
            ).GetAwaiter().GetResult();
            
            if (result.IsSuccess)
            {
                Console.WriteLine($"Server is accessible at http://localhost:{opts.ServerPort}");
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"Failed to start container: {result.ErrorMessage}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    private static int RunStopAndReturnExitCode(StopOptions opts)
    {
        try
        {
            Console.WriteLine($"Stopping container '{opts.ContainerName}'");
            
            var result = DockerContainerManager.StopContainerAsync(
                opts.ContainerName,
                message => Console.WriteLine(message)
            ).GetAwaiter().GetResult();
            
            if (result.IsSuccess)
            {
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"Failed to stop container: {result.ErrorMessage}");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
