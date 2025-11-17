# TODO - Unity MCP Server

This file tracks planned features, improvements, and enhancements for the Unity MCP Server project.

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

## 🔄 Backlog

### Features

- [ ] **Multiple Unity Instance Support**
  - Allow connecting to multiple Unity editors simultaneously
  - Add instance selection/management in Dashboard
  - Update MCP tools to support instance targeting

- [ ] **Performance Monitoring**
  - Add performance metrics collection
  - Track tool execution times
  - Add performance dashboard view
  - Expose metrics via MCP tools

- [ ] **Advanced Scene Query Capabilities**
  - Add scene graph search/filter tools
  - Component value getters/setters
  - Batch property modifications
  - Scene diff/comparison tools

- [ ] **Prefab Instantiation Support**
  - Add `unity_instantiate_prefab` tool
  - Support for prefab variants
  - Nested prefab handling
  - Prefab override management

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

### Documentation

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

### DevOps & Quality

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

## 🔬 Research & Exploration

- [ ] **Code Coverage Exploration**
  - Research best code coverage tools for C# Unity packages
  - Evaluate: Coverlet, OpenCover, dotCover
  - Determine coverage targets for different code areas
  - Document coverage strategy in CLAUDE.md

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

## 📝 Notes

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

**Last Updated:** 2025-01-17
**Maintained by:** [@Abbabon](https://github.com/Abbabon)
