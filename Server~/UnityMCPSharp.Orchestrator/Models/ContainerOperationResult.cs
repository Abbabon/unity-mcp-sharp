namespace UnityMCPSharp.Orchestrator.Models;

/// <summary>
/// Represents the result of a container operation
/// </summary>
public class ContainerOperationResult
{
    public ContainerStatus Status { get; set; }
    public string? ContainerId { get; set; }
    public string? ErrorMessage { get; set; }
    
    public bool IsSuccess => 
        Status == ContainerStatus.Created || 
        Status == ContainerStatus.Started || 
        Status == ContainerStatus.AlreadyRunning ||
        Status == ContainerStatus.Stopped;
}