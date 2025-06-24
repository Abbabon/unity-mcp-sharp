using CommandLine;

namespace UnityMCPSharp.Orchestrator.Options;

[Verb("stop", HelpText = "Stop the Unity MCP Sharp container")]
public class OrchestratorStopOptions
{
    [Option('n', "name", Required = false, Default = "unity-mcp-sharp-server", HelpText = "Name of the container")]
    public string ContainerName { get; set; } = "unity-mcp-sharp-server";
}