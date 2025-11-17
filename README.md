<div align="center">

# 🎮 Unity MCP Server

**Model Context Protocol (MCP) integration for Unity Editor**
Enable AI assistants to interact with Unity through console logs, compilation, and scene manipulation.

<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-1-orange.svg?style=flat-square)](#contributors)
<!-- ALL-CONTRIBUTORS-BADGE:END -->

[![Build Server](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/build-server.yml/badge.svg)](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/build-server.yml)
[![Publish Docker](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/publish-docker.yml/badge.svg)](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/publish-docker.yml)
[![CodeQL](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/codeql.yml/badge.svg)](https://github.com/Abbabon/unity-mcp-sharp/actions/workflows/codeql.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![GitHub Release](https://img.shields.io/github/v/release/Abbabon/unity-mcp-sharp?include_prereleases&style=flat-square)](https://github.com/Abbabon/unity-mcp-sharp/releases)
[![Maintenance](https://img.shields.io/badge/Maintained%3F-yes-green.svg)](https://github.com/Abbabon/unity-mcp-sharp/graphs/commit-activity)

[![openupm](https://img.shields.io/npm/v/com.mezookan.unity-mcp-sharp?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.mezookan.unity-mcp-sharp/)
[![Downloads](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.mezookan.unity-mcp-sharp)](https://openupm.com/packages/com.mezookan.unity-mcp-sharp/)
![Top Language](https://img.shields.io/github/languages/top/Abbabon/unity-mcp-sharp)

[🚀 Quick Start](#-quick-start) • [📦 Installation](#-installation) • [🛠️ MCP Tools](#-available-mcp-tools) • [📖 Docs](Documentation~/Installation.md) • [❓ Issues](https://github.com/Abbabon/unity-mcp-sharp/issues)

</div>

---

## 📋 Table of Contents

- [✨ Features](#-features)
- [🏗️ Architecture](#-architecture)
- [🚀 Quick Start](#-quick-start)
- [📦 Installation](#-installation)
- [🤖 Using with AI Assistants](#-using-with-ai-assistants)
- [🛠️ Available MCP Tools](#-available-mcp-tools)
- [🐳 Docker Image](#-docker-image)
- [💻 Development](#-development)
- [⚙️ Configuration](#-configuration)
- [🔧 Troubleshooting](#-troubleshooting)
- [🤝 Contributing](#-contributing)
- [📄 License](#-license)

---

## ✨ Features

<details open>
<summary><b>🔌 WebSocket Communication (JSON-RPC 2.0)</b></summary>

- Real-time bidirectional communication with Unity Editor
- Extensible command/response pattern
- Support for Unity operations and queries
</details>

<details open>
<summary><b>🛠️ 23 MCP Tools Available</b></summary>

| Category | Tools |
|----------|-------|
| **Console & Compilation** | Get console logs, trigger/check compilation status |
| **GameObjects** | Create, find, batch create, add components, list scene objects |
| **Scenes** | List, open, close, save, get/set active scene |
| **Assets** | Create scripts, create assets (Materials, Textures, etc.), refresh database |
| **Play Mode** | Enter, exit, get play mode state |
| **Project Info** | Get Unity version, project name, paths |
| **System** | Run any Unity menu item programmatically |
</details>

<details open>
<summary><b>🤖 Optimized for LLM Interaction</b></summary>

- ✅ All tools return confirmation messages for reliable feedback
- 🔗 Tool descriptions include cross-references for chaining operations
- ⚠️ Side effects and warnings clearly documented
- 📝 Rich return descriptions help LLMs understand responses
</details>

<details open>
<summary><b>📦 Unity Package (OpenUPM compatible)</b></summary>

- 🎨 UIToolkit-based dashboard with status monitoring
- 👁️ Visual feedback system with operation tracking
- 🐳 Docker container lifecycle management
- 🔄 Auto-connect and auto-start capabilities
- ⚙️ Configuration via ScriptableObject
</details>

<details open>
<summary><b>🐳 Dockerized Server</b></summary>

- Built with .NET 9.0 and ASP.NET Core
- Published to GitHub Container Registry (ghcr.io)
- Multi-platform support (linux/amd64, linux/arm64)
- Full CI/CD pipeline with GitHub Actions
</details>

---

## 🏗️ Architecture

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

---

## 🚀 Quick Start

### Prerequisites

- **Unity** 2021.3 or later
- **Docker Desktop** installed and running
- **.NET 9.0 SDK** (for server development only)

### 3-Step Setup

1. **Install the package** (see [Installation](#-installation) below)
2. **Open Setup Wizard** in Unity: `Tools → Unity MCP Server → Setup Wizard`
3. **Start & Connect** via Dashboard: `Tools → Unity MCP Server → Dashboard`

✅ Done! You're ready to use AI assistants with Unity.

---

## 📦 Installation

<details open>
<summary><b>Option 1: OpenUPM (Recommended) ⭐</b></summary>

```bash
openupm add com.mezookan.unity-mcp-sharp
```
</details>

<details>
<summary><b>Option 2: Git URL</b></summary>

1. Open Unity Package Manager
2. Click `+` → "Add package from git URL..."
3. Enter: `https://github.com/Abbabon/unity-mcp-sharp.git`
</details>

<details>
<summary><b>Option 3: Manual Installation</b></summary>

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.mezookan.unity-mcp-sharp": "https://github.com/Abbabon/unity-mcp-sharp.git"
  }
}
```
</details>

### First-Time Setup

<details open>
<summary><b>Click to expand setup steps</b></summary>

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
</details>

---

## 🤖 Using with AI Assistants

<details>
<summary><b>VS Code / GitHub Copilot</b></summary>

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
</details>

<details>
<summary><b>Cursor IDE</b></summary>

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
</details>

<details>
<summary><b>Claude Desktop</b></summary>

Add to your Claude Desktop MCP configuration:

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
</details>

---

## 🛠️ Available MCP Tools

> **All tools are designed for optimal LLM interaction** with confirmation messages, tool chaining hints, and side effect warnings.

<details>
<summary><b>🔍 System & Project Information (4 tools)</b></summary>

### `unity_get_project_info`
Get Unity project metadata including name, version, active scene, paths, and editor state.

**Returns:** Project information with name, Unity version, active scene, data path, play/pause state

**💡 Tip:** Use this first when starting work on a project to understand the environment.

---

### `unity_get_console_logs`
Get recent console logs from Unity Editor (errors, warnings, debug logs).

**Returns:** Recent console logs with type, message, and stack traces

**💡 Tip:** Call this after creating scripts, entering play mode, or when compilation fails.

---

### `unity_get_compilation_status`
Check if Unity is currently compiling and if last compilation succeeded.

**Returns:** Compilation status (idle/compiling) and last compilation result

**🔗 Related:** `unity_trigger_script_compilation`, `unity_get_console_logs`

---

### `unity_trigger_script_compilation`
Force Unity to recompile all C# scripts.

**Returns:** Confirmation that compilation was triggered

**⚠️ Note:** Unity temporarily disconnects during compilation. Use `unity_get_compilation_status` after to verify success.

</details>

<details>
<summary><b>🎮 GameObjects (6 tools)</b></summary>

### `unity_create_game_object`
Create a new GameObject in the currently active scene.

**Parameters:**
- `name` (string, required): GameObject name
- `x`, `y`, `z` (float, default: 0): World position
- `components` (string, optional): Comma-separated components (e.g., "Rigidbody,BoxCollider")
- `parent` (string, optional): Parent GameObject name

**Returns:** Confirmation with name, position, components, and hierarchy location

**📌 Example:** Create a "Player" at position (0, 1, 0) with Rigidbody and CapsuleCollider

**🔗 Related:** `unity_find_game_object`, `unity_add_component_to_object`

---

### `unity_find_game_object`
Find a GameObject by name, tag, or path with detailed information.

**Parameters:**
- `name` (string, required): GameObject name
- `searchBy` (string, default: "name"): Search mode: "name", "tag", or "path"

**Returns:** Position, rotation, scale, active state, and all attached components

**🔗 Related:** `unity_list_scene_objects`, `unity_add_component_to_object`

---

### `unity_add_component_to_object`
Add a component to an existing GameObject.

**Parameters:**
- `gameObjectName` (string, required): Target GameObject
- `componentType` (string, required): Component type (e.g., "Rigidbody", "BoxCollider", custom scripts)

**Returns:** Confirmation that component was added

**💡 Tip:** Use `unity_find_game_object` first to verify the GameObject exists.

---

### `unity_list_scene_objects`
Get the complete GameObject hierarchy of the active scene.

**Returns:** Hierarchical list with active/inactive state indicators

**🔗 Related:** `unity_find_game_object`, `unity_create_game_object`

---

### `unity_batch_create_game_objects`
Create multiple GameObjects in a single operation (more efficient than one-by-one).

**Parameters:**
- `gameObjectsJson` (string, required): JSON array of GameObject specs

**Returns:** Confirmation that batch creation was initiated

---

### `unity_create_game_object_in_scene`
Create a GameObject in a specific scene (not necessarily the active one).

**Parameters:**
- `scenePath` (string, required): Scene path (e.g., "Scenes/Level1.unity")
- `name`, `x`, `y`, `z`, `components`, `parent`: Same as `unity_create_game_object`

**Returns:** Confirmation with scene path, name, and position

**⚠️ Note:** If scene is not loaded, it will be opened additively first.

</details>

<details>
<summary><b>🎬 Scenes (6 tools)</b></summary>

### `unity_list_scenes`
List all .unity scene files in the project.

**Returns:** List of scene paths relative to project root

**🔗 Related:** `unity_open_scene`, `unity_get_active_scene`

---

### `unity_get_active_scene`
Get information about the currently active scene.

**Returns:** Scene name, path, isDirty status, root GameObject count, loaded state

**💡 Tip:** Use `unity_save_scene` if isDirty is true to save changes.

---

### `unity_open_scene`
Open a Unity scene by path.

**Parameters:**
- `scenePath` (string, required): Path relative to Assets folder
- `additive` (bool, default: false): Keep other scenes open if true

**Returns:** Confirmation with scene path and mode (single/additive)

**🔗 Related:** `unity_list_scenes`, `unity_get_active_scene`

---

### `unity_close_scene`
Close a specific scene (only works with multiple scenes open).

**Parameters:**
- `sceneIdentifier` (string, required): Scene name or path

**Returns:** Confirmation that scene was closed

**⚠️ Note:** Cannot close the last open scene.

---

### `unity_save_scene`
Save the active scene or a specific scene.

**Parameters:**
- `scenePath` (string, optional): Specific scene to save (null = active)
- `saveAll` (bool, default: false): Save all open scenes

**Returns:** Confirmation of which scene(s) were saved

**⚠️ Important:** Always save after making changes, otherwise they'll be lost!

---

### `unity_set_active_scene`
Set which scene is active (where new GameObjects are created).

**Parameters:**
- `sceneIdentifier` (string, required): Scene name or path

**Returns:** Confirmation that scene is now active

**⚠️ Note:** Only works when multiple scenes are open.

</details>

<details>
<summary><b>📁 Assets & Scripts (3 tools)</b></summary>

### `unity_create_script`
Create a new C# MonoBehaviour script file.

**Parameters:**
- `scriptName` (string, required): Script name (without .cs)
- `folderPath` (string, required): Path within Assets (e.g., "Scripts/Player")
- `scriptContent` (string, required): Full C# class code

**Returns:** Confirmation with file path and recompilation notice

**🔗 Related:** `unity_get_compilation_status`, `unity_get_console_logs`

---

### `unity_create_asset`
Create any type of Unity asset (Material, Texture2D, ScriptableObject, etc.) using reflection.

**Parameters:**
- `assetName` (string, required): Asset name (without extension)
- `folderPath` (string, required): Path within Assets
- `assetTypeName` (string, required): Full type name (e.g., "UnityEngine.Material")
- `propertiesJson` (string, optional): JSON properties to set

**Returns:** Confirmation with asset name, type, and path

**📌 Example properties:**
- Material: `{"shader":"Standard","color":"#FF0000"}`
- Texture2D: `{"width":256,"height":256}`

---

### `unity_refresh_assets`
Refresh Unity Asset Database to detect file changes.

**Returns:** Confirmation that refresh was initiated

**💡 Use after:** Batch file operations or when changes aren't detected automatically

**⚠️ Note:** Can take a few seconds for large projects. Use `unity_get_compilation_status` to check if recompilation is complete.

</details>

<details>
<summary><b>▶️ Play Mode (3 tools)</b></summary>

### `unity_enter_play_mode`
Enter Unity play mode (start running the game).

**Returns:** Confirmation message with important warning

**⚠️ IMPORTANT:** Changes made in play mode are NOT saved! GameObjects created will be destroyed when exiting.

**🔗 Related:** `unity_get_play_mode_state`, `unity_exit_play_mode`

---

### `unity_exit_play_mode`
Exit Unity play mode (stop running the game).

**Returns:** Confirmation that play mode was exited

**⚠️ Note:** All changes made during play mode will be reverted.

---

### `unity_get_play_mode_state`
Get current play mode state.

**Returns:** Current state (Playing, Paused, or Stopped)

**🔗 Related:** `unity_enter_play_mode`, `unity_exit_play_mode`

</details>

<details>
<summary><b>⚙️ System Utilities (1 tool)</b></summary>

### `unity_run_menu_item`
Execute any Unity Editor menu item by its path.

**Parameters:**
- `menuPath` (string, required): Full menu path (e.g., "GameObject/Create Empty", "Edit/Undo")

**Returns:** Confirmation that menu item was executed

**💡 Use as:** Fallback for operations not covered by dedicated tools

**📌 Examples:**
- `"GameObject/Create Empty"`
- `"Edit/Undo"`
- `"Assets/Refresh"`

</details>

---

## 🐳 Docker Image

<details>
<summary><b>Pull from GitHub Container Registry</b></summary>

```bash
docker pull ghcr.io/abbabon/unity-mcp-server:latest
```
</details>

<details>
<summary><b>Run Manually</b></summary>

```bash
docker run -d \
  --name unity-mcp-server \
  -p 8080:8080 \
  --restart unless-stopped \
  ghcr.io/abbabon/unity-mcp-server:latest
```
</details>

<details>
<summary><b>Available Tags</b></summary>

| Tag | Description |
|-----|-------------|
| `latest` | Latest stable version from main branch |
| `v*.*.*` | Specific version tags (e.g., `v0.3.2`) |
| `main` | Latest build from main branch |
</details>

---

## 💻 Development

<details>
<summary><b>Development Scripts</b></summary>

The project includes convenience scripts in `Scripts~/`:

```bash
# Build server + Docker image
./Scripts~/rebuild.sh

# Start MCP server container
./Scripts~/start-mcp-server.sh

# Run smoke tests
./Scripts~/test.sh
```
</details>

<details>
<summary><b>Server Development</b></summary>

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
</details>

<details>
<summary><b>Unity Package Development</b></summary>

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
</details>

---

## ⚙️ Configuration

Access configuration via `Tools → Unity MCP Server → Create MCP Configuration` or through the Dashboard.

<details>
<summary><b>Available Settings</b></summary>

| Setting | Default | Description |
|---------|---------|-------------|
| Server URL | `ws://localhost:8080/ws` | WebSocket connection URL |
| Docker Image | `ghcr.io/abbabon/unity-mcp-server:latest` | Docker image to use |
| Auto-connect | `true` | Connect automatically on startup |
| Auto-start | `false` | Start container automatically |
| Retry Attempts | `3` | Connection retry attempts |
| Retry Delay | `2000ms` | Delay between retries |
| Verbose Logging | `false` | Enable detailed logs |
| Max Log Buffer | `1000` | Maximum log entries to keep |
</details>

---

## 🔧 Troubleshooting

<details>
<summary><b>❌ Docker not found</b></summary>

**Solution:** Install Docker Desktop and ensure it's running.

Download from [docker.com](https://www.docker.com/products/docker-desktop/)
</details>

<details>
<summary><b>❌ Connection refused</b></summary>

**Possible causes:**

1. **Docker container not running** → Start it from Dashboard
2. **Port 8080 already in use** → Change port in configuration
3. **Firewall blocking connection** → Allow Docker in firewall settings
</details>

<details>
<summary><b>❌ Container fails to start</b></summary>

**Check logs:**

```bash
docker logs unity-mcp-server
```

**Or** use the **Logs** tab in the Unity MCP Dashboard.
</details>

<details>
<summary><b>❌ "Image not found" error</b></summary>

The package will automatically pull the image on first start. If this fails:

```bash
# Manually pull the image
docker pull ghcr.io/abbabon/unity-mcp-server:latest
```
</details>

<details>
<summary><b>❌ macOS: "Docker command not found"</b></summary>

**Solution:** The package automatically checks common Docker paths on macOS:
- `/usr/local/bin/docker` (Docker Desktop)
- `/opt/homebrew/bin/docker` (Homebrew on Apple Silicon)
- `/usr/bin/docker` (Standard location)

If still not found, ensure Docker Desktop is installed and running.
</details>

For more troubleshooting help, see the [Troubleshooting Guide](Documentation~/Troubleshooting.md).

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

<details>
<summary><b>CI/CD Pipeline</b></summary>

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
</details>

---

## 📄 License

MIT License - see [LICENSE](LICENSE) file for details.

---

## 🔗 Links

### 📚 Documentation
- [Installation Guide](Documentation~/Installation.md)
- [Configuration Guide](Documentation~/Configuration.md)
- [Troubleshooting Guide](Documentation~/Troubleshooting.md)
- [Testing Guide](Documentation~/Testing.md)

### 🌐 Resources
- **Issues:** [GitHub Issues](https://github.com/Abbabon/unity-mcp-sharp/issues)
- **Model Context Protocol:** [modelcontextprotocol.io](https://modelcontextprotocol.io)
- **Docker Registry:** [ghcr.io/abbabon/unity-mcp-server](https://ghcr.io/abbabon/unity-mcp-server)

---

## 📊 Project Stats

### Language Breakdown

<div align="center">

![Language Stats](https://github-readme-stats.vercel.app/api/top-langs/?username=Abbabon&repo=unity-mcp-sharp&layout=compact&theme=github_dark&hide_border=true&langs_count=8)

</div>

### Contributors

Thanks to these wonderful people who have contributed to this project!

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Abbabon"><img src="https://avatars.githubusercontent.com/u/5870498?v=4?s=100" width="100px;" alt="AmitN"/><br /><sub><b>AmitN</b></sub></a><br /><a href="https://github.com/Abbabon/unity-mcp-sharp/commits?author=Abbabon" title="Code">💻</a> <a href="https://github.com/Abbabon/unity-mcp-sharp/commits?author=Abbabon" title="Documentation">📖</a> <a href="#infra-Abbabon" title="Infrastructure (Hosting, Build-Tools, etc)">🚇</a> <a href="#maintenance-Abbabon" title="Maintenance">🚧</a> <a href="#projectManagement-Abbabon" title="Project Management">📆</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

---

## 🙏 Credits

Created by [AmitN](https://github.com/Abbabon)

Built with:
- [Model Context Protocol SDK](https://github.com/modelcontextprotocol/csharp-sdk)
- [.NET 9.0](https://dotnet.microsoft.com/)
- [ASP.NET Core](https://asp.net/)
- [Unity](https://unity.com/)

---

<div align="center">

**Made with ❤️ for the Unity and AI communities**

## ⭐ Star History

[![Star History Chart](https://api.star-history.com/svg?repos=Abbabon/unity-mcp-sharp&type=date&legend=top-left)](https://www.star-history.com/#Abbabon/unity-mcp-sharp&type=date&legend=top-left)

</div>
