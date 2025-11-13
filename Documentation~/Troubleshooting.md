# Troubleshooting Guide

This guide covers common issues you may encounter when using Unity MCP Server and their solutions.

## Table of Contents

- [Installation Issues](#installation-issues)
- [Docker Issues](#docker-issues)
- [Connection Issues](#connection-issues)
- [Server Issues](#server-issues)
- [Unity Integration Issues](#unity-integration-issues)
- [Performance Issues](#performance-issues)
- [Getting Help](#getting-help)

## Installation Issues

### Package not appearing in Unity

**Symptom:** Installed package but can't find it in Unity

**Solution:**
1. Check Unity Package Manager (Window → Package Manager)
2. Ensure you're viewing "In Project" packages
3. Refresh package list
4. Check `Packages/manifest.json` contains the package entry
5. Restart Unity Editor

### OpenUPM installation fails

**Symptom:** `openupm add` command fails

**Solution:**
```bash
# Update OpenUPM CLI
npm install -g openupm-cli

# Try again
openupm add com.unitymcpsharp.unity-mcp

# If still failing, add manually to manifest.json
```

Add to `Packages/manifest.json`:
```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": ["com.unitymcpsharp"]
    }
  ],
  "dependencies": {
    "com.unitymcpsharp.unity-mcp": "0.1.0"
  }
}
```

### Git URL installation fails

**Symptom:** "Unable to add package from git URL"

**Solution:**
1. Verify Git is installed: `git --version`
2. Check you have network access
3. Try HTTPS URL instead of SSH
4. Check firewall/proxy settings
5. Download and install as local package

## Docker Issues

### Docker not found

**Symptom:** "Docker is not installed" in Setup Wizard

**Solution:**
1. Download Docker Desktop: https://www.docker.com/products/docker-desktop/
2. Install Docker Desktop
3. Start Docker Desktop
4. Wait for Docker to fully start (check system tray icon)
5. Verify installation:
   ```bash
   docker --version
   docker ps
   ```
6. Restart Unity
7. Re-check in Setup Wizard

### Docker daemon not running

**Symptom:** "Cannot connect to the Docker daemon"

**Solution:**
- **macOS/Windows:** Start Docker Desktop from Applications
- **Linux:**
  ```bash
  sudo systemctl start docker
  sudo systemctl enable docker  # Start on boot
  ```

Verify Docker is running:
```bash
docker info
```

### Permission denied accessing Docker

**Symptom:** "permission denied while trying to connect to Docker daemon"

**Solution:**

**Linux:**
```bash
# Add your user to docker group
sudo usermod -aG docker $USER

# Log out and log back in, or:
newgrp docker

# Verify
docker ps
```

**macOS/Windows:** Docker Desktop should handle permissions automatically

### Container fails to start

**Symptom:** Container starts but immediately stops

**Diagnostic:**
```bash
# Check container status
docker ps -a

# View container logs
docker logs unity-mcp-server

# Check for port conflicts
lsof -i :8080  # macOS/Linux
netstat -ano | findstr :8080  # Windows
```

**Common causes:**

1. **Port 8080 already in use**
   ```bash
   # Find process using port 8080
   lsof -i :8080

   # Kill the process or change MCP server port
   docker run -p 8081:8080 ghcr.io/abbabon/unity-mcp-server

   # Update Unity configuration to use port 8081
   ```

2. **Insufficient memory**
   - Increase Docker memory limit in Docker Desktop settings
   - Recommended: At least 2GB

3. **Image corrupted**
   ```bash
   # Remove old image
   docker rmi ghcr.io/abbabon/unity-mcp-server

   # Pull fresh image
   docker pull ghcr.io/abbabon/unity-mcp-server:latest
   ```

### Image pull fails

**Symptom:** "Error pulling image"

**Solution:**
```bash
# Check Docker Hub status
curl https://status.docker.com/

# Try pulling manually
docker pull ghcr.io/abbabon/unity-mcp-server:latest

# If behind proxy, configure Docker proxy settings
# Docker Desktop → Settings → Resources → Proxies

# Alternative: Download image from GitHub Releases
```

## Connection Issues

### "Connection refused" error

**Symptom:** Unity can't connect to server

**Diagnostic Checklist:**
- [ ] Server container is running: `docker ps | grep unity-mcp-server`
- [ ] Server is healthy: `curl http://localhost:8080/health`
- [ ] Correct URL in configuration: `ws://localhost:8080/ws`
- [ ] No firewall blocking port 8080
- [ ] Docker is running

**Solutions:**

1. **Start server if not running:**
   - Unity Dashboard → Start Server
   - Or manually: `docker start unity-mcp-server`

2. **Check server health:**
   ```bash
   curl http://localhost:8080/health
   # Should return: {"status":"Healthy"}

   curl http://localhost:8080/
   # Should return server info
   ```

3. **Verify WebSocket endpoint:**
   - Use WebSocket test tool: https://www.piesocket.com/websocket-tester
   - Connect to: `ws://localhost:8080/ws`
   - Should connect successfully

4. **Check firewall:**
   - macOS: System Preferences → Security & Privacy → Firewall
   - Windows: Windows Defender Firewall → Allow app
   - Add Docker Desktop to allowed apps

### Connection timeout

**Symptom:** Connection attempt times out after 30 seconds

**Solution:**
1. Increase retry delay in configuration
2. Check network latency if using remote server:
   ```bash
   ping your-server-hostname
   ```
3. Verify server is responding:
   ```bash
   telnet localhost 8080
   ```
4. Check Docker container logs for errors:
   ```bash
   docker logs unity-mcp-server
   ```

### Frequent disconnections

**Symptom:** Connection drops repeatedly

**Possible causes:**

1. **Network instability**
   - Check network connection
   - Try wired connection instead of WiFi
   - Reduce distance from router

2. **Server resource issues**
   - Check Docker container resource usage:
     ```bash
     docker stats unity-mcp-server
     ```
   - Increase Docker memory if needed

3. **Firewall interference**
   - Temporarily disable firewall to test
   - Add permanent exception if that resolves it

4. **Proxy/VPN conflicts**
   - Try disabling VPN
   - Configure proxy settings in Docker

## Server Issues

### Server not responding

**Symptom:** Server running but not responding to requests

**Diagnostic:**
```bash
# Check if server is alive
curl http://localhost:8080/health

# Check server logs
docker logs unity-mcp-server --tail 100

# Check server processes
docker exec unity-mcp-server ps aux
```

**Solution:**
```bash
# Restart container
docker restart unity-mcp-server

# If still not responding, stop and remove
docker stop unity-mcp-server
docker rm unity-mcp-server

# Start fresh
docker run -d --name unity-mcp-server -p 8080:8080 \
  ghcr.io/abbabon/unity-mcp-server:latest
```

### Server crashes on startup

**Symptom:** Container starts then immediately exits

**Check logs:**
```bash
docker logs unity-mcp-server
```

**Common errors:**

1. **"Address already in use"**
   - Port 8080 is taken
   - Solution: Change port or kill conflicting process

2. **"Permission denied"**
   - Docker permission issue
   - Solution: Run Docker Desktop as administrator (Windows) or fix permissions (Linux)

3. **".NET runtime not found"**
   - Image corruption
   - Solution: Re-pull image

### Slow server performance

**Symptom:** Server responds slowly to requests

**Solution:**
1. Increase Docker resources:
   - Docker Desktop → Settings → Resources
   - Increase CPU: 2+ cores
   - Increase Memory: 4GB+

2. Check host system resources:
   ```bash
   # macOS/Linux
   top

   # Windows
   Task Manager → Performance
   ```

3. Reduce logging level:
   - Disable verbose logging in Unity configuration
   - Reduce max log buffer size

## Unity Integration Issues

### Tools not working

**Symptom:** MCP tools execute but nothing happens in Unity

**Diagnostic:**
1. Check Unity Console for errors
2. Enable verbose logging in MCP Configuration
3. Check Dashboard → Logs tab
4. Verify Unity Editor is not paused or busy

**Common issues:**

1. **"Method not found"**
   - Unity package may be outdated
   - Solution: Update package to latest version

2. **"Permission denied"**
   - Unity Editor API restrictions
   - Solution: Ensure Unity is in Play Mode if required

3. **"Scene not loaded"**
   - Trying to manipulate objects with no scene open
   - Solution: Open a scene first

### Console logs not appearing in server

**Symptom:** Unity logs not visible in MCP tools

**Solution:**
1. Check connection status (must be connected)
2. Verify `MCPEditorIntegration` is active:
   - Look for "[MCPEditorIntegration] Initialized" in console
3. Check log buffer size in configuration
4. Restart Unity if integration didn't initialize

### Compilation requests not working

**Symptom:** `unity_trigger_compilation` tool does nothing

**Solution:**
1. Check if Unity is already compiling
2. Ensure no compile errors blocking compilation
3. Check Unity Console for script errors
4. Try manual compilation: `Assets → Reimport All`

### GameObject creation fails

**Symptom:** `unity_create_gameobject` returns success but object not created

**Checklist:**
- [ ] Scene is open and loaded
- [ ] Not in Play Mode (unless intentional)
- [ ] Check Hierarchy window
- [ ] Check if object created but hidden

**Solution:**
```csharp
// Verify object was created
var obj = GameObject.Find("YourObjectName");
Debug.Log(obj != null ? "Found!" : "Not found");
```

## Performance Issues

### High memory usage

**Symptom:** Unity MCP uses excessive memory

**Solution:**
1. Reduce max log buffer size
2. Disable verbose logging
3. Clear log buffer periodically:
   - Restart Unity
   - Or clear in Dashboard

### Unity Editor slowdown

**Symptom:** Unity becomes slow when MCP is active

**Solution:**
1. Disable auto-connect if not needed
2. Reduce log buffer size
3. Disable real-time log forwarding:
   - Modify `MCPEditorIntegration.cs`
   - Comment out real-time log forwarding in `OnLogMessageReceived`

### Docker resource consumption

**Symptom:** Docker uses too much CPU/memory

**Check usage:**
```bash
docker stats unity-mcp-server
```

**Solution:**
1. Limit container resources:
   ```bash
   docker update unity-mcp-server --cpus=1 --memory=512m
   ```

2. Or recreate with limits:
   ```bash
   docker run -d --name unity-mcp-server \
     -p 8080:8080 \
     --cpus=1 \
     --memory=512m \
     ghcr.io/abbabon/unity-mcp-server:latest
   ```

## IDE Integration Issues

### MCP not appearing in VS Code

**Symptom:** MCP server not available in VS Code

**Solution:**
1. Verify `.vscode/settings.json` configuration:
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

2. Restart VS Code
3. Check VS Code output panel for errors
4. Verify server is running: `curl http://localhost:8080/mcp`

### Tools not showing in Cursor

**Symptom:** Unity tools not available in Cursor IDE

**Solution:**
1. Check Cursor MCP configuration
2. Restart Cursor IDE
3. Use MCP Inspector to verify tools are exposed:
   ```bash
   npx @modelcontextprotocol/inspector http://localhost:8080/mcp
   ```

## Getting Help

### Enable verbose logging

For better diagnostics:

1. Open Unity MCP Dashboard
2. Configuration tab
3. Enable "Verbose Logging"
4. Save Configuration
5. Restart Unity
6. Reproduce issue
7. Check Unity Console for detailed logs

### Collect diagnostic information

When reporting issues, include:

```bash
# System info
unity --version          # Unity version
docker --version         # Docker version
dotnet --version         # .NET version (if developing)

# Server logs
docker logs unity-mcp-server --tail 200 > mcp-server.log

# Container status
docker ps -a | grep unity-mcp > container-status.txt

# Network test
curl -v http://localhost:8080/health > health-check.txt
```

### Reporting Issues

1. Search existing issues: https://github.com/Abbabon/unity-mcp-sharp/issues
2. Create new issue with:
   - Clear description of problem
   - Steps to reproduce
   - Expected vs actual behavior
   - Diagnostic information (above)
   - Screenshots if applicable
   - Unity version
   - Package version

### Community Support

- **GitHub Discussions:** https://github.com/Abbabon/unity-mcp-sharp/discussions
- **MCP Discord:** https://discord.gg/modelcontextprotocol
- **Unity Forums:** Tag with "MCP" or "unity-mcp-sharp"

## Additional Resources

- [Installation Guide](Installation.md)
- [Configuration Guide](Configuration.md)
- [README](../README.md)
- [Development Guide](../CLAUDE.md)

---

**Still stuck?** [Open an issue](https://github.com/Abbabon/unity-mcp-sharp/issues/new) and we'll help!
