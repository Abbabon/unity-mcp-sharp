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

- **Planned Tools** (Coming Soon)
  - Read Unity console logs in real-time
  - Trigger and monitor script compilation
  - Create and manipulate GameObjects in scenes
  - List scene hierarchy
  - Get project information

- **Unity Package** (OpenUPM compatible)
  - UIToolkit-based dashboard with status monitoring
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

### `unity_read_console_log`

Read recent Unity console log entries.

**Parameters:**
- `count` (int, default: 50): Number of recent log entries (max: 500)
- `logType` (string, default: "all"): Filter by type: "all", "error", "warning", "info"

**Example:**
```
Read the last 100 Unity console logs
```

### `unity_trigger_compilation`

Trigger Unity script compilation.

**Parameters:**
- `waitForCompletion` (bool, default: false): Wait for compilation to finish

**Example:**
```
Trigger Unity compilation and wait for it to complete
```

### `unity_get_compilation_status`

Get current compilation status.

**Returns:** Whether Unity is currently compiling and if last compilation succeeded.

### `unity_create_gameobject`

Create a new GameObject in the active scene.

**Parameters:**
- `name` (string, required): GameObject name
- `position` (string, default: "0,0,0"): Position as "x,y,z"
- `components` (string, optional): Comma-separated component types (e.g., "Rigidbody,BoxCollider")
- `parent` (string, optional): Parent GameObject name

**Example:**
```
Create a GameObject named "Player" at position 0,1,0 with Rigidbody and CapsuleCollider components
```

### `unity_list_scene_objects`

List all GameObjects in the active scene hierarchy.

**Parameters:**
- `includeInactive` (bool, default: true): Include inactive objects

### `unity_get_project_info`

Get Unity project information (name, version, active scene, platform, etc.).

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
