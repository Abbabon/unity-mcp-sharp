#!/bin/bash
set -e

# Kill any existing containers with this name
docker stop unity-mcp-server 2>/dev/null || true
docker rm unity-mcp-server 2>/dev/null || true

# Kill any containers from this image that don't have the name
docker ps -a --filter "ancestor=unity-mcp-server:test" --format "{{.Names}}" | while read name; do
  if [ "$name" != "unity-mcp-server" ] && [ -n "$name" ]; then
    docker stop "$name" 2>/dev/null || true
    docker rm "$name" 2>/dev/null || true
  fi
done

# Start container with:
# - Consistent name for predictability
# - Interactive mode for stdio
# - Port 3727 exposed for Unity WebSocket
# - Stdio mode enabled for MCP protocol
docker run -i --rm \
  --name unity-mcp-server \
  -p 3727:3727 \
  -e UNITY_MCP_ASPPORT=3727 \
  -e MCP_STDIO_MODE=true \
  unity-mcp-server:test
