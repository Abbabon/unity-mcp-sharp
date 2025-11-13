# Changelog

All notable changes to Unity MCP Server will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Planned Features
- Additional MCP tools for asset manipulation
- Support for multiple Unity instances
- Performance monitoring and metrics
- Advanced scene query capabilities
- Prefab instantiation support
- Build pipeline integration

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
