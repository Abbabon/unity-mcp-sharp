# Unity MCP Server

**Model Context Protocol (MCP) integration for Unity Editor** - Enable AI assistants to interact with Unity through console logs, compilation, and scene manipulation.

[![Build Server](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/build-server.yml/badge.svg)](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/build-server.yml)
[![Publish Docker](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/publish-docker.yml/badge.svg)](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/publish-docker.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

> **Note:** v0.1.0 focuses on WebSocket communication with Unity. Full MCP protocol integration with IDEs will be added in a future release when the C# MCP SDK is more mature.

## Features

- **WebSocket Communication (JSON-RPC 2.0)**
  - Real-time bidirectional communication with Unity Editor
  - Extensible command/response pattern
  - Support for Unity operations and queries

- **23 MCP Tools Available**
  - **Console & Compilation:** Get console logs, trigger/check compilation status
  - **GameObjects:** Create, find, batch create, add components, list scene objects
  - **Scenes:** List, open, close, save, get/set active scene
  - **Assets:** Create scripts, create assets (Materials, Textures, etc.), refresh database
  - **Play Mode:** Enter, exit, get play mode state
  - **Project Info:** Get Unity version, project name, paths
  - **System:** Run any Unity menu item programmatically

- **Optimized for LLM Interaction**
  - All tools return confirmation messages for reliable feedback
  - Tool descriptions include cross-references for chaining operations
  - Side effects and warnings clearly documented
  - Rich return descriptions help LLMs understand responses

- **Unity Package** (OpenUPM compatible)
  - UIToolkit-based dashboard with status monitoring
  - Visual feedback system with operation tracking
  - Docker container lifecycle management
  - Auto-connect and auto-start capabilities
  - Configuration via ScriptableObject

- **Dockerized Server**
  - Built with .NET 9.0 and ASP.NET Core
  - Published to GitHub Container Registry (ghcr.io)
  - Multi-platform support (linux/amd64, linux/arm64)
  - Full CI/CD pipeline with GitHub Actions

## Architecture

```
┌─────────────────┐         ┌──────────────────┐         ┌─────────────────┐
│   AI Assistant  │         │   Unity Editor   │         │  Unity Package  │
│  (IDE/LLM)      │◄────────┤                  │◄────────┤  (OpenUPM)      │
└────────┬────────┘  MCP    │                  │ Editor  └────────┬────────┘
         │         (HTTP)   │                  │  API             │
         │                  └──────────────────┘                  │
         │                                                        │
         │                                                        │
         └────────────────┐                    ┌─────────────────┘
                          │                    │
                          ▼                    ▼ WebSocket
                    ┌──────────────────────────────┐
                    │   Unity MCP Server           │
                    │   (Docker Container)         │
                    │   ┌────────────────────┐     │
                    │   │  ASP.NET Core      │     │
                    │   │  - HTTP Endpoint   │     │
                    │   │  - WebSocket       │     │
                    │   │  - JSON-RPC 2.0    │     │
                    │   └────────────────────┘     │
                    └──────────────────────────────┘
```

## Quick Start

### Prerequisites

- **Unity** 2021.3 or later
- **Docker Desktop** installed and running
- **.NET 9.0 SDK** (for server development only)

### Installation

#### Option 1: OpenUPM (Recommended)

```bash
openupm add com.unitymcpsharp.unity-mcp
```

#### Option 2: Git URL

1. Open Unity Package Manager
2. Click `+` → "Add package from git URL..."
3. Enter: `https://github.com/Abbabon/unity-mcp-sharp.git`

#### Option 3: Manual

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.unitymcpsharp.unity-mcp": "https://github.com/Abbabon/unity-mcp-sharp.git"
  }
}
```

### First-Time Setup

1. **Install Docker Desktop** (if not already installed)
   - Download from [docker.com](https://www.docker.com/products/docker-desktop/)
   - Start Docker Desktop

2. **Open the Setup Wizard**
   - In Unity: `Tools → Unity MCP Server → Setup Wizard`
   - Follow the on-screen instructions

3. **Start the Server**
   - Go to `Tools → Unity MCP Server → Dashboard`
   - Click **"Start Server"** (downloads Docker image on first run)
   - Click **"Connect"** to establish WebSocket connection

4. **Verify Connection**
   - Dashboard shows "Connected ✓" in green
   - Console logs: "Unity MCP Server connected successfully"

### Using with AI Assistants

#### VS Code / GitHub Copilot

Add to `.vscode/settings.json`:

```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:8080/mcp",
      "transport": "sse"
    }
  }
}
```

#### Cursor IDE

Add to `~/.cursor/config.json`:

```json
{
  "mcpServers": {
    "unity": {
      "url": "http://localhost:8080/mcp",
      "transport": "sse"
    }
  }
}
```

## Available MCP Tools

All tools are designed for optimal LLM interaction with:
- **Confirmation messages** - Every operation returns success feedback
- **Tool chaining hints** - Descriptions suggest related tools to use next
- **Side effect warnings** - Important behaviors clearly documented

### System & Project Information

#### `unity_get_project_info`
Get Unity project metadata including name, version, active scene, paths, and editor state.

**Returns:** Project information with name, Unity version, active scene, data path, play/pause state

**Use this first** when starting work on a project to understand the environment.

#### `unity_get_console_logs`
Get recent console logs from Unity Editor (errors, warnings, debug logs).

**Returns:** Recent console logs with type, message, and stack traces

**Tip:** Call this after creating scripts, entering play mode, or when compilation fails.

#### `unity_get_compilation_status`
Check if Unity is currently compiling and if last compilation succeeded.

**Returns:** Compilation status (idle/compiling) and last compilation result

**Related tools:** `unity_trigger_script_compilation`, `unity_get_console_logs`

#### `unity_trigger_script_compilation`
Force Unity to recompile all C# scripts.

**Returns:** Confirmation that compilation was triggered

**Note:** Unity temporarily disconnects during compilation. Use `unity_get_compilation_status` after to verify success.

### GameObjects

#### `unity_create_game_object`
Create a new GameObject in the currently active scene.

**Parameters:**
- `name` (string, required): GameObject name
- `x`, `y`, `z` (float, default: 0): World position
- `components` (string, optional): Comma-separated components (e.g., "Rigidbody,BoxCollider")
- `parent` (string, optional): Parent GameObject name

**Returns:** Confirmation with name, position, components, and hierarchy location

**Example:** Create a "Player" at position (0, 1, 0) with Rigidbody and CapsuleCollider

**Related tools:** `unity_find_game_object`, `unity_add_component_to_object`

#### `unity_find_game_object`
Find a GameObject by name, tag, or path with detailed information.

**Parameters:**
- `name` (string, required): GameObject name
- `searchBy` (string, default: "name"): Search mode: "name", "tag", or "path"

**Returns:** Position, rotation, scale, active state, and all attached components

**Related tools:** `unity_list_scene_objects`, `unity_add_component_to_object`

#### `unity_add_component_to_object`
Add a component to an existing GameObject.

**Parameters:**
- `gameObjectName` (string, required): Target GameObject
- `componentType` (string, required): Component type (e.g., "Rigidbody", "BoxCollider", custom scripts)

**Returns:** Confirmation that component was added

**Tip:** Use `unity_find_game_object` first to verify the GameObject exists.

#### `unity_list_scene_objects`
Get the complete GameObject hierarchy of the active scene.

**Returns:** Hierarchical list with active/inactive state indicators

**Related tools:** `unity_find_game_object`, `unity_create_game_object`

#### `unity_batch_create_game_objects`
Create multiple GameObjects in a single operation (more efficient than one-by-one).

**Parameters:**
- `gameObjectsJson` (string, required): JSON array of GameObject specs

**Returns:** Confirmation that batch creation was initiated

#### `unity_create_game_object_in_scene`
Create a GameObject in a specific scene (not necessarily the active one).

**Parameters:**
- `scenePath` (string, required): Scene path (e.g., "Scenes/Level1.unity")
- `name`, `x`, `y`, `z`, `components`, `parent`: Same as `unity_create_game_object`

**Returns:** Confirmation with scene path, name, and position

**Note:** If scene is not loaded, it will be opened additively first.

### Scenes

#### `unity_list_scenes`
List all .unity scene files in the project.

**Returns:** List of scene paths relative to project root

**Related tools:** `unity_open_scene`, `unity_get_active_scene`

#### `unity_get_active_scene`
Get information about the currently active scene.

**Returns:** Scene name, path, isDirty status, root GameObject count, loaded state

**Tip:** Use `unity_save_scene` if isDirty is true to save changes.

#### `unity_open_scene`
Open a Unity scene by path.

**Parameters:**
- `scenePath` (string, required): Path relative to Assets folder
- `additive` (bool, default: false): Keep other scenes open if true

**Returns:** Confirmation with scene path and mode (single/additive)

**Related tools:** `unity_list_scenes`, `unity_get_active_scene`

#### `unity_close_scene`
Close a specific scene (only works with multiple scenes open).

**Parameters:**
- `sceneIdentifier` (string, required): Scene name or path

**Returns:** Confirmation that scene was closed

**Note:** Cannot close the last open scene.

#### `unity_save_scene`
Save the active scene or a specific scene.

**Parameters:**
- `scenePath` (string, optional): Specific scene to save (null = active)
- `saveAll` (bool, default: false): Save all open scenes

**Returns:** Confirmation of which scene(s) were saved

**Important:** Always save after making changes, otherwise they'll be lost!

#### `unity_set_active_scene`
Set which scene is active (where new GameObjects are created).

**Parameters:**
- `sceneIdentifier` (string, required): Scene name or path

**Returns:** Confirmation that scene is now active

**Note:** Only works when multiple scenes are open.

### Assets & Scripts

#### `unity_create_script`
Create a new C# MonoBehaviour script file.

**Parameters:**
- `scriptName` (string, required): Script name (without .cs)
- `folderPath` (string, required): Path within Assets (e.g., "Scripts/Player")
- `scriptContent` (string, required): Full C# class code

**Returns:** Confirmation with file path and recompilation notice

**Related tools:** `unity_get_compilation_status`, `unity_get_console_logs`

#### `unity_create_asset`
Create any type of Unity asset (Material, Texture2D, ScriptableObject, etc.) using reflection.

**Parameters:**
- `assetName` (string, required): Asset name (without extension)
- `folderPath` (string, required): Path within Assets
- `assetTypeName` (string, required): Full type name (e.g., "UnityEngine.Material")
- `propertiesJson` (string, optional): JSON properties to set

**Returns:** Confirmation with asset name, type, and path

**Example properties:**
- Material: `{"shader":"Standard","color":"#FF0000"}`
- Texture2D: `{"width":256,"height":256}`

#### `unity_refresh_assets`
Refresh Unity Asset Database to detect file changes.

**Returns:** Confirmation that refresh was initiated

**Use after:** Batch file operations or when changes aren't detected automatically

**Note:** Can take a few seconds for large projects. Use `unity_get_compilation_status` to check if recompilation is complete.

### Play Mode

#### `unity_enter_play_mode`
Enter Unity play mode (start running the game).

**Returns:** Confirmation message with important warning

**IMPORTANT:** Changes made in play mode are NOT saved! GameObjects created will be destroyed when exiting.

**Related tools:** `unity_get_play_mode_state`, `unity_exit_play_mode`

#### `unity_exit_play_mode`
Exit Unity play mode (stop running the game).

**Returns:** Confirmation that play mode was exited

**Note:** All changes made during play mode will be reverted.

#### `unity_get_play_mode_state`
Get current play mode state.

**Returns:** Current state (Playing, Paused, or Stopped)

**Related tools:** `unity_enter_play_mode`, `unity_exit_play_mode`

### System Utilities

#### `unity_run_menu_item`
Execute any Unity Editor menu item by its path.

**Parameters:**
- `menuPath` (string, required): Full menu path (e.g., "GameObject/Create Empty", "Edit/Undo")

**Returns:** Confirmation that menu item was executed

**Use as:** Fallback for operations not covered by dedicated tools

**Examples:**
- `"GameObject/Create Empty"`
- `"Edit/Undo"`
- `"Assets/Refresh"`

## Docker Image

### Pull from GitHub Container Registry

```bash
docker pull ghcr.io/abbabon/unity-mcp-server:latest
```

### Run Manually

```bash
docker run -d \
  --name unity-mcp-server \
  -p 8080:8080 \
  --restart unless-stopped \
  ghcr.io/abbabon/unity-mcp-server:latest
