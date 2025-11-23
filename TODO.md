# TODO - Unity MCP Server

This file tracks planned features, improvements, and enhancements for the Unity MCP Server project.

**Based on competitive analysis of 4 Unity MCP implementations** (CoplayDev/unity-mcp, CoderGamester/mcp-unity, Arodoid/UnityMCP, IvanMurzak/Unity-MCP)

---

## 📅 Current Sprint

### High Priority

- [ ] **Code Coverage Setup**
  - Set up CodeCov integration for test coverage tracking
  - Add code coverage badge to README
  - Configure CI to report coverage on PRs
  - Target: 70%+ coverage for critical paths

- [ ] **Security Scanning**
  - Integrate Snyk for dependency vulnerability scanning
  - Add Snyk badge to README
  - Set up automated security alerts
  - Review and fix any existing vulnerabilities

### Medium Priority - README Enhancements

- [ ] **Demo GIF/Video**
  - Record screen capture of Unity MCP Dashboard in action
  - Show connection, tool execution, and live feedback
  - Add to README hero section
  - Upload to GitHub assets or use GIF hosting service

- [ ] **Typing SVG Animation**
  - Create animated tagline using readme-typing-svg
  - Showcase key features: "Unity + AI Integration", "Model Context Protocol", "Real-time Editor Control"
  - Add below main header

- [ ] **Activity Graph**
  - Add GitHub activity graph visualization
  - Use github-readme-activity-graph or similar service
  - Place in Project Stats section

### Community & Social

- [ ] **GitHub Discussions**
  - Enable GitHub Discussions for the repository
  - Add discussions badge to README
  - Create initial discussion topics:
    - Feature Requests
    - Q&A
    - Show and Tell
    - Ideas

- [ ] **Discord Server** (Optional)
  - Consider creating a Discord server for community
  - If created, add Discord badge to README
  - Set up channels for support, development, announcements

- [ ] **Sponsorship/Funding**
  - Create `.github/FUNDING.yml` file
  - Consider GitHub Sponsors, Ko-fi, or Patreon
  - Add funding badge to README if applicable

---

## 🔥 HIGH PRIORITY - Critical Feature Gaps

### Prefab System (TOP PRIORITY 🎯)

**Status:** Zero prefab support currently
**Impact:** Critical - prefabs are fundamental to Unity workflows
**Reference:** IvanMurzak/Unity-MCP, CoplayDev/unity-mcp

- [ ] **unity_create_prefab**
  - Create prefab asset from existing GameObject
  - Support prefab variants
  - Place in Assets folder with proper naming

- [ ] **unity_instantiate_prefab**
  - Spawn prefab instances in scene
  - Set position, rotation, scale
  - Support for parent assignment
  - Return instance information

- [ ] **unity_open_prefab**
  - Open prefab in isolation mode
  - Enable prefab editing context
  - Track prefab stage state

- [ ] **unity_save_prefab**
  - Persist prefab changes
  - Handle nested prefab modifications
  - Validate prefab integrity

- [ ] **unity_close_prefab_stage**
  - Exit prefab editing mode
  - Return to scene editing
  - Cleanup prefab stage

- [ ] **unity_get_prefab_info**
  - Query prefab status (is prefab, variant, instance)
  - Get prefab asset path
  - Check modification status

### GameObject & Component Lifecycle

**Status:** Can create but cannot destroy
**Impact:** High - incomplete lifecycle management
**Reference:** IvanMurzak/Unity-MCP, CoplayDev/unity-mcp

- [ ] **unity_destroy_game_object**
  - Remove GameObjects from scene
  - Support immediate vs. deferred destruction
  - Handle hierarchy (destroy children option)
  - Proper cleanup and validation

- [ ] **unity_remove_component**
  - Remove components from GameObjects
  - Validate component dependencies
  - Handle required components properly

- [ ] **unity_duplicate_game_object**
  - Clone GameObjects with all components
  - Support hierarchy duplication
  - Set position for duplicate
  - Return new GameObject information

- [ ] **unity_get_all_components_of_type**
  - Find all instances of specific component type
  - Search across all loaded scenes
  - Useful for refactoring and analysis
  - Reference: IvanMurzak

### Asset Lifecycle Operations

**Status:** Can create assets, cannot read/modify/delete/move
**Impact:** High - incomplete asset management
**Reference:** IvanMurzak/Unity-MCP, CoplayDev/unity-mcp

- [ ] **unity_read_asset**
  - Access asset properties and metadata
  - Support any asset type (materials, textures, scriptable objects, etc.)
  - Return comprehensive asset information

- [ ] **unity_modify_asset**
  - Update asset settings and properties
  - Support serialized property modification
  - Use SerializedObject API pattern

