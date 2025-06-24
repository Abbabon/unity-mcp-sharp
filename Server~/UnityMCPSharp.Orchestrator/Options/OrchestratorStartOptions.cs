using CommandLine;

namespace UnityMCPSharp.Orchestrator.Options
{
    [Verb("start", HelpText = "Start the Unity MCP Sharp container")]
    public class OrchestratorStartOptions
    {
        [Option('n', "name", Required = false, Default = "unity-mcp-sharp-server", HelpText = "Name of the container")]
        public string ContainerName { get; set; } = "unity-mcp-sharp-server";
    
        [Option('i', "image", Required = false, Default = "ghcr.io/abbabon/unity-mcp-sharp:latest", HelpText = "Docker image to use")]
        public string ImageName { get; set; } = "ghcr.io/abbabon/unity-mcp-sharp:latest";
    
        [Option('p', "port", Required = false, Default = 3001, HelpText = "Port to expose the MCP server on")]
        public int ServerPort { get; set; } = 3001;
    
        [Option('b', "bridge-port", Required = false, Default = 8090, HelpText = "Port to expose the Unity Bridge on")]
        public int UnityBridgePort { get; set; } = 8090;
    }
}