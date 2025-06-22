#!/bin/bash

# Colors for terminal output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Unity MCP Server Docker Image Builder${NC}"
echo "========================================"

# Default image name
IMAGE_NAME="unity-mcp-server"

# Check if image name is provided as an argument
if [ -n "$1" ]; then
    IMAGE_NAME=$1
    echo -e "Using custom image name: ${GREEN}$IMAGE_NAME${NC}"
else
    echo -e "Using default image name: ${GREEN}$IMAGE_NAME${NC}"
fi

# Check if --no-cache option is provided
if [ "$2" = "--no-cache" ]; then
    NO_CACHE="--no-cache"
    echo -e "${YELLOW}Building without cache${NC}"
else
    NO_CACHE=""
fi

# Navigate to the server directory
echo "Building Docker image from server Dockerfile..."
cd "$(dirname "$0")"

# Build the Docker image
echo -e "${YELLOW}Running Docker build command...${NC}"
docker build $NO_CACHE -t $IMAGE_NAME -f unity-mcp-sharp-server/Dockerfile .

# Check if the build was successful
if [ $? -eq 0 ]; then
    echo -e "${GREEN}===========================================${NC}"
    echo -e "${GREEN}Docker image built successfully!${NC}"
    echo -e "${GREEN}Image name: $IMAGE_NAME${NC}"
    echo -e "${GREEN}===========================================${NC}"
    echo -e "You can run the container with:"
    echo -e "${YELLOW}docker run -p 3001:3001 $IMAGE_NAME${NC}"
else
    echo -e "\033[0;31mDocker build failed!${NC}"
    exit 1
fi