- [ ] **unity_delete_asset**
  - Remove assets from project
  - Validate no active references
  - Move to trash (soft delete) vs. permanent delete

- [ ] **unity_move_asset**
  - Relocate assets within project
  - Maintain GUID and references
  - Update meta files properly

- [ ] **unity_rename_asset**
  - Change asset names
  - Preserve GUID and references
  - Update all references automatically

- [ ] **unity_create_folder**
  - Create asset directories
  - Support nested folder creation
  - Proper meta file generation

### Script Management (Beyond Creation)

**Status:** Can create scripts only
**Impact:** High - no script maintenance capabilities
**Reference:** CoplayDev/unity-mcp (most advanced script editing)

- [ ] **unity_read_script**
  - Read existing C# script contents
  - Return full script text with line numbers
  - Support encoding detection

- [ ] **unity_edit_script** (Phase 1: Text-based)
  - Modify scripts using text replacement
  - Find and replace patterns
  - Line-based editing
  - SHA256 validation for conflict detection

- [ ] **unity_edit_script** (Phase 2: Structured)
  - Method insertion/replacement/deletion
  - Class member modifications
  - Using statement management
  - Preserve formatting and comments
  - Reference: CoplayDev's `script_apply_edits`

- [ ] **unity_delete_script**
  - Remove script files
  - Validate no active component references
  - Check compilation impact

- [ ] **unity_get_script_hash**
  - Get SHA256 hash for scripts
  - Conflict detection before editing
  - Metadata access (path, size, modified date)
  - Reference: CoplayDev

- [ ] **unity_validate_script** (see Research section for Roslyn integration)
  - Basic syntax validation before save
  - Check for common errors
  - Optional Roslyn integration for advanced validation

---

## 📊 MEDIUM PRIORITY - Workflow Enhancement

### Material & Shader Tools

**Status:** Basic material creation via unity_create_asset, no dedicated tools
**Impact:** Medium - important for rendering workflows
**Reference:** IvanMurzak/Unity-MCP

- [ ] **unity_create_material** (Dedicated tool)
  - Enhanced material creation beyond generic asset
  - Shader selection during creation
  - Property presets (PBR, Unlit, etc.)
  - Return material reference

- [ ] **unity_modify_material**
  - Change material properties (color, metallic, roughness, etc.)
  - Texture assignment
  - Shader property access
  - Batch property updates

- [ ] **unity_assign_material**
  - Link materials to renderers
  - Support multiple material slots
  - Apply to renderer components

- [ ] **unity_list_shaders**
  - Get all available shaders in project
  - Filter by type (Built-in, URP, HDRP, Custom)
  - Return shader property information

- [ ] **unity_assign_shader**
  - Change material shader
  - Preserve compatible properties
  - Handle shader migration

### Test Execution

**Status:** No test running capabilities
**Impact:** Medium - important for CI/CD integration
**Reference:** IvanMurzak/Unity-MCP, CoplayDev/unity-mcp, CoderGamester/mcp-unity

- [ ] **unity_run_tests**
  - Execute Unity Test Runner
  - Support editmode/playmode tests
  - Filter by category, assembly, name
  - Return test results with pass/fail status
  - Timeout configuration

- [ ] **unity_list_tests**
  - Query available tests
  - Group by category and assembly
  - Return test metadata

- [ ] **unity_get_test_results**
  - Access last test run results
  - Detailed failure information
  - Performance metrics

- [ ] **unity://tests/{testMode}** resource
  - MCP resource for test information
  - Reference: CoderGamester

### Package Management

**Status:** No package management capabilities
**Impact:** Medium - dependency management
**Reference:** CoderGamester/mcp-unity

- [ ] **unity_add_package**
  - Install packages via Package Manager
  - Support package names and git URLs
  - Version specification
  - Return installation status

- [ ] **unity_remove_package**
  - Uninstall packages
  - Validate no active dependencies

- [ ] **unity_list_packages**
  - Query installed packages
  - Show available updates
  - Package metadata access

- [ ] **unity://packages** resource
  - MCP resource for package information
  - Real-time package status
  - Reference: CoderGamester

### Editor Selection Management

**Status:** No selection management
**Impact:** Medium - enables context-aware operations
**Reference:** IvanMurzak/Unity-MCP, CoderGamester/mcp-unity

- [ ] **unity_get_selection**
  - Retrieve currently selected GameObjects
  - Return selection list with details
  - Support multiple selection

- [ ] **unity_set_selection**
  - Modify editor selection programmatically
  - Single or multiple object selection
  - Ping/highlight in hierarchy

- [ ] **unity_clear_selection**
  - Deselect all objects

### Additional Medium Priority

