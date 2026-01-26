# Quick MCP Benchmark Test

A repeatable test scenario to verify the Unity MCP plugin is working correctly. This test can be run by any LLM to validate the full MCP → Unity → Runtime pipeline in approximately 3-5 minutes.

## Purpose

This benchmark tests the core MCP functionality:
- ✅ Server connection and communication
- ✅ Project information retrieval
- ✅ Scene operations
- ✅ GameObject creation
- ✅ Script creation and compilation
- ✅ Play mode control
- ✅ Console log reading and verification

## Prerequisites

Before running this benchmark:
1. MCP Server is running (`docker run -p 3727:3727 ghcr.io/abbabon/unity-mcp-server:latest`)
2. Unity Editor is open with TestProject~ loaded
3. Unity MCP Dashboard shows "Connected ✓"

## Test Scenario: Hello World Benchmark

### Step 1: Get Project Information

**MCP Tool:** `unity_get_project_info`

**Expected Result:**
- Project name should be "TestProject" (or similar)
- Unity version returned
- Active scene name returned
- No errors

**Verification:** Tool returns valid JSON with project details.

---

### Step 2: Open Demo Scene

**MCP Tool:** `unity_open_scene`

**Parameters:**
```json
{
  "scenePath": "Assets/Scenes/Demo.unity"
}
```

**Expected Result:**
- Scene opens successfully
- Confirmation message returned

**Verification:** Scene is loaded (can verify with `unity_get_project_info` showing scene name as "Demo").

---

### Step 3: Create Test GameObject

**MCP Tool:** `unity_create_game_object`

**Parameters:**
```json
{
  "name": "HelloWorldTester",
  "x": 0,
  "y": 0,
  "z": 0
}
```

**Expected Result:**
- GameObject "HelloWorldTester" created
- Appears in Hierarchy at root level
- Confirmation message returned

**Verification:** Tool returns success confirmation.

---

### Step 4: Create Hello World Script

**MCP Tool:** `unity_create_script`

**Parameters:**
```json
{
  "scriptName": "HelloWorldTest",
  "folderPath": "Scripts",
  "scriptContent": "using UnityEngine;\n\npublic class HelloWorldTest : MonoBehaviour\n{\n    private int logCount = 0;\n    \n    void Update()\n    {\n        if (logCount < 3)\n        {\n            Debug.Log(\"Hello World!\");\n            logCount++;\n        }\n    }\n}"
}
```

**Why Update() instead of Start():** The script logs in Update() with a counter because Start() can execute before the MCP log handler is fully initialized after domain reload. Update() ensures logs are captured reliably.

**Expected Result:**
- Script file created at `Assets/Scripts/HelloWorldTest.cs`
- Compilation triggered automatically
- Confirmation message returned

**Verification:** Tool returns success confirmation.

---

### Step 5: Wait for Compilation

**MCP Tool:** `unity_get_compilation_status`

**Expected Result:**
- `isCompiling: false` (compilation complete)
- `lastCompilationSucceeded: true`

**Verification:** Wait 10-15 seconds after script creation, then poll this tool. Unity disconnects during compilation and reconnects afterward, so the first poll may fail - retry after 5 seconds if needed.

**Timeout:** If compilation takes longer than 60 seconds, something may be wrong.

---

### Step 6: Attach Script to GameObject

**MCP Tool:** `unity_add_component_to_object`

**Parameters:**
```json
{
  "gameObjectName": "HelloWorldTester",
  "componentType": "HelloWorldTest"
}
```

**Expected Result:**
- HelloWorldTest component attached to HelloWorldTester GameObject
- Confirmation message returned

**Verification:** Tool returns success confirmation.

---

### Step 7: Enter Play Mode

**MCP Tool:** `unity_enter_play_mode`

**Expected Result:**
- Unity enters play mode
- Script's `Start()` method executes
- "Hello World!" printed to console 3 times
- Confirmation message returned

**Verification:** Tool returns success confirmation.

