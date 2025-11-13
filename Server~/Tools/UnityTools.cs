using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools;

/// <summary>
/// Unity Editor tools exposed via MCP protocol
/// </summary>
[McpServerToolType]
public class UnityTools(ILogger<UnityTools> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<UnityTools> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Get recent console logs from Unity Editor. Returns error messages, warnings, and debug logs from the Unity Console window. Useful for debugging runtime issues and monitoring Unity's output.")]
    [return: Description("Recent console logs from Unity Editor including errors, warnings, and info messages")]
    public async Task<string> UnityGetConsoleLogsAsync()
    {
        _logger.LogInformation("Requesting console logs from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<ConsoleLogsResponse>("unity.getConsoleLogs", null);
            if (response?.Logs != null && response.Logs.Count > 0)
            {
                return string.Join("\n", response.Logs.Select(log =>
                    $"[{log.Type}] {log.Message}" + (string.IsNullOrEmpty(log.StackTrace) ? "" : $"\n{log.StackTrace}")));
            }
            return "No console logs available.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

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

    [McpServerTool]
    [Description("Force Unity to recompile all C# scripts. Use this after making code changes or when experiencing compilation issues. Unity will reload assemblies and report any compilation errors.")]
    public async Task UnityTriggerScriptCompilationAsync()
    {
        _logger.LogInformation("Triggering Unity script compilation...");

        await _webSocketService.BroadcastNotificationAsync("unity.triggerCompilation", null);
    }

    [McpServerTool]
    [Description("Check if Unity is currently compiling scripts. Returns whether compilation is in progress and if the last compilation succeeded or failed. Useful before running play mode or making additional code changes.")]
    [return: Description("Current compilation status: whether Unity is compiling and if last compilation succeeded")]
    public async Task<string> UnityGetCompilationStatusAsync()
    {
        _logger.LogInformation("Requesting compilation status from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<CompilationStatusResponse>("unity.getCompilationStatus", null);
            if (response != null)
            {
                var status = response.IsCompiling ? "Compiling..." : "Idle";
                var lastResult = response.LastCompilationSucceeded ? "succeeded" : "failed";
                return $"Compilation Status: {status}\nLast Compilation: {lastResult}";
            }
            return "Unable to retrieve compilation status.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public class CompilationStatusResponse
    {
        public bool IsCompiling { get; set; }
        public bool LastCompilationSucceeded { get; set; }
    }

    [McpServerTool]
    [Description("Create a new GameObject in the currently active Unity scene. You can specify its name, 3D position, components to add (e.g., 'Rigidbody,BoxCollider'), and parent object. The GameObject will be selected in the Hierarchy after creation.")]
    public async Task UnityCreateGameObjectAsync(
        [Description("Name of the GameObject to create")] string name,
        [Description("X position in world space (default: 0)")] float x = 0,
        [Description("Y position in world space (default: 0)")] float y = 0,
        [Description("Z position in world space (default: 0)")] float z = 0,
        [Description("Comma-separated list of Unity components to add (e.g., 'Rigidbody,BoxCollider,AudioSource')")] string? components = null,
        [Description("Name of parent GameObject in hierarchy (optional, leave empty for root level)")] string? parent = null)
    {
        _logger.LogInformation("Creating GameObject: {Name}", name);

        var parameters = new
        {
            name,
            position = new { x, y, z },
            components = components?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            parent
        };

        await _webSocketService.BroadcastNotificationAsync("unity.createGameObject", parameters);
    }

    [McpServerTool]
    [Description("Get the complete GameObject hierarchy of the currently active Unity scene. Returns all GameObjects with their parent-child relationships, showing which objects are active or inactive. Useful for understanding scene structure.")]
    [return: Description("Hierarchical list of all GameObjects in the active scene with their active/inactive state")]
    public async Task<string> UnityListSceneObjectsAsync()
    {
        _logger.LogInformation("Requesting scene objects from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<SceneObjectsResponse>("unity.listSceneObjects", null);
            if (response?.Objects != null && response.Objects.Count > 0)
            {
                return string.Join("\n", response.Objects.Select(obj =>
                    $"{new string(' ', obj.Depth * 2)}{(obj.IsActive ? "✓" : "✗")} {obj.Name}"));
            }
            return "No GameObjects in scene or scene is empty.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

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

    [McpServerTool]
    [Description("Get metadata about the Unity project including project name, Unity version, currently active scene name and path, data directory path, and whether the Editor is in play mode or paused. Useful for context about the project environment.")]
    [return: Description("Project information: name, Unity version, active scene, paths, and editor state")]
    public async Task<string> UnityGetProjectInfoAsync()
    {
        _logger.LogInformation("Requesting project info from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<ProjectInfoResponse>("unity.getProjectInfo", null);
            if (response != null)
            {
                return $"Project Name: {response.ProjectName}\n" +
                       $"Unity Version: {response.UnityVersion}\n" +
                       $"Active Scene: {response.ActiveScene}\n" +
                       $"Scene Path: {response.ScenePath}\n" +
                       $"Data Path: {response.DataPath}\n" +
                       $"Is Playing: {response.IsPlaying}\n" +
                       $"Is Paused: {response.IsPaused}";
            }
            return "Unable to retrieve project info.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }

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
}
