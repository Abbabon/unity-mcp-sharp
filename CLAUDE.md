# Unity MCP Server - Development Guide

This document provides development guidelines, workflow instructions, and architectural details for contributors and AI assistants working on the Unity MCP Server project.

## Project Overview

Unity MCP Server is a **dual-purpose project**:

1. **Unity Package** (UPM) - Installed in Unity projects via OpenUPM
2. **MCP Server** (Dockerized .NET application) - Runs in a container, communicates with Unity

### Key Technologies

- **.NET 9.0** - Server runtime
- **ASP.NET Core** - Web framework for SSE and WebSocket
- **Model Context Protocol SDK** - Official C# MCP implementation
- **Unity 2021.3+** - Target Unity version
- **UIToolkit** - Unity's modern UI framework
- **Docker** - Containerization
- **GitHub Actions** - CI/CD pipeline

## Repository Structure

```
unity-mcp-sharp/
├── .github/
│   └── workflows/           # CI/CD pipelines
│       ├── build-server.yml
│       ├── publish-docker.yml
│       └── publish-openupm.yml
│
├── Server~/                 # MCP Server (excluded from Unity package via ~)
│   ├── Models/              # JSON-RPC message models
│   ├── Services/            # WebSocket service, handlers
│   ├── Tools/               # MCP tool implementations
│   ├── Program.cs           # Server entry point
│   ├── Dockerfile
│   ├── docker-compose.yml
│   └── UnityMcpServer.csproj
│
├── Scripts~/                # Development scripts (excluded from Unity package via ~)
│   ├── rebuild.sh           # Build .NET server + Docker image
│   ├── sign-package.sh      # Sign UPM package for Unity 6+
│   ├── start-mcp-server.sh  # Start MCP server container
│   └── test.sh              # Smoke test script
│
├── Runtime/                 # Unity runtime scripts
│   ├── Scripts/
│   │   ├── MCPClient.cs     # WebSocket JSON-RPC client
│   │   ├── MCPServerManager.cs  # Docker lifecycle management
│   │   └── MCPConfiguration.cs  # ScriptableObject config
│   └── UnityMCPSharp.asmdef
│
├── Editor/                  # Unity Editor scripts
│   ├── Scripts/
│   │   ├── MCPDashboard.cs      # UIToolkit EditorWindow
│   │   ├── MCPEditorIntegration.cs  # Handles MCP requests
│   │   ├── MCPMenuItems.cs
│   │   └── DockerSetupWizard.cs
│   ├── UI/                  # UIToolkit assets (UXML, USS)
│   └── UnityMCPSharp.Editor.asmdef
│
├── Documentation~/          # User documentation (excluded from package)
│   ├── Installation.md
│   ├── Configuration.md
│   ├── PackageSigning.md    # Unity 6+ package signing guide
│   ├── Troubleshooting.md
│   ├── Testing.md
│   └── MultiEditor-TechnicalRundown.md  # Technical deep-dive docs
│
├── TestProject~/            # Test Unity project (excluded from package)
│
├── package.json             # Unity UPM manifest
├── README.md                # Main project readme
├── CHANGELOG.md             # Version history
├── CLAUDE.md                # This file - AI assistant instructions
└── LICENSE
```

**Note on folder naming convention:**
- Folders ending with `~/` are excluded from the Unity package
- `Server~/` - .NET MCP server code
- `Scripts~/` - Development/build scripts
- `Documentation~/` - Extended documentation
- `TestProject~/` - Local testing Unity project

## Architecture Details

### Communication Flow

```
LLM → MCP Client (IDE) → [HTTP/SSE] → MCP Server → [WebSocket/JSON-RPC] → Unity Package → Unity Editor APIs
```

### Dual Transport Design

The server supports **two transports simultaneously**:

1. **HTTP (Streamable HTTP)** at `/mcp`
   - For MCP protocol communication with IDEs (Claude Code, Cursor, etc.)
   - Uses the official MCP SDK (ModelContextProtocol.AspNetCore)
   - Supports both modern Streamable HTTP and legacy SSE for backward compatibility

2. **WebSocket** at `/ws`
   - For bidirectional communication with Unity Editor
   - JSON-RPC 2.0 protocol for consistency
   - Persistent connection for real-time updates

### Multi-Editor Support (v0.5.0+)

The server supports **multiple Unity Editor instances** connecting simultaneously:

