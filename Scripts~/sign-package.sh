#!/bin/bash
# Unity Package Signing Script
# Signs the UPM package with Unity Cloud organization credentials
#
# Prerequisites:
# 1. Unity 6.3+ Editor installed
# 2. .env file with UNITY_EMAIL, UNITY_PASSWORD, UNITY_ORG_ID
# 3. OR set environment variables directly
#
# Usage:
#   ./Scripts~/sign-package.sh [version]
#   ./Scripts~/sign-package.sh 0.6.0

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Load .env file if it exists
if [ -f "$PROJECT_ROOT/.env" ]; then
    echo "Loading credentials from .env file..."
    export $(grep -v '^#' "$PROJECT_ROOT/.env" | xargs)
fi

# Validate required environment variables
if [ -z "$UNITY_EMAIL" ] || [ -z "$UNITY_PASSWORD" ] || [ -z "$UNITY_ORG_ID" ]; then
    echo "Error: Missing required environment variables."
    echo "Please set UNITY_EMAIL, UNITY_PASSWORD, and UNITY_ORG_ID"
    echo "Either in .env file or as environment variables."
    echo ""
    echo "Get your Organization ID from: https://cloud.unity.com/account/my-organizations"
    exit 1
fi

# Get version from argument or package.json
VERSION="${1:-$(jq -r '.version' "$PROJECT_ROOT/package.json")}"
OUTPUT_FILE="$PROJECT_ROOT/unity-mcp-sharp-${VERSION}.tgz"

echo "Signing Unity MCP Sharp package v${VERSION}"
echo "Output: $OUTPUT_FILE"

# Find Unity Editor
if [ -n "$UNITY_PATH" ]; then
    UNITY="$UNITY_PATH"
elif [ -d "/Applications/Unity/Hub/Editor" ]; then
    # Find latest Unity 6.x installation on macOS
    UNITY=$(find /Applications/Unity/Hub/Editor -name "Unity" -path "*/6*/MacOS/Unity" 2>/dev/null | sort -V | tail -1)
    if [ -z "$UNITY" ]; then
        # Fall back to any Unity installation
        UNITY=$(find /Applications/Unity/Hub/Editor -name "Unity" -path "*/MacOS/Unity" 2>/dev/null | sort -V | tail -1)
    fi
elif [ -d "$HOME/Unity/Hub/Editor" ]; then
    # Linux path
    UNITY=$(find "$HOME/Unity/Hub/Editor" -name "Unity" -type f 2>/dev/null | sort -V | tail -1)
fi

if [ -z "$UNITY" ] || [ ! -f "$UNITY" ]; then
    echo "Error: Unity Editor not found."
    echo "Please install Unity 6.3+ via Unity Hub, or set UNITY_PATH in .env"
    echo ""
    echo "Example: UNITY_PATH=/Applications/Unity/Hub/Editor/6000.3.0f1/Unity.app/Contents/MacOS/Unity"
    exit 1
fi

echo "Using Unity: $UNITY"

# Sign the package
echo "Signing package..."
"$UNITY" -batchmode -quit \
    -username "$UNITY_EMAIL" \
    -password "$UNITY_PASSWORD" \
    -upmPack "$PROJECT_ROOT" "$OUTPUT_FILE" \
    -cloudOrganization "$UNITY_ORG_ID" \
    -logfile -

if [ -f "$OUTPUT_FILE" ]; then
    echo ""
    echo "Package signed successfully!"
    echo "Output: $OUTPUT_FILE"
    echo ""
    echo "To verify the signature, import this .tgz file in Unity 6.3+ and check Package Manager."

    # Show file info
    ls -lh "$OUTPUT_FILE"
else
    echo "Error: Package signing failed. Check the Unity log output above."
    exit 1
fi
