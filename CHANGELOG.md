# Changelog

All notable changes to Unity MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Support for multiple Unity instances
- Performance monitoring and metrics
- Advanced scene query capabilities
- Prefab instantiation support
- Build pipeline integration
- Scene View overlay for MCP operations

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