**Architecture:**
- Each Unity Editor registers with unique metadata (project name, scene, machine, process ID)
- Each MCP session (HTTP connection) can select a different Unity Editor
- Session-to-editor mapping persists across Unity compilation reconnects
- Smart auto-selection: single editor auto-selected, multiple editors require explicit selection

**Key Components:**
- `EditorSessionManager` - Manages session-to-editor mappings using `ConcurrentDictionary`
- `McpSessionContext` - AsyncLocal storage for MCP session IDs (propagates through async chains)
- `McpSessionMiddleware` - ASP.NET middleware captures HTTP connection ID as session ID
- `EditorMetadata` - Rich metadata model for Unity Editor instances

**Session Isolation:**
```
MCP Session A → Unity Editor 1 (ProjectX, SceneA)
MCP Session B → Unity Editor 2 (ProjectY, SceneB)
MCP Session C → Unity Editor 1 (same as Session A)
```

**New MCP Tools:**
- `unity_list_editors` - List all connected editors with metadata
- `unity_select_editor` - Select which editor to use for current session

### Request Handling Pattern

1. **MCP Tool Invocation** (from IDE)
   - Tool called via MCP endpoint
   - Server routes JSON-RPC notification to selected Unity Editor via WebSocket
   - Unity executes Editor API calls
   - Unity sends result back via WebSocket
   - Server returns MCP tool result

2. **Unity Events** (from Unity Editor)
   - Unity detects event (log message, compilation, etc.)
   - Unity sends JSON-RPC notification to server
   - Server can optionally broadcast to connected MCP clients

## Development Workflow

### Setting Up Development Environment

#### Server Development

```bash
# Navigate to server directory
cd Server~

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run locally (no Docker)
dotnet run

# Server will be available at http://localhost:3727
```

#### Unity Package Development

1. Open any Unity project (2021.3+)
2. Clone this repository
3. In Unity Package Manager:
   - Click `+` → "Add package from disk..."
   - Select the `package.json` in repository root

**Note:** Changes to scripts will hot-reload in Unity

### Adding New MCP Tools

**IMPORTANT:** All MCP tools must follow best practices for LLM interaction. See "MCP Tool Best Practices" section below before implementing new tools.

#### Step 1: Define the Tool (Server~/Tools/Category/YourTool.cs)

Create a new file in the appropriate category folder (GameObjects/, Scenes/, Assets/, System/).

```csharp
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Category;

[McpServerToolType]
public class YourTool(ILogger<YourTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<YourTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Clear description of what this tool does. Mention side effects. Suggest related tools to use next: unity_related_tool_1, unity_related_tool_2.")]
    [return: Description("What the tool returns - be specific")]
    public async Task<string> UnityYourToolNameAsync(
        [Description("Parameter description with examples (e.g., 'value1', 'value2')")] string param1,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing your tool with param: {Param1}", param1);

        await _webSocketService.BroadcastNotificationAsync("unity.yourMethod", new
        {
            param1
        });

        // ALWAYS return a confirmation message - never return void!
        return $"Operation '{param1}' completed successfully";
    }
}
```

#### Step 2: Handle in Unity (Editor/Scripts/MCPEditorIntegration.cs)

Add case to `HandleNotification()`:

```csharp
case "unity.yourMethod":
    HandleYourMethod(parameters);
    break;
```

Implement handler (executes on main thread via queue):

```csharp
private static void HandleYourMethod(object parameters)
{
    try
    {
        // Execute Unity Editor APIs directly (already on main thread)
        var data = JsonUtility.FromJson<YourDataClass>(JsonUtility.ToJson(parameters));

        // Do Unity things...

        // Send result back
        _ = _client.SendNotificationAsync("unity.yourMethodResult", new { success = true });
    }
    catch (System.Exception ex)
    {
        Debug.LogError($"[MCPEditorIntegration] Error in HandleYourMethod: {ex.Message}");
    }
}
```

**Note:** Handlers are automatically called on the main thread via `MCPClient.ProcessMainThreadQueue()`, so no `EditorApplication.delayCall` is needed.

**Important - JSON Serialization:** Always use `Newtonsoft.Json` for deserializing parameters from WebSocket messages, NOT Unity's `JsonUtility`:

```csharp
// Correct - use Newtonsoft.Json
var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
var data = Newtonsoft.Json.JsonConvert.DeserializeObject<YourDataClass>(json);

// Incorrect - Unity JsonUtility doesn't handle WebSocket objects properly
var json = JsonUtility.ToJson(parameters);  // DON'T USE THIS
var data = JsonUtility.FromJson<YourDataClass>(json);  // DON'T USE THIS
```

