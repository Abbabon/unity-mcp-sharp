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
│   ├── Troubleshooting.md
│   └── Testing.md
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

### Request Handling Pattern

1. **MCP Tool Invocation** (from IDE)
   - Tool called via MCP endpoint
   - Server broadcasts JSON-RPC notification to Unity via WebSocket
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

# Server will be available at http://localhost:8080
```

#### Unity Package Development

1. Open any Unity project (2021.3+)
2. Clone this repository
3. In Unity Package Manager:
   - Click `+` → "Add package from disk..."
   - Select the `package.json` in repository root

**Note:** Changes to scripts will hot-reload in Unity

### Adding New MCP Tools

#### Step 1: Define the Tool (Server~/Tools/UnityTools.cs)

```csharp
[McpTool(
    Name = "unity_your_tool_name",
    Description = "Clear description of what this tool does"
)]
public async Task<string> YourToolName(
    [Description("Parameter description")] string param1,
    CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Executing your tool");

    // Broadcast JSON-RPC request to Unity
    await _webSocketService.BroadcastNotificationAsync("unity.yourMethod", new
    {
        param1
    });

    return "Request sent to Unity Editor";
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

#### Step 3: Update Documentation

- Add tool description to README.md
- Add usage examples
- Update CHANGELOG.md

### Testing Locally

#### Test Server Only

```bash
# Quick rebuild with convenience script
./Scripts~/rebuild.sh

# Or manually:
cd Server~
docker build -t unity-mcp-server:test .
docker run -p 8080:8080 unity-mcp-server:test

# Test endpoints
curl http://localhost:8080/
curl http://localhost:8080/health

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
npx @modelcontextprotocol/inspector http://localhost:8080/mcp
```

### Building Docker Images

```bash
cd Server~

# Build for local platform
docker build -t unity-mcp-server .

# Build multi-platform (requires buildx)
docker buildx build --platform linux/amd64,linux/arm64 -t unity-mcp-server .

# Test the image
docker run --rm -p 8080:8080 unity-mcp-server
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

- `main` - Stable releases, protected
- `develop` - Integration branch (optional)
- `feature/*` - Feature branches
- `fix/*` - Bug fix branches

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

- Check server is running: `curl http://localhost:8080/health`
- Verify port 8080 is not in use
- Check firewall settings
- Try connecting to `ws://127.0.0.1:8080/ws` instead of localhost

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

## Useful Commands

```bash
# Development scripts (recommended)
./Scripts~/rebuild.sh           # Build server + Docker image
./Scripts~/start-mcp-server.sh  # Start MCP server container
./Scripts~/test.sh              # Run smoke tests

# Server development
cd Server~
dotnet run                      # Run server locally
dotnet watch run                # Run with hot reload
dotnet build -c Release         # Production build

# Docker operations (manual)
cd Server~
docker build -t unity-mcp-server:test .   # Build image
docker run -p 8080:8080 unity-mcp-server:test  # Run container
docker logs unity-mcp-server    # View logs
docker exec -it unity-mcp-server sh  # Enter container

# Git operations
git log --oneline --graph       # View commit history
git status                      # Check working tree
git diff                        # View changes

# Testing MCP
npx @modelcontextprotocol/inspector http://localhost:8080/mcp
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
