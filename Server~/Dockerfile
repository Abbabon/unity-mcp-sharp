# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["UnityMcpServer.csproj", "./"]
COPY ["NuGet.config", "./"]

# Restore dependencies
RUN dotnet restore "UnityMcpServer.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "UnityMcpServer.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "UnityMcpServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1000 mcpserver && chown -R mcpserver:mcpserver /app
USER mcpserver

# Copy published application
COPY --from=publish /app/publish .

# Expose ports (default 3727, can be overridden via UNITY_MCP_ASPPORT)
EXPOSE 3727

# Health check (uses UNITY_MCP_ASPPORT if set, otherwise default 3727)
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD ["/bin/sh", "-c", "curl -f http://localhost:${UNITY_MCP_ASPPORT:-3727}/health || exit 1"]

# Set environment variables (UNITY_MCP_ASPPORT overrides this in Program.cs)
ENV ASPNETCORE_URLS=http://+:3727
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "UnityMcpServer.dll"]
