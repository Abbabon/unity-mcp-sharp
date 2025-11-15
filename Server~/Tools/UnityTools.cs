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

    [McpServerTool]
    [Description("Create a new C# MonoBehaviour script file in the Unity project. The script will be created in the Assets folder and will trigger automatic Unity recompilation.")]
    public async Task UnityCreateScriptAsync(
        [Description("Name of the script (without .cs extension)")] string scriptName,
        [Description("Relative path within Assets folder (e.g., 'Scripts' or 'Scripts/Player')")] string folderPath,
        [Description("C# script content (full MonoBehaviour class code)")] string scriptContent)
    {
        _logger.LogInformation("Creating script: {ScriptName} in {FolderPath}", scriptName, folderPath);

        var parameters = new
        {
            scriptName,
            folderPath,
            scriptContent
        };

        await _webSocketService.BroadcastNotificationAsync("unity.createScript", parameters);
    }

    [McpServerTool]
    [Description("Add a component to an existing GameObject in the scene. Can add Unity built-in components or custom MonoBehaviour scripts.")]
    public async Task UnityAddComponentToObjectAsync(
        [Description("Name of the GameObject to add the component to")] string gameObjectName,
        [Description("Component type name (e.g., 'Rigidbody', 'BoxCollider', or your custom script name)")] string componentType)
    {
        _logger.LogInformation("Adding component {ComponentType} to GameObject {GameObjectName}", componentType, gameObjectName);

        var parameters = new
        {
            gameObjectName,
            componentType
        };

        await _webSocketService.BroadcastNotificationAsync("unity.addComponent", parameters);
    }

    [McpServerTool]
    [Description("Enter Unity play mode (start running the game/scene). Equivalent to pressing the Play button in Unity Editor.")]
    public async Task UnityEnterPlayModeAsync()
    {
        _logger.LogInformation("Entering Unity play mode");

        await _webSocketService.BroadcastNotificationAsync("unity.enterPlayMode", null);
    }

    [McpServerTool]
    [Description("Exit Unity play mode (stop running the game/scene). Equivalent to pressing the Stop button in Unity Editor.")]
    public async Task UnityExitPlayModeAsync()
    {
        _logger.LogInformation("Exiting Unity play mode");

        await _webSocketService.BroadcastNotificationAsync("unity.exitPlayMode", null);
    }

    [McpServerTool]
    [Description("Get the current play mode state of Unity Editor. Returns whether Unity is currently Playing, Paused, or Stopped.")]
    [return: Description("Current play mode state: Playing, Paused, or Stopped")]
    public async Task<string> UnityGetPlayModeStateAsync()
    {
        _logger.LogInformation("Requesting play mode state from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestAsync<PlayModeStateResponse>("unity.getPlayModeState", null);
            if (response != null)
            {
                return $"Play Mode State: {response.State}";
            }
            return "Unable to retrieve play mode state.";
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

    public class PlayModeStateResponse
    {
        public string State { get; set; } = string.Empty;
    }

    // ========== NEW UTILITY TOOLS ==========

    [McpServerTool]
    [Description("Refresh Unity Asset Database to detect file changes. Use this after batch file operations or when changes aren't detected automatically. Triggers Unity to reimport assets and recompile scripts.")]
    public async Task UnityRefreshAssetsAsync()
    {
        _logger.LogInformation("Refreshing Unity Asset Database");

        await _webSocketService.BroadcastNotificationAsync("unity.refreshAssets", null);
    }

    [McpServerTool]
    [Description("Create multiple GameObjects in a single operation. More efficient than creating them one by one. Each GameObject can have its own position, components, and parent.")]
    public async Task UnityBatchCreateGameObjectsAsync(
        [Description("JSON array of GameObject specifications. Each should have: name, position {x,y,z}, components (comma-separated), parent")] string gameObjectsJson)
    {
        _logger.LogInformation("Batch creating GameObjects");

        var parameters = new
        {
            gameObjectsJson
        };

        await _webSocketService.BroadcastNotificationAsync("unity.batchCreateGameObjects", parameters);
    }

    [McpServerTool]
    [Description("Find a GameObject by name, tag, or path and return detailed information about it including position, rotation, scale, active state, and all attached components.")]
    [return: Description("GameObject information including transform, active state, and components")]
    public async Task<string> UnityFindGameObjectAsync(
        [Description("Name of the GameObject to find")] string name,
        [Description("Search by: 'name' (default), 'tag', or 'path'. Path format: 'Parent/Child/Object'")] string searchBy = "name")
    {
        _logger.LogInformation("Finding GameObject: {Name} by {SearchBy}", name, searchBy);

        try
        {
            var parameters = new
            {
                name,
                searchBy
            };

            var response = await _webSocketService.SendRequestAsync<GameObjectInfoResponse>("unity.findGameObject", parameters);
            if (response != null)
            {
                var info = $"Name: {response.Name}\n" +
                          $"Path: {response.Path}\n" +
                          $"Active: {response.IsActive}\n" +
                          $"Position: ({response.Position.X:F2}, {response.Position.Y:F2}, {response.Position.Z:F2})\n" +
                          $"Rotation: ({response.Rotation.X:F2}, {response.Rotation.Y:F2}, {response.Rotation.Z:F2})\n" +
                          $"Scale: ({response.Scale.X:F2}, {response.Scale.Y:F2}, {response.Scale.Z:F2})\n" +
                          $"Components: {string.Join(", ", response.Components)}";
                return info;
            }
            return "GameObject not found.";
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

    // ========== SCENE MANAGEMENT TOOLS ==========

    [McpServerTool]
    [Description("List all Unity scene files (.unity) in the project. Returns scene paths relative to the project root. Useful for discovering available scenes before opening them.")]
    [return: Description("List of all scene file paths in the project")]
    public async Task<string> UnityListScenesAsync()
    {
        _logger.LogInformation("Listing all scenes in project");

        try
        {
            var response = await _webSocketService.SendRequestAsync<SceneListResponse>("unity.listScenes", null);
            if (response?.Scenes != null && response.Scenes.Count > 0)
            {
                return string.Join("\n", response.Scenes);
            }
            return "No scenes found in project.";
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

    public class SceneListResponse
    {
        public List<string> Scenes { get; set; } = new();
    }

    [McpServerTool]
    [Description("Open a Unity scene by path. Can open additively (keeping current scenes open) or single mode (closing all other scenes). Scene path should be relative to Assets folder.")]
    public async Task UnityOpenSceneAsync(
        [Description("Path to scene file relative to Assets folder (e.g., 'Scenes/Level1.unity')")] string scenePath,
        [Description("If true, opens scene additively without closing current scenes. If false (default), closes all other scenes first.")] bool additive = false)
    {
        _logger.LogInformation("Opening scene: {ScenePath} (additive: {Additive})", scenePath, additive);

        var parameters = new
        {
            scenePath,
            additive
        };

        await _webSocketService.BroadcastNotificationAsync("unity.openScene", parameters);
    }

    [McpServerTool]
    [Description("Close a specific Unity scene. Only works when multiple scenes are open. Cannot close the last open scene. Scene must be identified by its path or name.")]
    public async Task UnityCloseSceneAsync(
        [Description("Name or path of the scene to close")] string sceneIdentifier)
    {
        _logger.LogInformation("Closing scene: {SceneIdentifier}", sceneIdentifier);

        var parameters = new
        {
            sceneIdentifier
        };

        await _webSocketService.BroadcastNotificationAsync("unity.closeScene", parameters);
    }

    [McpServerTool]
    [Description("Get information about the currently active Unity scene. The active scene is where new GameObjects are created by default. Returns scene name, path, isDirty status, and GameObject count.")]
    [return: Description("Active scene information including name, path, dirty state, and object count")]
    public async Task<string> UnityGetActiveSceneAsync()
    {
        _logger.LogInformation("Getting active scene info");

        try
        {
            var response = await _webSocketService.SendRequestAsync<ActiveSceneResponse>("unity.getActiveScene", null);
            if (response != null)
            {
                return $"Scene Name: {response.Name}\n" +
                       $"Scene Path: {response.Path}\n" +
                       $"Is Dirty: {response.IsDirty}\n" +
                       $"Root GameObject Count: {response.RootCount}\n" +
                       $"Is Loaded: {response.IsLoaded}";
            }
            return "Unable to get active scene.";
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

    public class ActiveSceneResponse
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirty { get; set; }
        public int RootCount { get; set; }
        public bool IsLoaded { get; set; }
    }

    [McpServerTool]
    [Description("Set which scene should be the active scene. The active scene is where new GameObjects are created. Only works when multiple scenes are open. Scene must be already loaded.")]
    public async Task UnitySetActiveSceneAsync(
        [Description("Name or path of the scene to make active")] string sceneIdentifier)
    {
        _logger.LogInformation("Setting active scene: {SceneIdentifier}", sceneIdentifier);

        var parameters = new
        {
            sceneIdentifier
        };

        await _webSocketService.BroadcastNotificationAsync("unity.setActiveScene", parameters);
    }

    [McpServerTool]
    [Description("Save the currently active scene or a specific scene by path. Can save just the specified scene or all open scenes. Returns success/failure status.")]
    public async Task UnitySaveSceneAsync(
        [Description("Path to specific scene to save, or null/empty to save active scene")] string? scenePath = null,
        [Description("If true, saves all currently open scenes. If false (default), saves only the specified/active scene.")] bool saveAll = false)
    {
        _logger.LogInformation("Saving scene: {ScenePath} (saveAll: {SaveAll})", scenePath ?? "active", saveAll);

        var parameters = new
        {
            scenePath,
            saveAll
        };

        await _webSocketService.BroadcastNotificationAsync("unity.saveScene", parameters);
    }

    [McpServerTool]
    [Description("Create a GameObject in a specific scene (not necessarily the active one). Requires the scene to be loaded. If scene is not loaded, it will be opened additively first.")]
    public async Task UnityCreateGameObjectInSceneAsync(
        [Description("Path to the scene where the GameObject should be created (e.g., 'Scenes/Level1.unity')")] string scenePath,
        [Description("Name of the GameObject to create")] string name,
        [Description("X position in world space (default: 0)")] float x = 0,
        [Description("Y position in world space (default: 0)")] float y = 0,
        [Description("Z position in world space (default: 0)")] float z = 0,
        [Description("Comma-separated list of Unity components to add (e.g., 'Rigidbody,BoxCollider,AudioSource')")] string? components = null,
        [Description("Name of parent GameObject in hierarchy (optional, leave empty for root level)")] string? parent = null)
    {
        _logger.LogInformation("Creating GameObject '{Name}' in scene '{ScenePath}'", name, scenePath);

        var parameters = new
        {
            scenePath,
            name,
            position = new { x, y, z },
            components = components?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            parent
        };

        await _webSocketService.BroadcastNotificationAsync("unity.createGameObjectInScene", parameters);
    }
}
