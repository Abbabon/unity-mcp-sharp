# Configuration Guide

This guide covers how to configure the Unity MCP Server package to work with your Unity project.

## Creating Configuration Asset

The first time you use Unity MCP Server, you should create a configuration asset:

1. Go to `Tools > Unity MCP Server > Create MCP Configuration`
2. The configuration asset will be created at `Assets/Resources/MCPConfiguration.asset`
3. Select it in the Project window to view in Inspector

Alternatively, you can access and create configuration through the Dashboard:
- `Tools > Unity MCP Server > Dashboard` → Configuration tab → Save Configuration

## Configuration Settings

### Server Settings

#### Server URL
- **Default:** `ws://localhost:8080/ws`
- **Description:** WebSocket URL of the MCP server that Unity connects to
- **When to change:** If you're running the server on a different port or remote machine

**Example:**
```
ws://192.168.1.100:8080/ws  (remote server)
ws://localhost:9000/ws       (custom port)
```

#### HTTP URL
- **Default:** `http://localhost:8080`
- **Description:** HTTP URL for health checks and server info
- **When to change:** When server URL changes

#### Container Name
- **Default:** `unity-mcp-server`
- **Description:** Name of the Docker container
- **When to change:** If you want multiple instances or custom naming

#### Docker Image
- **Default:** `ghcr.io/abbabon/unity-mcp-server:latest`
- **Description:** Docker image to use for the server
- **Options:**
  - `latest` - Most recent stable version
  - `v0.1.0` - Specific version
  - `main` - Latest development build

**When to change:** To use a specific version or custom build

### Connection Settings

#### Auto Connect
- **Default:** `true`
- **Description:** Automatically connect to the MCP server when Unity starts
- **Recommendation:** Enable for seamless integration

**When to disable:**
- Working offline
- Server not always available
- Prefer manual connection control

#### Auto Start Container
- **Default:** `true`
- **Description:** Automatically start Docker container if not running
- **Requirement:** Docker Desktop must be installed and running

**When to disable:**
- Server runs on remote machine
- You manage Docker containers manually
- Development/testing scenarios

#### Retry Attempts
- **Default:** `3`
- **Range:** 0-10
- **Description:** Number of times to retry connection on failure

**Recommendation:**
- `3-5` for reliable networks
- `1-2` for development
- `5-10` for unreliable networks

#### Retry Delay (seconds)
- **Default:** `5`
- **Range:** 1-30
- **Description:** Delay between retry attempts in seconds

**Recommendation:**
- `2-5` for local connections
- `5-10` for remote connections
- `10-30` for slow/unreliable networks

### Logging Settings

#### Verbose Logging
- **Default:** `false`
- **Description:** Enable detailed logging of all MCP operations

**When to enable:**
- Debugging connection issues
- Developing new features
- Troubleshooting unexpected behavior

**Note:** Can generate a lot of console output

#### Max Log Buffer
- **Default:** `500`
- **Range:** 50-1000
- **Description:** Maximum number of console log entries to buffer for transmission to server

**Recommendation:**
- `100-300` for normal use
- `500-1000` for debugging or log-heavy projects
- `50-100` for performance-sensitive scenarios

## Configuration Presets

### Development Setup

```
Server URL: ws://localhost:8080/ws
Auto Connect: true
Auto Start Container: true
Retry Attempts: 3
Retry Delay: 5
Verbose Logging: true
Max Log Buffer: 500
```

**Use case:** Local development with detailed logging

### Production/Team Setup

```
Server URL: ws://mcp-server.company.local:8080/ws
Auto Connect: true
Auto Start Container: false
Retry Attempts: 5
Retry Delay: 10
Verbose Logging: false
Max Log Buffer: 200
```

**Use case:** Shared server, minimal logging

### Offline/Manual Setup

```
Auto Connect: false
Auto Start Container: false
Retry Attempts: 1
Verbose Logging: false
```

**Use case:** Manual control, no auto-start behavior

## Accessing Configuration in Code

If you're developing extensions or custom tools:

