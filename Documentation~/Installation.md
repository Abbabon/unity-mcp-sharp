# Installation Guide

## Prerequisites

- Unity 2021.3 or later
- Docker Desktop installed and running

## Installing via OpenUPM

### Option 1: OpenUPM CLI (Recommended)

```bash
openupm add com.unitymcpsharp.unity-mcp
```

### Option 2: Unity Package Manager

1. Open Unity Package Manager (Window > Package Manager)
2. Click the `+` button in the top-left
3. Select "Add package from git URL..."
4. Enter: `https://github.com/Abbabon/unity-mcp-sharp.git`

### Option 3: Manual Installation

1. Open your Unity project's `Packages/manifest.json`
2. Add the following to the `dependencies` section:

```json
{
  "dependencies": {
    "com.unitymcpsharp.unity-mcp": "https://github.com/Abbabon/unity-mcp-sharp.git"
  }
}
```

## Docker Setup

If you don't have Docker installed:

1. Download Docker Desktop from [docker.com](https://www.docker.com/products/docker-desktop/)
2. Install and start Docker Desktop
3. Verify installation by opening a terminal and running: `docker --version`

The Unity MCP package will automatically check for Docker and guide you through setup if needed.

## First-Time Setup

1. After installation, go to `Tools > Unity MCP Server > Dashboard` in Unity
2. The dashboard will check if Docker is installed
3. Click "Start Server" to download and run the MCP server container
4. The server will start on `http://localhost:8080`

## Verifying Installation

- Check the MCP Dashboard (Tools > Unity MCP Server > Dashboard)
- The Status tab should show "Connected" with a green indicator
- Console logs should show: "Unity MCP Server connected successfully"

## Troubleshooting

See [Troubleshooting.md](Troubleshooting.md) for common issues and solutions.
