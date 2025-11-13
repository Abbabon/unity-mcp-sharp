#!/bin/bash

set -e

echo "🔨 Rebuilding Unity MCP Server..."

# Navigate to server directory
cd "$(dirname "$0")/Server~"

echo ""
echo "📦 Building .NET server..."
dotnet build -c Release

echo ""
echo "🐳 Building Docker image..."
docker build -t unity-mcp-server:test .

echo ""
echo "✅ Build complete!"
echo ""
echo "Available images:"
docker images unity-mcp-server

echo ""
echo "To restart Unity container (dual transport: stdio + WebSocket):"
echo "  docker stop unity-mcp-server 2>/dev/null || true"
echo "  docker rm unity-mcp-server 2>/dev/null || true"
echo "  docker run -i -d --name unity-mcp-server -p 8080:8080 -e MCP_STDIO_MODE=true unity-mcp-server:test"
echo ""
echo "To test MCP tools via Claude Code:"
echo "  1. Reload MCP server: /mcp → disable/enable 'unity-mcp'"
echo "  2. Use tools: unity_get_console_logs, unity_get_project_info, etc."
