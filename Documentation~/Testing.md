# Unity MCP Server - Test Checklist

## Quick Benchmark (LLM-Executable)

For a fast, automated validation of core MCP functionality, see **[QuickBenchmark.md](QuickBenchmark.md)**.

This benchmark can be executed by any LLM in under 2 minutes and tests:
- Connection and project info retrieval
- Scene operations
- GameObject and script creation
- Compilation workflow
- Play mode control
- Console log verification

---

## Server Component Tests

### Build & Run
- [ ] `dotnet restore` succeeds without errors
- [ ] `dotnet build` succeeds without errors
- [ ] `dotnet run` starts server successfully
- [ ] Server listens on port 3727
- [ ] No errors in server console output

### Endpoints
- [ ] GET `/health` returns `{"status":"Healthy"}`
- [ ] GET `/` returns server info JSON
- [ ] GET `/mcp` accepts SSE connections
- [ ] WebSocket `/ws` accepts connections
- [ ] WebSocket accepts JSON-RPC messages

### Docker
- [ ] `docker build` completes successfully
- [ ] `docker run` starts container
- [ ] Container stays running (doesn't exit)
- [ ] Container health check passes
- [ ] Container logs show no errors
- [ ] Endpoints accessible from host machine

## Unity Package Tests

### Installation
- [ ] Package installs from local path
- [ ] No compilation errors after import
- [ ] Assembly definitions load correctly
- [ ] No missing script references

### UI Components
- [ ] Dashboard window opens: `Tools > Unity MCP Server > Dashboard`
- [ ] Setup Wizard opens: `Tools > Unity MCP Server > Setup Wizard`
- [ ] All menu items appear under `Tools > Unity MCP Server`
- [ ] Dashboard tabs render (Status, Configuration, Logs)
- [ ] Configuration fields are editable
- [ ] All buttons are clickable

### Docker Integration
- [ ] Docker detection works (detects if Docker is installed)
- [ ] Docker status detection works (detects if container is running)
- [ ] "Start Server" button works
- [ ] "Stop Server" button works
- [ ] Server logs display in Logs tab
- [ ] Docker image pulls on first start

## Integration Tests

### Connection
- [ ] Unity connects to server via WebSocket
- [ ] Connection status shows "Connected ✓" in green
- [ ] Console logs: "Unity MCP Server connected successfully"
- [ ] Server logs: "Unity Editor connected"
- [ ] Disconnect button works
- [ ] Reconnect after disconnect works

### MCP Tools (via Inspector)

#### unity_get_project_info
- [ ] Tool appears in MCP Inspector
- [ ] Returns project name
- [ ] Returns Unity version
- [ ] Returns active scene name
- [ ] Returns platform info

#### unity_create_gameobject
- [ ] Creates GameObject with correct name
- [ ] GameObject appears in Hierarchy
- [ ] Position is set correctly
- [ ] Components are added correctly
- [ ] Parent relationship works

#### unity_trigger_compilation
- [ ] Unity shows "Compiling..." indicator
- [ ] Console logs: "Compilation started"
- [ ] Compilation completes
- [ ] Console logs: "Compilation finished"

#### unity_get_compilation_status
- [ ] Returns isCompiling status
- [ ] Returns lastCompilationSucceeded status

#### unity_read_console_log
- [ ] Returns recent console logs
- [ ] Filtering by logType works
- [ ] Count parameter works
- [ ] Includes errors, warnings, and info logs

#### unity_list_scene_objects
- [ ] Returns scene hierarchy
- [ ] Shows nested objects correctly
- [ ] includeInactive parameter works

### Real-time Events

#### Log Forwarding
- [ ] Unity logs appear in server in real-time
- [ ] Log type is preserved (Error, Warning, Info)
- [ ] Stack traces are included

#### Compilation Events
- [ ] Server receives "compilationStarted" notification
- [ ] Server receives "compilationFinished" notification
- [ ] Success/failure status is correct

## Performance Tests
- [ ] Server handles multiple rapid requests
- [ ] WebSocket connection stays stable over time
- [ ] Memory usage is reasonable
- [ ] No memory leaks over extended use
- [ ] Unity Editor remains responsive

## Error Handling
- [ ] Server handles invalid JSON-RPC messages
- [ ] Unity handles server disconnection gracefully
- [ ] Auto-reconnect works after network interruption
- [ ] Invalid tool parameters return error messages
- [ ] Timeout handling works correctly

## Documentation Tests
- [ ] README instructions are accurate
- [ ] Installation guide works as written
- [ ] Configuration guide matches actual settings
- [ ] Troubleshooting guide addresses real issues
- [ ] Code examples compile and run

## Cross-Platform Tests

### macOS
- [ ] Docker Desktop detects correctly
- [ ] Server runs in Docker
- [ ] Unity package works

### Windows
- [ ] Docker Desktop detects correctly
- [ ] Server runs in Docker
- [ ] Unity package works
- [ ] PowerShell commands work

### Linux
- [ ] Docker detects correctly
- [ ] Server runs in Docker
- [ ] Unity package works

## CI/CD Tests
- [ ] build-server.yml workflow succeeds
- [ ] publish-docker.yml workflow succeeds
- [ ] Docker image pushes to ghcr.io
- [ ] Multi-arch build works (amd64, arm64)

---

## Test Results Summary

**Date:** _______________
**Tester:** _______________
**Unity Version:** _______________
**Docker Version:** _______________
**OS:** _______________

**Overall Status:** ☐ PASS ☐ FAIL ☐ PARTIAL

**Notes:**
