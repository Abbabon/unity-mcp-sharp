namespace UnityMCPSharp.Orchestrator.Models
{
    /// <summary>
    /// Represents the status of a container operation
    /// </summary>
    public enum ContainerStatus
    {
        Created,
        Started,
        AlreadyRunning,
        Stopped,
        NotRunning,
        NotFound,
        Error
    }
}