```

### Available Tags

- `latest` - Latest stable version from main branch
- `v*.*.*` - Specific version tags (e.g., `v0.1.0`)
- `main` - Latest build from main branch

## Development

### Development Scripts

The project includes convenience scripts in `Scripts~/`:

```bash
# Build server + Docker image
./Scripts~/rebuild.sh

# Start MCP server container
./Scripts~/start-mcp-server.sh

# Run smoke tests
./Scripts~/test.sh
```

### Server Development

```bash
cd Server~

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run locally
dotnet run

# Build Docker image (or use ./Scripts~/rebuild.sh)
docker build -t unity-mcp-server:test .

# Run with docker-compose
docker-compose up
```

### Unity Package Development

The package is structured as a Unity UPM package:

```
.
├── Runtime/              # Runtime scripts (MCPClient, MCPServerManager)
├── Editor/               # Editor scripts (Dashboard, Integration, Menu Items)
├── Documentation~/       # User documentation (excluded from package)
├── Scripts~/             # Development scripts (excluded from package)
├── Server~/              # MCP server (excluded from package)
├── TestProject~/         # Test Unity project (excluded from package)
└── package.json          # UPM manifest
```

**Note:** Directories with `~/` suffix are excluded from Unity package imports.

## Configuration

Access configuration via `Tools → Unity MCP Server → Create MCP Configuration` or through the Dashboard.

**Settings:**
- Server URL (default: `ws://localhost:8080/ws`)
- Docker image name
- Auto-connect on startup
- Auto-start container
- Retry attempts and delays
- Verbose logging
- Max log buffer size

