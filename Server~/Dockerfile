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

# Create non-root user
RUN useradd -m -u 1000 mcpserver && chown -R mcpserver:mcpserver /app
USER mcpserver

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "UnityMcpServer.dll"]
