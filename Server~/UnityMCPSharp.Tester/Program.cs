using CommandLine;
using UnityMCPSharp.Orchestrator;
using UnityMCPSharp.Orchestrator.Models;
using UnityMCPSharp.Orchestrator.Options;

namespace UnityMCPSharp.Tester
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Console.WriteLine("Unity MCP Sharp Container Orchestrator");
            
            return await Parser.Default.ParseArguments<OrchestratorStartOptions, OrchestratorStopOptions>(args)
                .MapResult(
                    async (OrchestratorStartOptions opts) => await HandleStartResult(await DockerContainerManager.RunStartAndReturnExitCode(opts), opts),
                    async (OrchestratorStopOptions opts) => await HandleStopResult(await DockerContainerManager.RunStopAndReturnExitCode(opts)),
                    _ => Task.FromResult(1));
        }
        
        private static Task<int> HandleStartResult(ContainerOperationResult result, OrchestratorStartOptions opts)
        {
            if (result.IsSuccess)
            {
                Console.WriteLine($"Container operation successful. Status: {result.Status}");
                
                if (result.Status == ContainerStatus.Started || result.Status == ContainerStatus.AlreadyRunning)
                {
                    Console.WriteLine($"MCP Server is accessible at http://localhost:{opts.ServerPort}");
                    Console.WriteLine($"Unity Bridge is available on port {opts.UnityBridgePort}");
                }
                
                return Task.FromResult(0);
            }
            else
            {
                Console.Error.WriteLine($"Container operation failed. Status: {result.Status}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                }
                return Task.FromResult(1);
            }
        }
        
        private static Task<int> HandleStopResult(ContainerOperationResult result)
        {
            if (result.IsSuccess)
            {
                Console.WriteLine($"Container successfully stopped.");
                return Task.FromResult(0);
            }
            else
            {
                Console.Error.WriteLine($"Failed to stop container. Status: {result.Status}");
                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.Error.WriteLine($"Error: {result.ErrorMessage}");
                }
                return Task.FromResult(1);
            }
        }
    }
}