## Troubleshooting

### Docker not found

**Solution:** Install Docker Desktop and ensure it's running.

### Connection refused

**Possible causes:**
1. Docker container not running → Start it from Dashboard
2. Port 8080 already in use → Change port in configuration
3. Firewall blocking connection → Allow Docker in firewall settings

### Container fails to start

**Check logs:**
```bash
docker logs unity-mcp-server
```

**Or** use the **Logs** tab in the Unity MCP Dashboard.

### "Image not found" error

The package will automatically pull the image on first start. If this fails:

```bash
# Manually pull the image
docker pull ghcr.io/abbabon/unity-mcp-server:latest
```

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## CI/CD Pipeline

The project includes comprehensive GitHub Actions workflows:

- **Build Server** - Builds and tests the .NET server on every push/PR
- **Publish Docker** - Publishes multi-arch images to ghcr.io on main branch
- **Publish OpenUPM** - Creates GitHub releases and guides OpenUPM publication on version tags

### Creating a Release

```bash
# Update version in package.json
# Commit changes
git add package.json
git commit -m "Bump version to 1.0.0"

# Create and push tag
git tag v1.0.0
git push origin main --tags
```

This triggers the full CI/CD pipeline.

## License

MIT License - see [LICENSE](LICENSE) file for details.

## Links

- **Documentation:**
  - [Installation Guide](Documentation~/Installation.md)
  - [Configuration Guide](Documentation~/Configuration.md)
  - [Troubleshooting](Documentation~/Troubleshooting.md)
  - [Testing Guide](Documentation~/Testing.md)
- **Issues:** [GitHub Issues](https://github.com/Abbabon/unity-mcp-sharp/issues)
- **Model Context Protocol:** [modelcontextprotocol.io](https://modelcontextprotocol.io)
- **Docker Hub:** [ghcr.io/abbabon/unity-mcp-server](https://ghcr.io/abbabon/unity-mcp-server)

## Credits

Created by [AmitN](https://github.com/Abbabon)

Built with:
- [Model Context Protocol SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [.NET 9.0](https://dotnet.microsoft.com/)
- [ASP.NET Core](https://asp.net/)
- [Unity](https://unity.com/)

---

**Made with ❤️ for the Unity and AI communities**