#### Step 3: Asset Database Management

After making changes to files in the Assets folder, Unity's Asset Database must be refreshed to detect the changes.

**When automatic refresh happens:**
- After `unity_create_script` - calls `AssetDatabase.Refresh()` automatically
- After file writes in handlers - should call `AssetDatabase.Refresh()`
- Unity Editor detects file changes after a short delay (when focus returns to Unity)

**When to use unity_refresh_assets tool:**
- After batch file operations via external tools
- When changes aren't detected automatically
- When you need immediate recompilation

**Best practices:**
```csharp
// In handlers that create/modify files in Assets/
System.IO.File.WriteAllText(assetPath, content);
AssetDatabase.Refresh();  // Trigger immediate refresh
```

**Important:** Always call `AssetDatabase.Refresh()` after any file I/O operations in the Assets folder to ensure Unity detects changes immediately.

#### Step 4: Asset Creation with Complex Structures

The `unity_create_asset` tool uses an enhanced system for creating and populating Unity assets with complex nested structures.

**AssetHelper Architecture:**

1. **ConvertValue()** - Enhanced type conversion supporting:
   - Unity types: Vector3, Vector2, Color, Quaternion, Bounds, Rect
   - String formats: "x,y,z", "#RRGGBB", comma-separated values
   - JSON objects: `{"x": 0, "y": 0, "z": 0}`
   - Asset references: via AssetDatabase.LoadAssetAtPath

2. **SetPropertiesFromJson_Advanced()** - Uses Unity's SerializedObject API:
   - Handles complex nested structures (classes within classes)
   - Properly populates arrays and List<T> (e.g., `List<PrimitiveData>`)
   - Recursive property setting for nested objects
   - Supports all Unity SerializedPropertyTypes

3. **SetArrayProperty()** - Specialized array/list handling:
   - Sets array size correctly
   - Iterates elements and sets nested properties recursively
   - Works with List<CustomClass> structures

**Example - Creating ScriptableObject with nested List:**

```csharp
unity_create_asset(
  assetName: "DemoPrimitives",
  folderPath: "ScriptableObjects",
  assetTypeName: "PrimitiveSpawnConfig",
  propertiesJson: {
    "primitives": [
      {
        "primitiveType": 0,
        "position": {"x": 0, "y": 0, "z": 0},
        "color": {"r": 1, "g": 0, "b": 0, "a": 1},
        "scale": {"x": 1, "y": 1, "z": 1}
      },
      {
        "primitiveType": 1,
        "position": {"x": 2, "y": 0, "z": 0},
        "color": {"r": 0, "g": 0, "b": 1, "a": 1},
        "scale": {"x": 1, "y": 1, "z": 1}
      }
    ]
  }
)
```

This will properly populate a `List<PrimitiveData>` field on the ScriptableObject, with all nested Vector3 and Color values correctly set.

**Supported Asset Types:**
- **ScriptableObject** - Any custom ScriptableObject with complex nested structures
- **Material** - With shader and property assignment
- **Texture2D** - Basic texture creation
- **AnimationClip** - Animation asset creation
- **Any Unity asset type** - Generic fallback for other asset types

**Implementation Details:**

The system uses a hybrid approach:
- **SerializedObject API** for complex structures (arrays, nested objects, Unity types)
- **Reflection** as fallback for properties not found via SerializedProperty
- **Type conversion** for both string and JSON inputs

**Key Files:**
- `Editor/Scripts/Handlers/Assets/AssetHelper.cs` - Core asset creation logic
- `Editor/Scripts/Handlers/Assets/CreateAssetHandler.cs` - Handler that calls AssetHelper
- `Server~/Tools/Assets/CreateAssetTool.cs` - MCP tool definition

#### Step 5: Update Documentation

**IMPORTANT:** All documentation files must be placed in the `Documentation~/` folder (excluded from Unity package).

- Add tool description to README.md with parameters, returns, and related tools
- Add usage examples
- Update CHANGELOG.md
- **For technical deep-dives:** Create feature-specific documentation in `Documentation~/` (e.g., `Documentation~/MultiEditor-TechnicalRundown.md`)
  - Use descriptive names indicating the feature scope (not generic names like "TECHNICAL_RUNDOWN.md")
  - Include comprehensive "what was done" and "how it was implemented" sections
  - List all technologies/SDKs used
  - Document architecture decisions and trade-offs

---

## MCP Tool Best Practices

All MCP tools in this project follow strict best practices to ensure optimal LLM interaction. **These are REQUIRED for all tools:**

