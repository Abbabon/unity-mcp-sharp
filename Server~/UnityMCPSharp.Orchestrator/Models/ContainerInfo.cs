namespace UnityMCPSharp.Orchestrator.Models;

/// <summary>
/// Contains information about a Docker container
/// </summary>
public class ContainerInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}
