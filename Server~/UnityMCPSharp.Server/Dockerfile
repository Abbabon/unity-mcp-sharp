FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
# Use environment variables for port configuration
ENV SERVER_PORT=3001
ENV UNITY_BRIDGE_PORT=8090
EXPOSE ${SERVER_PORT} ${UNITY_BRIDGE_PORT}

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["UnityMCPSharp.Server/UnityMCPSharp.Server.csproj", "UnityMCPSharp.Server/"]
RUN dotnet restore "UnityMCPSharp.Server/UnityMCPSharp.Server.csproj"
COPY . .
WORKDIR "/src/UnityMCPSharp.Server"
RUN dotnet build "UnityMCPSharp.Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UnityMCPSharp.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UnityMCPSharp.Server.dll"]