### 1. Always Return Confirmations (Never void)

**Bad ❌:**
```csharp
public async Task UnityCreateGameObjectAsync(...)
{
    await _webSocketService.BroadcastNotificationAsync(...);
    // Returns nothing - LLM has no feedback!
}
```

**Good ✅:**
```csharp
[return: Description("Confirmation message with GameObject name and position")]
public async Task<string> UnityCreateGameObjectAsync(...)
{
    await _webSocketService.BroadcastNotificationAsync(...);
    return $"GameObject '{name}' created at position ({x}, {y}, {z})";
}
```

**Why:** LLMs need confirmation that operations succeeded so they can chain operations confidently.

### 2. Rich Tool Descriptions with Tool Chaining Hints

**Bad ❌:**
```csharp
[Description("Create a GameObject")]
```

**Good ✅:**
```csharp
[Description("Create a new GameObject in the currently active Unity scene. You can specify its name, 3D position, components to add (e.g., 'Rigidbody,BoxCollider'), and parent object. The GameObject will be selected in the Hierarchy after creation. Use unity_find_game_object to verify creation or unity_add_component_to_object to add more components later.")]
```

**Why:**
- LLMs learn what the tool does and when to use it
- Cross-references help LLMs discover tool chains
- Examples show correct parameter formats

### 3. Parameter Descriptions with Examples

**Bad ❌:**
```csharp
[Description("The name")] string name
```

**Good ✅:**
```csharp
[Description("Comma-separated list of Unity components to add (e.g., 'Rigidbody,BoxCollider,AudioSource')")] string? components = null
```

**Why:** Examples teach LLMs the expected format, reducing errors.

### 4. Return Descriptions

**Always include `[return: Description]`:**

```csharp
[return: Description("Confirmation message with GameObject name and position")]
public async Task<string> UnityCreateGameObjectAsync(...)
```

**Why:** Helps LLMs understand what to expect back and how to use the response.

### 5. Document Side Effects and Warnings

**Important behaviors must be documented:**

```csharp
[Description("Enter Unity play mode (start running the game/scene). Equivalent to pressing the Play button in Unity Editor. IMPORTANT: Changes made to GameObjects in play mode are NOT saved unless explicitly copied. Any GameObjects created in play mode will be destroyed when exiting. Use unity_get_play_mode_state to check current state, and unity_exit_play_mode when done testing.")]
```

**Common warnings to include:**
- Play mode changes are NOT saved
- Compilation causes temporary disconnect
- Asset refresh takes time
- Scene changes must be saved

### 6. Consistent Naming Convention

- Tool class: `YourToolName + Tool` (e.g., `CreateGameObjectTool`)
- Method name: `Unity + Action + Target + Async` (e.g., `UnityCreateGameObjectAsync`)
- Tool name (auto-generated): `unity_action_target` (e.g., `unity_create_game_object`)

### 7. Proper File Organization

- Place tools in category folders: `Server~/Tools/GameObjects/`, `Server~/Tools/Scenes/`, etc.
- One tool per file
- File name matches class name

### 8. Example Tool Following All Best Practices

```csharp
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class CreateGameObjectTool(ILogger<CreateGameObjectTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<CreateGameObjectTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Create a new GameObject in the currently active Unity scene. You can specify its name, 3D position, components to add (e.g., 'Rigidbody,BoxCollider'), and parent object. The GameObject will be selected in the Hierarchy after creation. Use unity_find_game_object to verify creation or unity_add_component_to_object to add more components later.")]
    [return: Description("Confirmation message with GameObject name and position")]
    public async Task<string> UnityCreateGameObjectAsync(
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

        var componentInfo = components != null ? $" with components [{components}]" : "";
        var parentInfo = parent != null ? $" as child of '{parent}'" : " at root level";
        return $"GameObject '{name}' created at position ({x}, {y}, {z}){componentInfo}{parentInfo}";
    }
}
```

### Summary Checklist

When creating a new MCP tool, verify:

- ✅ Returns `Task<string>` with confirmation message (never `Task` void)
- ✅ Has `[return: Description]` attribute
- ✅ Tool description includes what it does, side effects, and related tools
- ✅ All parameters have `[Description]` with examples where helpful
- ✅ Follows naming convention: `Unity{Action}{Target}Async`
- ✅ Placed in correct category folder
- ✅ Documentation added to README.md
- ✅ Tested with actual LLM interaction

### Testing Locally

#### Test Server Only

