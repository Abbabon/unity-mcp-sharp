#!/bin/bash
set -e

echo "=== Unity MCP Server Smoke Test ==="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

function print_success() {
    echo -e "${GREEN}✅ $1${NC}"
}

function print_error() {
    echo -e "${RED}❌ $1${NC}"
}

function print_info() {
    echo -e "${YELLOW}ℹ️  $1${NC}"
}

# Cleanup function
function cleanup() {
    if [ -n "$SERVER_PID" ]; then
        print_info "Stopping server (PID: $SERVER_PID)..."
        kill $SERVER_PID 2>/dev/null || true
        wait $SERVER_PID 2>/dev/null || true
    fi

    if docker ps -q -f name=mcp-smoke-test 2>/dev/null; then
        print_info "Stopping test container..."
        docker stop mcp-smoke-test >/dev/null 2>&1 || true
        docker rm mcp-smoke-test >/dev/null 2>&1 || true
    fi
}

trap cleanup EXIT

echo "[1/6] Building .NET server..."
cd Server~
if dotnet build -c Release --nologo -v quiet; then
    print_success "Server build passed"
else
    print_error "Server build failed"
    exit 1
fi

echo ""
echo "[2/6] Running server locally..."
dotnet run --no-build -c Release --urls http://localhost:8080 > /tmp/mcp-server.log 2>&1 &
SERVER_PID=$!
print_info "Server started with PID: $SERVER_PID"
sleep 3

if ! kill -0 $SERVER_PID 2>/dev/null; then
    print_error "Server process died"
    cat /tmp/mcp-server.log
    exit 1
fi

echo ""
echo "[3/6] Testing server endpoints..."

# Test health endpoint
if curl -s -f http://localhost:8080/health | grep -q "Healthy"; then
    print_success "Health endpoint responded"
else
    print_error "Health endpoint failed"
    exit 1
fi

# Test root endpoint
if curl -s -f http://localhost:8080/ | grep -q "Unity MCP Server"; then
    print_success "Root endpoint responded"
else
    print_error "Root endpoint failed"
    exit 1
fi

# Test MCP endpoint exists
if curl -s -f -o /dev/null -w "%{http_code}" http://localhost:8080/mcp | grep -q "200\|405"; then
    print_success "MCP endpoint exists"
else
    print_error "MCP endpoint failed"
    exit 1
fi

echo ""
echo "[4/6] Stopping server..."
kill $SERVER_PID 2>/dev/null || true
wait $SERVER_PID 2>/dev/null || true
SERVER_PID=""
print_success "Server stopped"

echo ""
echo "[5/6] Building Docker image..."
if docker build -t unity-mcp-server:test . -q > /tmp/docker-build.log 2>&1; then
    print_success "Docker build passed"
else
    print_error "Docker build failed"
    cat /tmp/docker-build.log
    exit 1
fi

echo ""
echo "[6/6] Testing Docker container..."
docker run -d --name mcp-smoke-test -p 8080:8080 unity-mcp-server:test
sleep 5

# Check container is running
if ! docker ps | grep -q mcp-smoke-test; then
    print_error "Container not running"
    docker logs mcp-smoke-test
    exit 1
fi

# Test health check
if curl -s -f http://localhost:8080/health | grep -q "Healthy"; then
    print_success "Docker container health check passed"
else
    print_error "Docker container health check failed"
    docker logs mcp-smoke-test
    exit 1
fi

echo ""
echo "🎉 All smoke tests passed!"
echo ""
print_info "Test results:"
echo "  - .NET build: ✅"
echo "  - Server startup: ✅"
echo "  - Health endpoint: ✅"
echo "  - Root endpoint: ✅"
echo "  - MCP endpoint: ✅"
echo "  - Docker build: ✅"
echo "  - Docker container: ✅"
echo ""
echo "Server logs available at: /tmp/mcp-server.log"
echo "Docker build logs available at: /tmp/docker-build.log"
echo ""
print_info "Next steps for full E2E test:"
echo "  1. cd Server~ && dotnet run"
echo "  2. Open Unity test project (TestProject~)"
echo "  3. Tools → Unity MCP Server → Dashboard"
echo "  4. Click Connect"
echo "  5. Test tools using MCP Inspector:"
echo "     npx @modelcontextprotocol/inspector http://localhost:8080/mcp"