```csharp
using UnityMCPSharp;

// Get configuration instance
var config = MCPConfiguration.Instance;

// Access settings
Debug.Log($"Server URL: {config.serverUrl}");
Debug.Log($"Auto-connect: {config.autoConnect}");

// Modify settings (Editor only)
#if UNITY_EDITOR
config.serverUrl = "ws://localhost:9000/ws";
config.autoConnect = false;

// Save changes
UnityEditor.EditorUtility.SetDirty(config);
UnityEditor.AssetDatabase.SaveAssets();
#endif
```

## Environment-Specific Configuration

### Development vs Production

Create different configuration assets for different environments:

1. **Development:** `Assets/Resources/MCPConfiguration.asset`
   - Auto-connect, auto-start enabled
   - Verbose logging
   - Local server

2. **Production:** `Assets/Resources/MCPConfiguration_Prod.asset`
   - Remote server URL
   - Minimal logging
   - Higher retry attempts

Switch between them programmatically or manually in build process.

### Per-Platform Configuration

You can create platform-specific configurations:

```csharp
#if UNITY_EDITOR_WIN
config.serverUrl = "ws://windows-server:8080/ws";
#elif UNITY_EDITOR_OSX
config.serverUrl = "ws://mac-server:8080/ws";
#elif UNITY_EDITOR_LINUX
config.serverUrl = "ws://linux-server:8080/ws";
#endif
```

## Configuration File Location

The configuration is stored as a ScriptableObject asset:

```
Assets/
└── Resources/
    └── MCPConfiguration.asset
```

**Important:** Must be in a `Resources` folder to be loadable at runtime.

## Backup and Version Control

### Include in Version Control

Add to your `.gitignore` to exclude personal settings:

```
# Exclude personal MCP configuration
Assets/Resources/MCPConfiguration.asset
Assets/Resources/MCPConfiguration.asset.meta
```

Provide a template instead:

```
Assets/Resources/MCPConfiguration.template.asset
```

### Backup Configuration

Export configuration for backup:

1. Select `MCPConfiguration.asset` in Project window
2. Right-click → Export Package
3. Save as `MCPConfiguration_Backup.unitypackage`

Import when needed:
1. Assets → Import Package → Custom Package
2. Select backup file
3. Import

## Advanced Configuration

### Custom Server Build

If you're running a custom MCP server build:

```
Docker Image: localhost:5000/my-unity-mcp-server:dev
```

### Multiple Server Instances

Run multiple servers on different ports:

**Server 1:**
```
Container Name: unity-mcp-server-1
Server URL: ws://localhost:8080/ws
```

**Server 2:**
```
Container Name: unity-mcp-server-2
Server URL: ws://localhost:8081/ws
```

## Troubleshooting Configuration Issues

### Configuration not loading

**Symptom:** Changes to configuration don't take effect

**Solution:**
1. Ensure configuration is in `Assets/Resources/` folder
2. Restart Unity Editor
3. Check for errors in Console
4. Create new configuration if corrupted

### Auto-connect not working

**Symptom:** Unity doesn't connect on startup

**Checklist:**
- [ ] Auto Connect is enabled in configuration
- [ ] Docker Desktop is running
- [ ] Server container is running (`docker ps`)
- [ ] No firewall blocking port 8080
- [ ] Check Unity Console for errors

### Container not starting automatically

**Symptom:** Auto Start Container enabled but container doesn't start

**Checklist:**
- [ ] Docker Desktop is installed and running
- [ ] Verify: `docker --version` in terminal
- [ ] Check Docker image exists: `docker images | grep unity-mcp-server`
- [ ] Check Docker daemon is accessible
- [ ] Look for errors in Dashboard → Logs tab

## Best Practices

1. **Keep configuration in Resources folder** for runtime access
2. **Use version control for team configurations** but gitignore personal settings
3. **Document custom configurations** in your project README
4. **Test configuration changes** in Dashboard before saving
5. **Use verbose logging** only during debugging
6. **Set appropriate buffer sizes** based on project needs
7. **Configure retry settings** based on network reliability

## Related Documentation

- [Installation Guide](Installation.md) - Initial setup
- [Troubleshooting](Troubleshooting.md) - Common issues and solutions
- [README](../README.md) - Project overview

---

For more help, visit the [GitHub Issues](https://github.com/Abbabon/unity-mcp-sharp/issues) page.
