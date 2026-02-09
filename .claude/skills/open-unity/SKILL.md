---
name: open-unity
description: Open the TestProject~ Unity project with the correct Unity version. Reads the version from ProjectVersion.txt and launches via Unity Hub.
---

# Open Unity Test Project

Opens the `TestProject~/` Unity project with the matching Unity Editor version.

## Instructions

Run the following command from the repository root:

```bash
PROJECT_DIR="$(git rev-parse --show-toplevel)/TestProject~" && UNITY_VERSION=$(grep "m_EditorVersion:" "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt" | sed 's/m_EditorVersion: //') && UNITY_PATH="" && for p in "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app" "/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity.app" "$HOME/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app"; do [ -d "$p" ] && UNITY_PATH="$p" && break; done && if [ -z "$UNITY_PATH" ]; then echo "Error: Unity ${UNITY_VERSION} not found. Install via Unity Hub."; else echo "Opening TestProject~ with Unity ${UNITY_VERSION}"; open -n -a "$UNITY_PATH" --args -projectPath "$PROJECT_DIR"; fi
```

This will:
1. Locate the repo root via `git rev-parse` (works from any subdirectory)
2. Read the Unity version from `TestProject~/ProjectSettings/ProjectVersion.txt`
3. Find the matching Unity installation at standard Unity Hub paths
4. Open the project with `-projectPath`, or report if the version is missing

## Notes

- The test project currently uses Unity **2022.3.62f2**
- No external scripts or repos required — the command is fully self-contained
- If the required Unity version is not installed, install it via Unity Hub
- On macOS, uses `open -n -a` to launch Unity
