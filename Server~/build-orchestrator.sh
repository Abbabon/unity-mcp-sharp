#!/bin/bash

# build-orchestrator.sh
# Script to build the Unity MCP Sharp orchestrator and copy the DLL to the Unity Editor Lib folder

# Exit immediately if a command exits with a non-zero status
set -e

# Function to display usage information
show_usage() {
    echo "Usage: $0 [--release]"
    echo ""
    echo "Options:"
    echo "  --release    Build in Release configuration (default is Debug)"
    echo ""
    echo "This script builds the UnityMCPSharp.Orchestrator project with its dependencies"
    echo "and copies them to the Unity Editor Lib folder at ../Editor/Lib"
}

# Parse command line arguments
BUILD_CONFIG="Debug"
if [ "$1" == "--release" ]; then
    BUILD_CONFIG="Release"
elif [ -n "$1" ] && [ "$1" != "" ]; then
    show_usage
    exit 1
fi

# Check if dotnet is available
if ! command -v dotnet &> /dev/null; then
    echo "Error: dotnet command not found. Please install .NET SDK."
    exit 1
fi

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ORCHESTRATOR_DIR="$SCRIPT_DIR/UnityMCPSharp.Orchestrator"
DESTINATION_DIR="$SCRIPT_DIR/../Editor/Lib"
TEMP_DIR="$SCRIPT_DIR/temp_publish"

# Check if the Orchestrator project exists
if [ ! -d "$ORCHESTRATOR_DIR" ]; then
    echo "Error: Orchestrator project not found at $ORCHESTRATOR_DIR"
    exit 1
fi

# Create the destination directory if it doesn't exist
mkdir -p "$DESTINATION_DIR"

# Clean up any previous temp directory
rm -rf "$TEMP_DIR"
mkdir -p "$TEMP_DIR"

echo "Publishing UnityMCPSharp.Orchestrator in $BUILD_CONFIG configuration..."
# Using dotnet publish to collect all dependencies in one place
dotnet publish "$ORCHESTRATOR_DIR/UnityMCPSharp.Orchestrator.csproj" -c "$BUILD_CONFIG" -o "$TEMP_DIR" --self-contained false

if [ ! -d "$TEMP_DIR" ]; then
    echo "Error: Publish directory not found at $TEMP_DIR"
    exit 1
fi

echo "Copying DLLs to $DESTINATION_DIR..."
# Clean destination directory of DLLs but keep .meta files
if [ -d "$DESTINATION_DIR" ]; then
    rm -f "$DESTINATION_DIR"/*.dll
fi

# Copy required DLLs to destination
cp "$TEMP_DIR/UnityMCPSharp.Orchestrator.dll" "$DESTINATION_DIR/"
cp "$TEMP_DIR/CommandLine.dll" "$DESTINATION_DIR/" 2>/dev/null || echo "Warning: CommandLine.dll not found"
cp "$TEMP_DIR/Docker.DotNet.dll" "$DESTINATION_DIR/" 2>/dev/null || echo "Warning: Docker.DotNet.dll not found"
cp "$TEMP_DIR/Newtonsoft.Json.dll" "$DESTINATION_DIR/" 2>/dev/null || echo "Warning: Newtonsoft.Json.dll not found"

# List what was copied
echo -e "\nFiles copied to $DESTINATION_DIR:"
ls -l "$DESTINATION_DIR"

# Verify critical dependencies
echo -e "\nVerifying critical dependencies..."
MISSING=0
for critical in "UnityMCPSharp.Orchestrator.dll" "CommandLine.dll" "Docker.DotNet.dll"; do
    if [ ! -f "$DESTINATION_DIR/$critical" ]; then
        echo "ERROR: Critical file $critical is missing!"
        MISSING=1
    else
        echo "✓ $critical"
    fi
done

# Clean up temp directory
echo "Cleaning up temporary files..."
rm -rf "$TEMP_DIR"

if [ $MISSING -eq 1 ]; then
    echo -e "\nERROR: Some critical dependencies are missing. The build may not work in Unity."
    exit 1
else
    echo -e "\nSuccess! All critical dependencies copied successfully."
    echo "Orchestrator DLL and dependencies have been built and copied to:"
    echo "$DESTINATION_DIR"
fi