- [ ] **unity_clear_console**
  - Clear Unity console logs
  - Reference: CoplayDev

- [ ] **Layer & Tag Management**
  - Get/Add/Remove layers
  - Get/Add/Remove tags
  - Reference: IvanMurzak

- [ ] **ScriptableObject Operations**
  - Dedicated read/modify tools for ScriptableObjects
  - Currently handled by generic asset tools
  - Reference: IvanMurzak

---

## 🔬 RESEARCH & ADVANCED

### C# Execution Engine 🚀

**Status:** Not implemented (security considerations)
**Impact:** High for advanced users - powerful automation capabilities
**Security:** Requires sandboxing, timeout protection, permission system
**Reference:** Arodoid/UnityMCP (execute_editor_command), IvanMurzak/Unity-MCP (reflection system)

- [ ] **Research Phase: Security Model**
  - Investigate sandboxing approaches
  - Define allowed/disallowed APIs
  - Permission system design
  - Timeout and resource limits

- [ ] **POC: unity_execute_code**
  - Execute arbitrary C# code in editor context
  - Compile and run with Roslyn
  - Return execution results
  - Error handling and stack traces

- [ ] **Advanced: Reflection-based Method Execution**
  - Invoke methods on existing objects
  - Access Unity Editor APIs dynamically
  - Object discovery and reference passing
  - Reference: IvanMurzak's reflection-powered system

- [ ] **Safety Features**
  - Whitelist of allowed namespaces
  - Execution time limits
  - Memory constraints
  - Audit logging

**Note:** This feature is powerful but requires careful security implementation. Consider making it opt-in with clear warnings.

### Roslyn Integration 🔧

**Status:** Not implemented
**Impact:** Medium-High - improves script reliability
**Reference:** CoplayDev/unity-mcp (optional Roslyn), IvanMurzak/Unity-MCP

- [ ] **Research: Roslyn Integration Options**
  - Evaluate Roslyn NuGet packages
  - Unity compatibility assessment
  - Performance impact analysis
  - Integration architecture

- [ ] **Basic Script Validation**
  - Syntax error detection before save
  - Common error checking
  - No external dependencies required

- [ ] **Advanced Roslyn Validation** (Optional)
  - Full compilation simulation
  - Semantic analysis
  - Code fix suggestions
  - Configurable validation levels

- [ ] **unity_validate_script** tool
  - Validate script without saving
  - Return syntax errors and warnings
  - Support basic + advanced modes

**Note:** Make Roslyn integration optional/configurable to avoid unnecessary dependencies.

### Additional MCP Resources

**Status:** 7 resources currently implemented
**Reference:** CoderGamester/mcp-unity has similar resource architecture

- [ ] **unity://packages** resource
  - Package Manager information
  - Installed packages query
  - Available updates

- [ ] **unity://assets** resource
  - Asset Database queryable resource
  - Search and filter assets
  - Asset metadata access

- [ ] **unity://menu-items** resource
  - List available editor menu items
  - Enable menu item execution discovery

- [ ] **unity://gameobject/{id}** resource
  - Parameterized resource for GameObject details
  - Access by instance ID
  - Detailed component information

### Other Research Items

- [ ] **WebAssembly Server**
  - Investigate running MCP server as WASM
  - Eliminate Docker dependency
  - Direct browser-based integration
  - POC implementation

- [ ] **LLM Fine-tuning**
  - Collect usage data (with consent)
  - Fine-tune LLM for Unity-specific tasks
  - Improve tool selection accuracy
  - Better parameter inference

---

## 🔄 Backlog

### Features

- [x] **Multiple Unity Instance Support** ✅ COMPLETED (v0.5.0)
  - Multi-editor support with session routing
  - Per-session editor selection
  - Smart auto-selection
  - unity_list_editors and unity_select_editor tools

- [ ] **Performance Monitoring**
  - Add performance metrics collection
  - Track tool execution times
  - Add performance dashboard view
  - Expose metrics via MCP tools

- [ ] **Advanced Scene Query Capabilities**
  - Add scene graph search/filter tools
  - Component value getters/setters (beyond unity_set_component_field)
  - Batch property modifications
  - Scene diff/comparison tools

- [ ] **Build Pipeline Integration**
  - Add `unity_build_project` tool
  - Build configuration management
  - Platform-specific build options
  - Post-build automation hooks

- [ ] **Scene View Overlay**
  - Add MCP operations overlay in Scene View
  - Visual feedback for tool execution
  - Real-time operation status
  - Click-to-inspect scene objects

- [ ] **Workspace Integration**
  - Automatic Library/PackedCache integration for IDEs
  - Reference: CoderGamester automatic workspace setup

---

## 📚 Documentation

