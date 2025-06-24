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
    echo "This script builds the UnityMCPSharp.Orchestrator project and copies the DLL"
    echo "to the Unity Editor Lib folder at ../Editor/Editor/Lib"
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

# Check if the Orchestrator project exists
if [ ! -d "$ORCHESTRATOR_DIR" ]; then
    echo "Error: Orchestrator project not found at $ORCHESTRATOR_DIR"
    exit 1
fi

# Create the destination directory if it doesn't exist
mkdir -p "$DESTINATION_DIR"

echo "Building UnityMCPSharp.Orchestrator in $BUILD_CONFIG configuration..."
dotnet build "$ORCHESTRATOR_DIR/UnityMCPSharp.Orchestrator.csproj" -c "$BUILD_CONFIG"

# Get the output directory and DLL path
DLL_PATH="$ORCHESTRATOR_DIR/bin/$BUILD_CONFIG/net9.0/UnityMCPSharp.Orchestrator.dll"

if [ ! -f "$DLL_PATH" ]; then
    echo "Error: Built DLL not found at $DLL_PATH"
    exit 1
fi

# Copy the DLL to the destination
echo "Copying DLL to $DESTINATION_DIR..."
cp "$DLL_PATH" "$DESTINATION_DIR/"

# Copy dependent DLLs if needed
DEPENDENCIES_DIR="$ORCHESTRATOR_DIR/bin/$BUILD_CONFIG/net9.0"
echo "Copying dependencies..."
cp "$DEPENDENCIES_DIR/Docker.DotNet.dll" "$DESTINATION_DIR/" 2>/dev/null || :
cp "$DEPENDENCIES_DIR/Newtonsoft.Json.dll" "$DESTINATION_DIR/" 2>/dev/null || :
cp "$DEPENDENCIES_DIR/CommandLine.dll" "$DESTINATION_DIR/" 2>/dev/null || :
cp "$DEPENDENCIES_DIR/System.Runtime.InteropServices.RuntimeInformation.dll" "$DESTINATION_DIR/" 2>/dev/null || :

# Check if the copy was successful
if [ -f "$DESTINATION_DIR/UnityMCPSharp.Orchestrator.dll" ]; then
    echo "Success! Orchestrator DLL has been built and copied to:"
    echo "$DESTINATION_DIR/UnityMCPSharp.Orchestrator.dll"
    exit 0
else
    echo "Error: Failed to copy the DLL to the destination directory."
    exit 1
fi
