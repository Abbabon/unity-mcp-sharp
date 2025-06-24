#!/bin/bash

# operate-server.sh
# Simple script to start and stop the Unity MCP Sharp server using the Tester app

# Function to display usage information
show_usage() {
    echo "Usage: $0 [start|stop]"
    echo ""
    echo "Commands:"
    echo "  start    Start the Unity MCP Sharp server with default settings"
    echo "  stop     Stop the running Unity MCP Sharp server"
    echo ""
    echo "Default server settings:"
    echo "  - MCP Server port: 3001"
    echo "  - Unity Bridge port: 8090"
    echo ""
}

# Function to check if dotnet is available
check_dotnet() {
    if ! command -v dotnet &> /dev/null; then
        echo "Error: dotnet command not found. Please install .NET SDK."
        exit 1
    fi
}

# Check if at least one argument was provided
if [ $# -lt 1 ]; then
    show_usage
    exit 1
fi

command=$1

# Main execution
check_dotnet

# Get the script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TESTER_DIR="$SCRIPT_DIR/UnityMCPSharp.Tester"

# Check if the Tester project exists
if [ ! -d "$TESTER_DIR" ]; then
    echo "Error: Tester project not found at $TESTER_DIR"
    exit 1
fi

# Execute the command
case "$command" in
    start)
        echo "Starting Unity MCP Sharp server..."
        dotnet run --project "$TESTER_DIR" -- start
        ;;
    stop)
        echo "Stopping Unity MCP Sharp server..."
        dotnet run --project "$TESTER_DIR" -- stop
        ;;
    *)
        echo "Error: Unknown command '$command'"
        show_usage
        exit 1
        ;;
esac

exit 0