- [ ] **Video Tutorials**
  - Getting started tutorial
  - MCP tool usage examples
  - Integration with various AI assistants
  - Advanced workflows

- [ ] **API Documentation**
  - Generate API docs from XML comments
  - Host on GitHub Pages
  - Interactive tool explorer
  - Code examples for each tool

- [ ] **Migration Guides**
  - Version upgrade guides
  - Breaking changes documentation
  - Best practices guide
  - Performance optimization tips

- [ ] **Multilingual Documentation**
  - Consider translations (Chinese, Japanese, Spanish)
  - Reference: IvanMurzak has 4 languages, CoderGamester has 3

---

## 🧪 DevOps & Quality

- [ ] **Integration Tests**
  - Add integration test suite
  - Test MCP tool workflows end-to-end
  - Mock Unity Editor interactions
  - Automated test runs in CI

- [ ] **E2E Testing**
  - Automated UI testing for Dashboard
  - Test Docker container lifecycle
  - WebSocket connection resilience tests
  - Tool execution validation

- [ ] **Performance Benchmarks**
  - Benchmark tool execution times
  - WebSocket latency measurements
  - Memory usage profiling
  - Automated performance regression detection

- [ ] **Release Automation**
  - Fully automated changelog generation
  - Automated version bumping
  - Release notes from commits
  - Asset packaging for releases

---

## 🎨 Polish & UX

- [ ] **Dashboard Improvements**
  - Add tool execution history view
  - Real-time log filtering
  - Export logs functionality
  - Dark/light theme toggle

- [ ] **Error Handling**
  - Better error messages for common issues
  - Retry mechanisms for transient failures
  - User-friendly error dialogs
  - Error recovery suggestions

- [ ] **Localization**
  - Support for multiple languages
  - Localized error messages
  - Localized documentation
  - Community translation contributions

---

## 📝 Notes

### Competitive Analysis Summary

**Analyzed Projects:**
1. **CoplayDev/unity-mcp** (~4,000⭐) - Python-based, most popular, advanced script editing
2. **CoderGamester/mcp-unity** (~1,100⭐) - TypeScript + C#, good resource architecture
3. **IvanMurzak/Unity-MCP** (~533⭐) - C# only, 50+ tools, reflection-based, most comprehensive
4. **Arodoid/UnityMCP** (~480⭐) - TypeScript + C#, C# execution engine

**Unity MCP Sharp Unique Strengths:**
- ✅ Official C# MCP SDK (only implementation using official SDK)
- ✅ .NET 9.0 + ASP.NET Core (modern architecture)
- ✅ Multi-editor support with session routing (v0.5.0, unique feature)
- ✅ Docker containerization (easy distribution)
- ✅ LLM-optimized tool design (best practices)
- ✅ UIToolkit Dashboard with operation tracking
- ✅ SerializedObject API for complex asset creation

**Critical Feature Gaps Identified:**
1. Prefab system (0% coverage) - TOP PRIORITY
2. GameObject/Component deletion
3. Asset lifecycle (read/modify/delete/move)
4. Script management (read/edit/delete)
5. Material/Shader dedicated tools

### Implementation Strategy

**Balanced Approach:**
- Fill critical gaps (prefabs, deletion, asset lifecycle)
- Maintain architectural advantages (official SDK, multi-editor, LLM optimization)
- Add high-value features that complement strengths
- Research advanced features (C# execution, Roslyn) carefully

**Phased Rollout:**
- Phase 1: Prefab System (4-6 weeks)
- Phase 2: Lifecycle Operations (3-4 weeks) - GameObject/Component deletion, Asset CRUD
- Phase 3: Script Management (3-4 weeks) - Read/edit/delete with validation
- Phase 4: Workflow Tools (2-3 weeks) - Materials, Tests, Packages
- Phase 5: Advanced Features (research-dependent) - C# execution, Roslyn

### Code Coverage Priority

Code coverage tracking is important for:
- Ensuring critical paths are tested
- Preventing regressions
- Building confidence in releases
- Attracting contributors (shows quality)

**Action Items:**
1. Research C# coverage tools compatible with Unity packages
2. Set up coverage reporting in CI pipeline
3. Add coverage badge to README
4. Document coverage goals in contributing guide

### Badge Priority Order

✅ **Completed:**
- Build status
- Release version
- Maintenance status
- Language stats
- All contributors
- CodeQL

**Next:**
- Code coverage (high priority)
- Snyk security (high priority)
- Discussions badge (medium)
- Discord/community (if created)

---

**Last Updated:** 2025-01-23
**Maintained by:** [@Abbabon](https://github.com/Abbabon)
**Based on:** Competitive analysis of 4 Unity MCP implementations
