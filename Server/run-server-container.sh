#!/bin/bash

# Colors for terminal output
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${YELLOW}Unity MCP Server Docker Container Runner${NC}"
echo "==========================================="

# Default values
IMAGE_NAME="unity-mcp-server"
CONTAINER_NAME="unity-mcp-server-container"
HOST_PORT=3001
CONTAINER_PORT=3001

# Check if image name is provided as an argument
if [ -n "$1" ]; then
    IMAGE_NAME=$1
fi

# Check if container name is provided
if [ -n "$2" ]; then
    CONTAINER_NAME=$2
fi

# Check if host port is provided
if [ -n "$3" ]; then
    HOST_PORT=$3
fi

echo -e "Image name: ${GREEN}$IMAGE_NAME${NC}"
echo -e "Container name: ${GREEN}$CONTAINER_NAME${NC}"
echo -e "Port mapping: ${GREEN}$HOST_PORT:$CONTAINER_PORT${NC}"

# Check if the container already exists and remove it
EXISTING_CONTAINER=$(docker ps -a -q -f name=$CONTAINER_NAME)
if [ -n "$EXISTING_CONTAINER" ]; then
    echo -e "${YELLOW}Removing existing container $CONTAINER_NAME...${NC}"
    docker rm -f $CONTAINER_NAME > /dev/null 2>&1
fi

# Run the container
echo -e "${YELLOW}Starting the server container...${NC}"
docker run -d --name $CONTAINER_NAME -p $HOST_PORT:$CONTAINER_PORT -e SERVER_PORT=$CONTAINER_PORT $IMAGE_NAME

# Check if the container is running
if [ $? -eq 0 ]; then
    echo -e "${GREEN}===========================================${NC}"
    echo -e "${GREEN}Container started successfully!${NC}"
    echo -e "${GREEN}Container name: $CONTAINER_NAME${NC}"
    echo -e "${GREEN}The server is now running at: http://localhost:$HOST_PORT${NC}"
    echo -e "${GREEN}===========================================${NC}"
    echo -e "To view container logs: ${YELLOW}docker logs $CONTAINER_NAME${NC}"
    echo -e "To stop the container: ${YELLOW}docker stop $CONTAINER_NAME${NC}"
    echo -e "To remove the container: ${YELLOW}docker rm $CONTAINER_NAME${NC}"
else
    echo -e "\033[0;31mFailed to start the container!${NC}"
    exit 1
fi
