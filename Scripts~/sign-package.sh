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
#   ./Scripts~/sign-package.sh [--upload] [version]
#   ./Scripts~/sign-package.sh                    # Sign only
#   ./Scripts~/sign-package.sh --upload           # Sign and upload to GitHub release
#   ./Scripts~/sign-package.sh --upload 0.6.0     # Sign specific version and upload

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Cleanup function for handling failures
cleanup() {
    local exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "Script failed with exit code $exit_code. Cleaning up..."
        rm -rf "$PROJECT_ROOT/dist" 2>/dev/null || true
    fi
}
trap cleanup EXIT

# Check dependencies
if ! command -v jq &> /dev/null; then
    echo "Error: jq is required but not installed."
    echo "Install it with: brew install jq (macOS) or apt-get install jq (Linux)"
    exit 1
fi

# Parse arguments
UPLOAD_TO_RELEASE=false
VERSION=""

while [[ $# -gt 0 ]]; do
    case $1 in
        --upload|-u)
            UPLOAD_TO_RELEASE=true
            shift
            ;;
        *)
            VERSION="$1"
            shift
            ;;
    esac
done

# Load .env file if it exists (using safer source method)
if [ -f "$PROJECT_ROOT/.env" ]; then
    echo "Loading credentials from .env file..."
    set -a
    source "$PROJECT_ROOT/.env"
    set +a
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
VERSION="${VERSION:-$(jq -r '.version' "$PROJECT_ROOT/package.json")}"
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

# Get package name from package.json
PACKAGE_NAME=$(jq -r '.name' "$PROJECT_ROOT/package.json")
OUTPUT_DIR="$PROJECT_ROOT/dist"
EXPECTED_OUTPUT="$OUTPUT_DIR/${PACKAGE_NAME}-${VERSION}.tgz"
FINAL_OUTPUT="$PROJECT_ROOT/unity-mcp-sharp-${VERSION}.tgz"

# Clean up previous output
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Sign the package (|| true because Unity CLI may return non-zero even on success)
echo "Signing package..."
"$UNITY" -batchmode -quit \
    -username "$UNITY_EMAIL" \
    -password "$UNITY_PASSWORD" \
    -upmPack "$PROJECT_ROOT" "$OUTPUT_DIR" \
    -cloudOrganization "$UNITY_ORG_ID" \
    -logfile - || true

# Unity creates: dist/{package-name}-{version}.tgz
if [ -f "$EXPECTED_OUTPUT" ]; then
    # Move to project root with simpler name
    mv "$EXPECTED_OUTPUT" "$FINAL_OUTPUT"
    rm -rf "$OUTPUT_DIR"

    echo ""
    echo "Package signed successfully!"
    echo "Output: $FINAL_OUTPUT"
    echo ""

    # Show file info
    ls -lh "$FINAL_OUTPUT"

    # Upload to GitHub release if requested
    if [ "$UPLOAD_TO_RELEASE" = true ]; then
        echo ""
        echo "Uploading to GitHub release v${VERSION}..."

        # Check if gh CLI is available
        if ! command -v gh &> /dev/null; then
            echo "Error: GitHub CLI (gh) not found. Install it with: brew install gh"
            exit 1
        fi

        # Check if release exists
        if ! gh release view "v${VERSION}" &> /dev/null; then
            echo "Error: Release v${VERSION} not found."
            echo "Create the release first by pushing a tag: git tag v${VERSION} && git push origin v${VERSION}"
            exit 1
        fi

        # Upload the file (--clobber overwrites if exists)
        gh release upload "v${VERSION}" "$FINAL_OUTPUT" --clobber

        echo ""
        echo "Uploaded successfully!"
        echo "View release: $(gh release view "v${VERSION}" --json url --jq '.url')"
    else
        echo "To upload to GitHub release, run:"
        echo "  ./Scripts~/sign-package.sh --upload"
        echo ""
        echo "Or manually:"
        echo "  gh release upload v${VERSION} $FINAL_OUTPUT"
    fi
else
    echo "Error: Package signing failed. Check the Unity log output above."
    echo "Expected output at: $EXPECTED_OUTPUT"
    ls -la "$OUTPUT_DIR" 2>/dev/null || echo "Output directory not created"
    exit 1
fi
