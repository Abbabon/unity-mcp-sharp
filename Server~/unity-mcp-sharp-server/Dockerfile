FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Use environment variable for port configuration
ENV SERVER_PORT=3001
EXPOSE ${SERVER_PORT}

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["unity-mcp-sharp-server/unity-mcp-sharp-server.csproj", "unity-mcp-sharp-server/"]
RUN dotnet restore "unity-mcp-sharp-server/unity-mcp-sharp-server.csproj"
COPY . .
WORKDIR "/src/unity-mcp-sharp-server"
RUN dotnet build "unity-mcp-sharp-server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "unity-mcp-sharp-server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "unity-mcp-sharp-server.dll"]