```bash
# Quick rebuild with convenience script
./Scripts~/rebuild.sh

# Or manually:
cd Server~
docker build -t unity-mcp-server:test .
docker run -p 3727:3727 unity-mcp-server:test

# Test endpoints
curl http://localhost:3727/
curl http://localhost:3727/health

# Run smoke tests
./Scripts~/test.sh
```

#### Test Full Integration

1. Start server (Docker or `dotnet run`)
2. Open Unity project with package installed
3. Open MCP Dashboard (`Tools → Unity MCP Server → Dashboard`)
4. Click "Connect"
5. Test tools via IDE (VS Code, Cursor) or MCP Inspector:

```bash
npx @modelcontextprotocol/inspector http://localhost:3727/mcp
```

#### Integration Test Scenario (Pre-Release Validation)

**Purpose**: Comprehensive end-to-end test that validates MCP server, Unity integration, and all major tool categories before releases.

**When to Run**:
- Before committing major changes (e.g., Resources conversion, new features)
- Before creating releases
- When recording demos

**Test Scenario: PrimitiveSpawn Demo**

This scenario exercises the following MCP capabilities:
- Script creation (`unity_create_script`)
- Asset creation (`unity_create_asset` for ScriptableObjects)
- GameObject creation (`unity_create_game_object`)
- Component manipulation (`unity_add_component_to_object`)
- Play mode control (`unity_enter_play_mode`, `unity_exit_play_mode`)
- Scene queries (`unity_list_scene_objects`, `unity_find_game_object`)

**Steps:**

1. **Create ScriptableObject Configuration Script**
   - Create `PrimitiveSpawnConfig.cs` in `Assets/Scripts/`
   - Define fields: `List<PrimitiveData>` where each has `PrimitiveType`, `Vector3 position`, `Color color`
   - Mark as `[CreateAssetMenu]`

2. **Create Runtime Spawner Script**
   - Create `PrimitiveSpawner.cs` in `Assets/Scripts/`
   - Reference the ScriptableObject config
   - In `Start()`: instantiate all primitives from config data
   - Apply colors using materials

3. **Create Camera Orbit Script**
   - Create `CameraOrbit.cs` in `Assets/Scripts/`
   - Fields: `Transform target`, `float distance`, `float rotationSpeed`
   - In `Update()`: orbit around target using `RotateAround`

4. **Create ScriptableObject Asset Instance**
   - Use `unity_create_asset` to create instance of `PrimitiveSpawnConfig`
   - Populate with mock data (5-7 primitives):
     - Cube at (0, 0, 0), Red
     - Sphere at (2, 0, 0), Blue
     - Cylinder at (-2, 0, 0), Green
     - Capsule at (0, 0, 2), Yellow
     - Plane at (0, -1, 0), White (scaled 10x)

5. **Setup Scene**
   - Create `SpawnManager` GameObject
   - Attach `PrimitiveSpawner` script
   - Assign ScriptableObject config reference
   - Find Main Camera
   - Attach `CameraOrbit` script to camera
   - Set camera target to first primitive's position (0, 0, 0)

6. **Enter Play Mode and Validate**
   - Call `unity_enter_play_mode`
   - Verify primitives spawned correctly
   - Verify camera orbits around first primitive
   - Let run for 5-10 seconds to demonstrate
   - Call `unity_exit_play_mode`

**Expected Results:**
- All scripts compile without errors
- ScriptableObject created and populated
- Scene setup completes successfully
- Play mode shows 5-7 colored primitives
- Camera smoothly orbits around center cube
- No console errors

**What This Tests:**
- ✅ Script creation and compilation
- ✅ Asset database operations
- ✅ ScriptableObject workflow
- ✅ GameObject and component manipulation
- ✅ Play mode control
- ✅ Runtime script execution
- ✅ Material and rendering setup
- ✅ Full MCP → Unity → Runtime pipeline

**Troubleshooting:**
- If compilation fails: Check `unity_get_compilation_status` and `unity_get_console_logs`
- If primitives don't spawn: Verify ScriptableObject was populated correctly
- If camera doesn't orbit: Check camera target assignment in inspector
- If play mode fails: Ensure no compilation errors exist first

### Building Docker Images

```bash
cd Server~

# Build for local platform
docker build -t unity-mcp-server .

# Build multi-platform (requires buildx)
docker buildx build --platform linux/amd64,linux/arm64 -t unity-mcp-server .

# Test the image
docker run --rm -p 3727:3727 unity-mcp-server
```

### Running Tests

