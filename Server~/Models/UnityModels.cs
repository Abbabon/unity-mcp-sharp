namespace UnityMcpServer.Models;

/// <summary>
/// Shared response models for Unity MCP tools
/// </summary>

// Console logs models
public class ConsoleLogsResponse
{
    public List<LogEntry>? Logs { get; set; }
}

public class LogEntry
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}

// Compilation models
public class CompilationStatusResponse
{
    public bool IsCompiling { get; set; }
    public bool LastCompilationSucceeded { get; set; }
}

// Project info models
public class ProjectInfoResponse
{
    public string ProjectName { get; set; } = string.Empty;
    public string UnityVersion { get; set; } = string.Empty;
    public string ActiveScene { get; set; } = string.Empty;
    public string ScenePath { get; set; } = string.Empty;
    public string DataPath { get; set; } = string.Empty;
    public bool IsPlaying { get; set; }
    public bool IsPaused { get; set; }
}

// Play mode models
public class PlayModeStateResponse
{
    public string State { get; set; } = string.Empty;
}

// Scene objects models
public class SceneObjectsResponse
{
    public List<SceneObject>? Objects { get; set; }
}

public class SceneObject
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int Depth { get; set; }
}

// GameObject info models
public class GameObjectInfoResponse
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Vector3Data Position { get; set; } = new();
    public Vector3Data Rotation { get; set; } = new();
    public Vector3Data Scale { get; set; } = new();
    public List<string> Components { get; set; } = new();
}

public class Vector3Data
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
}

// Scene management models
public class SceneListResponse
{
    public List<string> Scenes { get; set; } = new();
}

public class ActiveSceneResponse
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool IsDirty { get; set; }
    public int RootCount { get; set; }
    public bool IsLoaded { get; set; }
}
