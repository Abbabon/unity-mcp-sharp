# Changelog

All notable changes to Unity MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Performance monitoring and metrics
- Advanced scene query capabilities
- Prefab instantiation support
- Build pipeline integration
- Scene View overlay for MCP operations

## [0.6.0] - 2026-01-28

### Added
- **Tool Profiles for Token Optimization:** Configurable tool profiles to reduce LLM token usage (#87, #88)
  - Three profiles: Minimal (12 tools, ~1k tokens), Standard (20 tools, ~2k tokens), Full (28 tools, ~3k tokens)
  - Configurable via Unity Dashboard Settings tab
  - Profile persists per-project in MCPConfiguration.asset
  - Server filters tool list per MCP session based on connected editor's profile
  - Auto-reconnect when profile changes; MCP clients (Cursor) need manual refresh to see updated tools
- **Auto-Focus Unity Editor:** Automatically bring Unity to foreground when MCP operations are received
  - Prevents timeout issues when Unity is minimized or behind other windows
  - Uses platform-specific APIs (SetForegroundWindow on Windows, NSApplication.activate on macOS)
  - Configurable via `autoBringToForeground` setting (default: enabled)
  - New `unity_bring_editor_to_foreground` tool for explicit control
- **Delete GameObject Tool:** New `unity_delete_game_object` tool to remove GameObjects from scene
- **Configurable Server Port:** Default port changed from 8080 to 3727 to avoid conflicts with common applications
  - New `serverPort` field in MCPConfiguration (validated range: 1024-65535)
  - Port configurable via Dashboard Configuration tab
  - URLs auto-update when port changes
  - Docker container uses `UNITY_MCP_ASPPORT` environment variable for dynamic port configuration
  - "Load Dev Config" button for quickly switching to local test image
- **Option to Disable MCP Logs:** New toggle in Dashboard to enable/disable MCP logs in Unity Console

### Changed
- **Shortened Tool Descriptions:** All 28 tool descriptions significantly shortened to reduce token usage
  - Removed verbose explanations and cross-references
  - Focused on essential information only

## [0.5.0] - 2025-11-23

### Added
- **Multi-Editor Support:** Full support for multiple Unity Editor instances connecting to a single MCP server
  - Each Unity Editor registers with unique metadata (project name, scene, machine, process ID)
  - Per-MCP-session editor selection: different LLM sessions can work with different Unity Editors simultaneously
  - Smart auto-selection: automatically selects editor if only one is connected
  - New MCP tools:
    - `unity_list_editors` - List all connected Unity Editors with metadata
    - `unity_select_editor` - Select which Unity Editor to use for the current MCP session
  - Editor selection persists across compilation reconnects (critical for Unity's recompilation workflow)

### Changed
- **Session-Aware Routing:** All MCP tools now route to the selected Unity Editor instead of broadcasting to all editors
  - Tools throw informative errors when multiple editors are connected but none is selected
  - Backwards compatible: single-editor setups work seamlessly without requiring selection
- **Editor Registration:** Unity Editors now register on connect with comprehensive metadata
  - Metadata updates automatically when scenes change
  - Server tracks editor connection state and removes disconnected editors from session mappings
- **MCP Session Context:** New middleware captures MCP session context using AsyncLocal storage
  - Enables per-session state management across async call chains
  - Each HTTP connection is treated as a unique MCP session

### Infrastructure
- **New Services:**
  - `EditorSessionManager` - Manages relationships between MCP sessions and Unity Editors
  - `McpSessionContext` - Provides async-local storage for MCP session IDs
  - `McpSessionMiddleware` - ASP.NET middleware to capture and propagate session context
- **New Models:**
  - `EditorMetadata` - Comprehensive metadata about connected Unity Editors
- **Enhanced UnityWebSocketService:**
  - `SendToEditorAsync` - Send to specific Unity Editor by connection ID
  - `SendToCurrentSessionEditorAsync` - Send to the selected editor for the current MCP session
  - `SendRequestToEditorAsync` - Send request to specific editor and await response
  - `SendRequestToCurrentSessionEditorAsync` - Send request to session's selected editor
  - Legacy broadcast methods retained for backwards compatibility

## [0.3.2] - 2025-11-16

### Fixed
- **macOS Docker Detection:** MCPServerManager now checks common Docker installation paths (`/usr/local/bin/docker`, `/opt/homebrew/bin/docker`, `/usr/bin/docker`) to resolve "Docker command not found" errors on macOS when Unity's restricted PATH doesn't include Docker binaries (fixes #1)

## [0.3.1] - 2025-11-16

### Changed
- **Package Name:** Renamed from `com.unitymcpsharp.unity-mcp` to `com.mezookan.unity-mcp-sharp`
- Updated all documentation references to new package name
- Updated OpenUPM workflow and release notes
- Updated test project package references

### Fixed
- Package name consistency across all project files
- OpenUPM scoped registry configuration in troubleshooting guide

## [0.3.0] - 2025-11-15

### Added
- **10 New MCP Tools:**
  - `unity_refresh_assets` - Refresh Unity Asset Database
  - `unity_batch_create_game_objects` - Create multiple GameObjects in one operation
  - `unity_find_game_object` - Find GameObject by name/tag/path with detailed info
  - `unity_save_scene` - Save active scene, specific scene, or all scenes
  - `unity_list_scenes` - List all .unity scene files in project
  - `unity_open_scene` - Open scene in single or additive mode
  - `unity_close_scene` - Close a specific scene
  - `unity_get_active_scene` - Get active scene information
  - `unity_set_active_scene` - Set which scene is active
  - `unity_create_game_object_in_scene` - Create GameObject in specific scene
  - `unity_run_menu_item` - Execute any Unity Editor menu item
  - `unity_create_asset` - Create any type of Unity asset (Materials, Textures, ScriptableObjects)
- **Comprehensive Operation Tracking:**
  - All 19 MCP operation handlers now log to MCPOperationTracker
  - Persistent operation log at `Temp/MCPOperations.log`
  - Operations visible in Unity MCP Dashboard
  - Configurable verbose logging and max log entries
  - Success/failure status tracking for all operations
- **Visual Feedback System:**
  - Operation tracking in MCPEditorIntegration
  - Dashboard current operation indicator
  - Recent operations log with timestamps and status
  - Customizable background color tint during operations
  - MCPConfiguration settings for visual feedback customization
- **Enhanced MCP Tool Descriptions:**
  - Rich tool descriptions with usage examples
  - Tool chaining hints (suggests related tools to use)
  - Parameter examples for better LLM understanding
  - Return value descriptions
  - Side effect warnings (e.g., play mode changes not saved)
- **Documentation Updates:**
  - CLAUDE.md: MCP Tool Best Practices section
  - CLAUDE.md: Asset Database refresh best practices
  - CLAUDE.md: JSON serialization guidelines
  - CLAUDE.md: Main thread queue pattern documentation
  - CLAUDE.md: Release process documentation

### Changed
- Improved all handlers to use Newtonsoft.Json for reliable parameter deserialization
- All handlers now execute directly on main thread (removed EditorApplication.delayCall)
- MCPConfiguration now passed to all handlers for consistent logging and tracking
- Docker workflow improved with proper attestation signing

### Fixed
- Docker workflow attestation error (missing step ID and permissions)
- Operation tracking completeness (all operations now logged)

## [0.1.0] - 2025-01-12

### Added
- Initial release of Unity MCP Server
- Dual transport architecture (MCP SSE + WebSocket JSON-RPC)
- Core MCP tools:
  - `unity_read_console_log` - Read Unity console logs
  - `unity_trigger_compilation` - Trigger script compilation
  - `unity_get_compilation_status` - Get compilation status
  - `unity_create_gameobject` - Create GameObjects in scene
  - `unity_list_scene_objects` - List scene hierarchy
  - `unity_get_project_info` - Get project information
- Unity Package features:
  - UIToolkit-based Dashboard EditorWindow
  - Docker container lifecycle management
  - Auto-connect and auto-start capabilities
  - Setup Wizard for Docker installation guidance
  - Configuration via ScriptableObject
  - Real-time console log forwarding
  - Compilation event monitoring
- Server features:
  - .NET 9.0 with ASP.NET Core
  - WebSocket service with JSON-RPC 2.0
  - Health check endpoint
  - Dockerized with multi-platform support (linux/amd64, linux/arm64)
- CI/CD Pipeline:
  - GitHub Actions for server build and test
  - Docker image publishing to GitHub Container Registry
  - OpenUPM publishing workflow
- Documentation:
  - Comprehensive README with quick start guide
  - Installation guide
  - Configuration guide
  - Troubleshooting guide
  - Development guide (CLAUDE.md)
- Licensing: MIT License

### Technical Details
- Unity compatibility: 2021.3+
- .NET version: 9.0
- Docker base image: mcr.microsoft.com/dotnet/aspnet:9.0
- MCP SDK: ModelContextProtocol 0.6.0

## Version History

### [0.1.0] - 2025-01-12
Initial public release

---

## Upgrade Guide

### From Pre-Release to 0.1.0

This is the first public release. No upgrade path needed.

## Breaking Changes

### 0.1.0
- Initial release, no breaking changes

## Deprecation Notices

None at this time.

## Security Updates

None at this time. All dependencies are up-to-date as of release.

---

For more information about changes, see the [commit history](https://github.com/Abbabon/unity-mcp-sharp/commits/main).