---

### Step 8: Wait for Execution

**Action:** Wait approximately 10-15 seconds for the script to execute and logs to be captured.

This gives Unity time to:
- Complete domain reload after entering play mode
- Reconnect MCP client
- Initialize the scene and run Update() for several frames
- Capture all Debug.Log calls in the log buffer

---

### Step 9: Exit Play Mode

**MCP Tool:** `unity_exit_play_mode`

**Expected Result:**
- Unity exits play mode
- Scene returns to edit mode
- Confirmation message returned

**Verification:** Tool returns success confirmation.

---

### Step 10: Verify Console Output

**MCP Tool:** `unity_get_console_logs`

**Parameters:** None required (returns all buffered logs)

**Expected Result:**
Console logs should contain exactly 3 entries with message "Hello World!":
```
[Log] Hello World!
[Log] Hello World!
[Log] Hello World!
```

**Verification:** 
- Count occurrences of "Hello World!" in the returned logs
- Should find exactly 3 instances
- All should be of type "Log" (not Error or Warning)

---

## Success Criteria

The benchmark **PASSES** if:
- ✅ All steps complete without errors
- ✅ Console contains 2-3 "Hello World!" log entries (timing may cause 1 to be missed during domain reload)
- ✅ No compilation errors occurred
- ✅ No exceptions in console
- ✅ Cleanup successful (HelloWorldTester deleted)

The benchmark **FAILS** if:
- ❌ Any MCP tool returns an error
- ❌ Compilation fails
- ❌ No "Hello World!" entries found
- ❌ Console shows errors or exceptions

---

## Cleanup (Required)

The benchmark now includes cleanup as part of the standard flow:

1. **Delete test GameObject:**
   - `unity_delete_game_object(name: "HelloWorldTester")`
   - Verify with `unity_list_scene_objects()` - should only show Main Camera and Directional Light

2. **Test script remains:**
   - `Assets/Scripts/HelloWorldTest.cs` remains in project (harmless)
   - Can be manually deleted if desired

3. **Scene state:**
   - Don't save the scene to keep it clean for future runs

---

## Quick Reference: Tool Sequence

For LLMs, here's the exact sequence to execute:

```
1. unity_get_project_info()
2. unity_open_scene(scenePath: "Assets/Scenes/Demo.unity")
3. unity_create_game_object(name: "HelloWorldTester", x: 0, y: 0, z: 0)
4. unity_create_script(scriptName: "HelloWorldTest", folderPath: "Scripts", scriptContent: <Update-based script>)
5. <wait 10-15 seconds for compilation + reconnection>
6. unity_get_compilation_status() -- verify succeeded
7. unity_add_component_to_object(gameObjectName: "HelloWorldTester", componentType: "HelloWorldTest")
8. unity_enter_play_mode()
9. <wait 10-15 seconds for play mode + log capture>
10. unity_get_console_logs() -- verify "Hello World!" entries (2-3 expected)
11. unity_exit_play_mode()
12. unity_delete_game_object(name: "HelloWorldTester") -- cleanup
13. unity_list_scene_objects() -- verify scene is clean
```

---

## Troubleshooting

### Compilation never completes
- Check if Unity is focused (auto-focus should help)
- Check for syntax errors in the script
- Try `unity_refresh_assets` manually

### "Hello World!" not in logs
- Verify the script was attached to the GameObject
- Check if play mode was actually entered
- Look for errors in console that may have prevented execution

### MCP tools timeout
- Verify Unity is not minimized/unfocused (throttles main thread)
- Check MCP Dashboard shows "Connected"
- Verify Docker container is running

### Script not found when adding component
- Compilation may not have finished
- Poll `unity_get_compilation_status` before adding component
- Ensure script class name matches file name exactly

---

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.1 | 2026-01-26 | Fixed parameter names, tool names, increased wait times x5, switched to Update()-based script |
| 1.0 | 2025-01-26 | Initial benchmark scenario |