```bash
cd Server~

# Run unit tests (when implemented)
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## Code Style Guidelines

### C# Server Code

- Follow [Microsoft C# Coding Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use `async/await` for all I/O operations
- Add XML documentation comments for public APIs
- Use dependency injection for services
- Log important events with appropriate levels

### Unity C# Code

- Follow [Unity C# Coding Guidelines](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)
- Use `PascalCase` for public members
- Use `_camelCase` for private fields
- Async operations should use `async/await` with Unity's threading constraints
- Use main thread queue pattern (`ConcurrentQueue<Action>`) for marshaling callbacks from background threads
- Process queue via `EditorApplication.update` to execute Unity API calls on the main thread

### UIToolkit Guidelines

- Create reusable UI components
- Use USS for styling when possible (currently inline styles for simplicity)
- Prefer data binding over manual UI updates
- Keep UI logic in EditorWindow, business logic in services

## Git Workflow

### Branch Strategy

- `main` - Stable releases only, protected. Only receives merges from `develop` during version releases.
- `develop` - Integration branch for all features. This is the default PR target.
- `feature/*` - Feature branches
- `fix/*` - Bug fix branches

**IMPORTANT REMINDER:** Always create a feature branch (e.g., `feature/new-tool`, `fix/bug-name`) before starting new work. Do not commit directly to `develop` or `main`. This enables proper PR reviews and keeps the history clean.

### Pull Request Targets

**CRITICAL: Feature PRs MUST target `develop`, NEVER `main`!**

| PR Type | Target Branch | Example |
|---------|---------------|---------|
| Feature PR | `develop` | `feature/prefab-system` → `develop` |
| Bug fix PR | `develop` | `fix/websocket-timeout` → `develop` |
| Hotfix PR | `main` (rare) | Critical production fixes only |
| Release PR | `main` | `develop` → `main` (version release) |

**Workflow:**
1. Create feature branch from `develop`
2. Open PR targeting `develop`
3. Merge feature into `develop`
4. When ready for release: merge `develop` → `main` and tag

**If you accidentally target `main`:** Change the PR base branch immediately:
```bash
gh api repos/OWNER/REPO/pulls/PR_NUMBER -X PATCH -f base=develop
```

### Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add unity_list_scene_objects tool
fix: resolve WebSocket connection timeout
docs: update installation guide
chore: bump version to 0.2.0
ci: add multi-arch Docker build
```

### Making Changes

```bash
# Create feature branch
git checkout -b feature/amazing-feature

# Make changes and commit
git add .
git commit -m "feat: add amazing feature"

# Push to GitHub
git push origin feature/amazing-feature

# Create Pull Request on GitHub
```

## Release Process

### Version Bumping

1. Update version in `package.json`
2. Update CHANGELOG.md
3. Commit changes:

```bash
git add package.json CHANGELOG.md
git commit -m "chore: bump version to X.Y.Z"
git push origin main
```

### Creating a Release

```bash
# Tag the commit
git tag vX.Y.Z

# Push tag
git push origin vX.Y.Z
```

This triggers:
1. GitHub Actions builds Docker image
2. Publishes to ghcr.io with version tag
3. Creates GitHub Release
4. Provides instructions for OpenUPM publication

### OpenUPM Publication

After creating a GitHub release:

1. Go to [OpenUPM](https://openupm.com/)
2. Follow [Adding UPM Package](https://openupm.com/docs/adding-upm-package.html) guide
3. Submit PR to openupm/openupm repository
4. Wait for review and merge

## CI/CD Pipeline

### Workflows

1. **build-server.yml**
   - Triggers: Push/PR to main or develop (Server~ changes)
   - Actions: Restore, build, test, publish artifacts

2. **publish-docker.yml**
   - Triggers: Push to main (Server~ changes), releases
   - Actions: Build multi-arch image, push to ghcr.io
   - Tags: latest, version tags, branch names

3. **publish-openupm.yml**
   - Triggers: Version tags (v*)
   - Actions: Verify version, create GitHub release, guide OpenUPM publication

### Secrets Required

- `GITHUB_TOKEN` - Automatically provided by GitHub Actions
- No additional secrets needed (uses ghcr.io with repo permissions)

## Troubleshooting Development Issues

### Server won't build

```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build
```

### Unity package not recognized

- Ensure `package.json` is in repository root
- Check version format (semver required)
- Verify Unity version compatibility

### WebSocket connection fails locally

- Check server is running: `curl http://localhost:3727/health`
- Verify port 3727 is not in use
- Check firewall settings
- Try connecting to `ws://127.0.0.1:3727/ws` instead of localhost

### Docker build fails

- Update Docker Desktop
- Clear build cache: `docker builder prune`
- Check Dockerfile syntax
- Ensure .dockerignore is not excluding necessary files

## Development Scripts

The `Scripts~/` folder contains useful development scripts:

### rebuild.sh
Builds both the .NET server and Docker image in one command:
```bash
./Scripts~/rebuild.sh
```

### start-mcp-server.sh
Starts the MCP server container with proper configuration:
```bash
./Scripts~/start-mcp-server.sh
```

### test.sh
Runs comprehensive smoke tests for the server:
```bash
./Scripts~/test.sh
```

### sign-package.sh
Signs the UPM package for Unity 6+ compatibility (requires Unity 6.3+ and Unity Cloud org):
```bash
# First, set up credentials in .env (copy from .env.example)
cp .env.example .env
# Edit .env with your Unity credentials

# Sign the package
./Scripts~/sign-package.sh

# Or with a specific version
./Scripts~/sign-package.sh 0.6.0

# Sign and upload to GitHub release
./Scripts~/sign-package.sh --upload
```

## Useful Commands

```bash
# Development scripts (recommended)
./Scripts~/rebuild.sh           # Build server + Docker image
./Scripts~/start-mcp-server.sh  # Start MCP server container
./Scripts~/test.sh              # Run smoke tests
./Scripts~/sign-package.sh      # Sign UPM package for Unity 6+

# Server development
cd Server~
dotnet run                      # Run server locally
dotnet watch run                # Run with hot reload
dotnet build -c Release         # Production build

# Docker operations (manual)
cd Server~
docker build -t unity-mcp-server:test .   # Build image
docker run -p 3727:3727 unity-mcp-server:test  # Run container
docker logs unity-mcp-server    # View logs
docker exec -it unity-mcp-server sh  # Enter container

# Git operations
git log --oneline --graph       # View commit history
git status                      # Check working tree
git diff                        # View changes

# Testing MCP
npx @modelcontextprotocol/inspector http://localhost:3727/mcp
```

## Release Process

This section documents the complete process for publishing a new version to OpenUPM.

### Prerequisites

- All changes committed and pushed to `main`
- Docker image published to ghcr.io (automatic on push to main)
- All tests passing
- Version number decided (follow [Semantic Versioning](https://semver.org/))
- (Optional) Package signing configured for Unity 6+ compatibility

### Package Signing Setup (Unity 6+ Compatibility)

To enable automated package signing in CI/CD:

**GitHub Secrets Required** (Settings → Secrets and variables → Actions):
| Secret | Description |
|--------|-------------|
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |
| `UNITY_ORG_ID` | Unity Cloud Organization ID |
| `UNITY_LICENSE` | Unity license file content (`.ulf` file) |

**GitHub Variable Required** (Settings → Secrets and variables → Actions → Variables):
| Variable | Value |
|----------|-------|
| `ENABLE_PACKAGE_SIGNING` | `true` |

**Get Organization ID:** https://cloud.unity.com/account/my-organizations

**Get Unity License for CI:**
1. Run: `unity-editor -batchmode -createManualActivationFile`
2. Upload `.alf` to https://license.unity3d.com/manual
3. Download `.ulf` and paste contents as `UNITY_LICENSE` secret

If signing is not configured, releases still work - just without signed packages.

### Release Checklist

#### 1. Update CHANGELOG.md

Move unreleased changes to a new versioned section:

```markdown
## [Unreleased]

### Planned Features
- Future features...

## [X.Y.Z] - YYYY-MM-DD

### Added
- New features from unreleased section

### Changed
- Changes from unreleased section

### Fixed
- Bug fixes from unreleased section
```

#### 2. Update package.json

Bump version to match the release version:

```json
{
  "version": "X.Y.Z",
  ...
}
```

#### 3. Verify Workflow Configuration

Ensure `.github/workflows/publish-openupm.yml` has the correct package name:
- Package name should be `com.mezookan.unity-mcp-sharp`
- Workflow triggers on tags matching `v*`
- GitHub Release is created automatically
- OpenUPM instructions are displayed

#### 4. Commit Release Preparation

```bash
git add CHANGELOG.md package.json .github/workflows/publish-openupm.yml
git commit -m "chore: prepare release vX.Y.Z"
git push origin main
```

#### 5. Create and Push Git Tag

The tag triggers the OpenUPM workflow:

```bash
# Create annotated tag
git tag -a vX.Y.Z -m "Release vX.Y.Z"

# Push tag to GitHub
git push origin vX.Y.Z
```

#### 6. Verify GitHub Actions

Monitor the workflow execution:

```bash
# Watch the workflow
gh run list

# View specific run
gh run view <run-id>

# View on GitHub
# https://github.com/Abbabon/unity-mcp-sharp/actions
```

The workflow will:
- Extract version from tag
- Verify `package.json` version matches tag
- (If signing enabled) Sign the UPM package with Unity Cloud credentials
- Create GitHub Release with:
  - Release notes from CHANGELOG
  - Docker pull command
  - OpenUPM installation command
  - Signed `.tgz` package (if signing enabled)
- Display manual steps for OpenUPM submission

#### 7. Submit to OpenUPM

After the GitHub Release is created, submit to OpenUPM:

**Option A: Via Pull Request (First-time submission)**

1. Fork the [openupm/openupm](https://github.com/openupm/openupm) repository
2. Add your package to `data/packages/<package-name>.yml`:

```yaml
name: com.mezookan.unity-mcp-sharp
displayName: Unity MCP Server
description: Model Context Protocol server for Unity Editor integration
repoUrl: 'https://github.com/Abbabon/unity-mcp-sharp'
parentRepoUrl: null
licenseSpdxId: MIT
licenseName: MIT License
topics:
  - mcp
  - ai
  - llm
  - editor
  - automation
hunter: YourGitHubUsername
gitTagPrefix: v
gitTagIgnore: ''
minVersion: '0.1.0'
image: null
readme: 'master:README.md'
```

3. Create PR to openupm/openupm
4. Wait for review and merge

**Option B: Via CLI (If you have access)**

```bash
openupm publish com.mezookan.unity-mcp-sharp
```

#### 8. Verify OpenUPM Publication

Once published, verify the package is available:

```bash
# Search for package
openupm search unity-mcp

# View package info
openupm view com.mezookan.unity-mcp-sharp

# Test installation in a Unity project
openupm add com.mezookan.unity-mcp-sharp
```

### Version Numbering Guidelines

Follow [Semantic Versioning](https://semver.org/):

- **Major (X.0.0)**: Breaking changes, incompatible API changes
- **Minor (0.X.0)**: New features, backwards-compatible
- **Patch (0.0.X)**: Bug fixes, backwards-compatible

Examples:
- `0.3.0`: Added comprehensive operation tracking (new features)
- `0.3.1`: Fixed operation tracking bug (bug fix)
- `1.0.0`: Stable public API, breaking changes from 0.x

### Common Issues

**Tag already exists:**
```bash
# Delete local tag
git tag -d vX.Y.Z

# Delete remote tag
git push origin :refs/tags/vX.Y.Z

# Create new tag
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z
```

**Workflow failed - version mismatch:**
- Ensure `package.json` version matches tag (without 'v' prefix)
- Tag: `v0.3.0` should match package.json: `"version": "0.3.0"`

**Docker image not published:**
- Check `.github/workflows/publish-docker.yml` workflow
- Verify Server~/ changes were pushed
- Manual trigger: `gh workflow run publish-docker.yml`

### Post-Release Tasks

1. **Update documentation** if needed
2. **Announce release** in community channels
3. **Monitor issues** for bug reports
4. **Plan next release** based on feedback

### Quick Release Command Summary

```bash
# 1. Update files
# Edit CHANGELOG.md and package.json manually

# 2. Commit
git add CHANGELOG.md package.json
git commit -m "chore: prepare release vX.Y.Z"
git push origin main

# 3. Tag and push
git tag -a vX.Y.Z -m "Release vX.Y.Z"
git push origin vX.Y.Z

# 4. Monitor
gh run list
gh run watch <run-id>

# 5. Submit to OpenUPM (manual PR or CLI)
```

## Resources

- [Model Context Protocol Docs](https://modelcontextprotocol.io)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [Unity Package Layout](https://docs.unity3d.com/Manual/cus-layout.html)
- [UIToolkit Documentation](https://docs.unity3d.com/Manual/UIElements.html)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

## Getting Help

- **Issues:** [GitHub Issues](https://github.com/Abbabon/unity-mcp-sharp/issues)
- **Discussions:** [GitHub Discussions](https://github.com/Abbabon/unity-mcp-sharp/discussions)
- **MCP Community:** [Discord](https://discord.gg/modelcontextprotocol)

---

Happy coding! 🚀
