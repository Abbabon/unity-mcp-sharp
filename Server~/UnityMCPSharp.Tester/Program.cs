using CommandLine;
using UnityMCPSharp.Orchestrator;
using UnityMCPSharp.Orchestrator.Models;
using UnityMCPSharp.Orchestrator.Options;

namespace UnityMCPSharp.Tester;

public class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine("Unity MCP Sharp Container Orchestrator");
        
        return Parser.Default.ParseArguments<OrchestratorStartOptions, OrchestratorStopOptions>(args)
            .MapResult(
                (OrchestratorStartOptions opts) => HandleStartResult(DockerContainerManager.RunStartAndReturnExitCode(opts), opts),
                (OrchestratorStopOptions opts) => HandleStopResult(DockerContainerManager.RunStopAndReturnExitCode(opts)),
                errs => 1);
    }
    
    private static int HandleStartResult(ContainerOperationResult result, OrchestratorStartOptions opts)
    {
        if (result.IsSuccess)
        {
            Console.WriteLine($"Container operation successful. Status: {result.Status}");
            
            if (result.Status == ContainerStatus.Started || result.Status == ContainerStatus.AlreadyRunning)
            {
                Console.WriteLine($"MCP Server is accessible at http://localhost:{opts.ServerPort}");
                Console.WriteLine($"Unity Bridge is available on port {opts.UnityBridgePort}");
            }
            
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"Container operation failed. Status: {result.Status}");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            }
            return 1;
        }
    }
    
    private static int HandleStopResult(ContainerOperationResult result)
    {
        if (result.IsSuccess)
        {
            Console.WriteLine($"Container successfully stopped.");
            return 0;
        }
        else
        {
            Console.Error.WriteLine($"Failed to stop container. Status: {result.Status}");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            }
            return 1;
        }
    }
}