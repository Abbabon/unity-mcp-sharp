using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityMCPSharp
{
    /// <summary>
    /// Manages Docker container lifecycle for Unity MCP Server
    /// </summary>
    public class MCPServerManager
    {
        private static MCPServerManager _instance;
        private static MCPClient _client; // Static to survive domain reload
        private static readonly object _lock = new object();

        private MCPConfiguration _config;

        public enum ServerStatus
        {
            Unknown,
            NotInstalled,
            Stopped,
            Starting,
            Running,
            Error
        }

        public ServerStatus Status { get; private set; } = ServerStatus.Unknown;
        public string StatusMessage { get; private set; } = "Checking...";

        public event Action<ServerStatus, string> OnStatusChanged;

        /// <summary>
        /// Get or create the singleton instance
        /// </summary>
        public static MCPServerManager GetInstance(MCPConfiguration config)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MCPServerManager(config);
                    }
                }
            }
            return _instance;
        }

        private MCPServerManager(MCPConfiguration config)
        {
            _config = config;

            // Create a new client only if one doesn't exist or if we're reinitializing
            if (_client == null)
            {
                _client = new MCPClient(
                    serverUrl: _config.serverUrl,
                    autoReconnect: true,
                    reconnectAttempts: _config.retryAttempts,
                    reconnectDelay: _config.retryDelay
                );
            }
        }

        public MCPClient GetClient() => _client;

        /// <summary>
        /// Check if Docker is installed
        /// </summary>
        public async Task<bool> IsDockerInstalledAsync()
        {
            try
            {
                var (exitCode, output) = await RunCommandAsync("docker", "--version");
                return exitCode == 0 && output.Contains("Docker version");
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if the MCP server container is running
        /// </summary>
        public async Task<bool> IsContainerRunningAsync()
        {
            try
            {
                var (exitCode, output) = await RunCommandAsync("docker", $"ps -q -f name={_config.containerName}");
                return exitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if a Docker image exists locally
        /// </summary>
        private async Task<bool> IsImageAvailableLocallyAsync(string imageName)
        {
            try
            {
                var (exitCode, output) = await RunCommandAsync("docker", $"images -q {imageName}");
                return exitCode == 0 && !string.IsNullOrWhiteSpace(output);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Find available local image (tries configured image, then test tag, then latest)
        /// </summary>
        private async Task<string> FindAvailableImageAsync()
        {
            // Try configured image
            if (await IsImageAvailableLocallyAsync(_config.dockerImage))
            {
                MCPLogger.Log($"[MCPServerManager] Found configured image locally: {_config.dockerImage}");
                return _config.dockerImage;
            }

            // Try unity-mcp-server:test (built during development)
            var testImage = "unity-mcp-server:test";
            if (await IsImageAvailableLocallyAsync(testImage))
            {
                MCPLogger.Log($"[MCPServerManager] Using local development image: {testImage}");
                return testImage;
            }

            // Try unity-mcp-server:latest
            var localLatest = "unity-mcp-server:latest";
            if (await IsImageAvailableLocallyAsync(localLatest))
            {
                MCPLogger.Log($"[MCPServerManager] Using local latest image: {localLatest}");
                return localLatest;
            }

            // No local image found, will need to pull
            MCPLogger.Log($"[MCPServerManager] No local image found, will pull: {_config.dockerImage}");
            return _config.dockerImage;
        }

        /// <summary>
        /// Check server status
        /// </summary>
        public async Task UpdateStatusAsync()
        {
            if (!await IsDockerInstalledAsync())
            {
                SetStatus(ServerStatus.NotInstalled, "Docker is not installed");
                return;
            }

            if (await IsContainerRunningAsync())
            {
                SetStatus(ServerStatus.Running, "Server container is running");
            }
            else
            {
                SetStatus(ServerStatus.Stopped, "Server container is not running");
            }
        }

        /// <summary>
        /// Start the MCP server Docker container
        /// </summary>
        public async Task<bool> StartServerAsync()
        {
            SetStatus(ServerStatus.Starting, "Starting server container...");

            try
            {
                // Check if container exists but is stopped
                var (existsCode, existsOutput) = await RunCommandAsync("docker", $"ps -a -q -f name={_config.containerName}");

                if (existsCode == 0 && !string.IsNullOrWhiteSpace(existsOutput))
                {
                    // Container exists, just start it
                    MCPLogger.Log("[MCPServerManager] Starting existing container...");
                    var (startCode, startOutput) = await RunCommandAsync("docker", $"start {_config.containerName}");

                    if (startCode != 0)
                    {
                        SetStatus(ServerStatus.Error, $"Failed to start container: {startOutput}");
                        return false;
                    }
                }
                else
                {
                    // Container doesn't exist, find available image
                    MCPLogger.Log("[MCPServerManager] Creating new container...");
                    SetStatus(ServerStatus.Starting, "Looking for Docker image...");

                    var imageToUse = await FindAvailableImageAsync();
                    var dockerRunCmd = $"run -d --name {_config.containerName} -p 8080:8080 --restart unless-stopped {imageToUse}";
                    var (runCode, runOutput) = await RunCommandAsync("docker", dockerRunCmd);

                    if (runCode != 0)
                    {
                        // If image doesn't exist locally, try to pull it
                        if (runOutput.Contains("Unable to find image"))
                        {
                            MCPLogger.Log($"[MCPServerManager] Image not found locally, attempting to pull: {imageToUse}");
                            SetStatus(ServerStatus.Starting, "Downloading server image from registry...");

                            var (pullCode, pullOutput) = await RunCommandAsync("docker", $"pull {imageToUse}");
                            if (pullCode != 0)
                            {
                                var errorMsg = $"Failed to pull image '{imageToUse}'. ";
                                if (pullOutput.Contains("manifest unknown") || pullOutput.Contains("not found"))
                                {
                                    errorMsg += "Image not published yet. For development, build locally with: cd Server~ && docker build -t unity-mcp-server:test .";
                                }
                                else
                                {
                                    errorMsg += $"Error: {pullOutput}";
                                }
                                SetStatus(ServerStatus.Error, errorMsg);
                                return false;
                            }

                            // Try running again
                            MCPLogger.Log("[MCPServerManager] Pull succeeded, creating container...");
                            var (runCode2, runOutput2) = await RunCommandAsync("docker", dockerRunCmd);
                            if (runCode2 != 0)
                            {
                                SetStatus(ServerStatus.Error, $"Failed to run container: {runOutput2}");
                                return false;
                            }
                        }
                        else
                        {
                            SetStatus(ServerStatus.Error, $"Failed to run container: {runOutput}");
                            return false;
                        }
                    }
                }

                // Wait a moment for container to start
                await Task.Delay(2000);

                // Verify it's running
                if (await IsContainerRunningAsync())
                {
                    SetStatus(ServerStatus.Running, "Server started successfully");
                    return true;
                }
                else
                {
                    SetStatus(ServerStatus.Error, "Container started but not running");
                    return false;
                }
            }
            catch (Exception ex)
            {
                SetStatus(ServerStatus.Error, $"Error starting server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stop the MCP server Docker container
        /// </summary>
        public async Task<bool> StopServerAsync()
        {
            try
            {
                MCPLogger.Log("[MCPServerManager] Stopping container...");
                var (exitCode, output) = await RunCommandAsync("docker", $"stop {_config.containerName}");

                if (exitCode == 0)
                {
                    SetStatus(ServerStatus.Stopped, "Server stopped");
                    return true;
                }
                else
                {
                    SetStatus(ServerStatus.Error, $"Failed to stop container: {output}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                SetStatus(ServerStatus.Error, $"Error stopping server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get server logs from Docker container
        /// </summary>
        public async Task<string> GetServerLogsAsync(int tailLines = 100)
        {
            try
            {
                var (exitCode, output) = await RunCommandAsync("docker", $"logs --tail {tailLines} {_config.containerName}");
                return exitCode == 0 ? output : $"Error getting logs: {output}";
            }
            catch (Exception ex)
            {
                return $"Error getting logs: {ex.Message}";
            }
        }

        private void SetStatus(ServerStatus status, string message)
        {
            Status = status;
            StatusMessage = message;
            OnStatusChanged?.Invoke(status, message);

            // Log to Unity Console - use LogError for errors, Log for everything else
            if (status == ServerStatus.Error)
            {
                MCPLogger.LogError($"[MCPServerManager] {status}: {message}");
            }
            else
            {
                MCPLogger.Log($"[MCPServerManager] Status: {status} - {message}");
            }
        }

        private async Task<(int exitCode, string output)> RunCommandAsync(string command, string arguments)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // On macOS/Linux, try to find docker in common locations
                    string executablePath = command;
                    if (command == "docker" && !System.IO.File.Exists(command))
                    {
                        string[] commonPaths = {
                            "/usr/local/bin/docker",           // Docker Desktop
                            "/opt/homebrew/bin/docker",        // Homebrew on Apple Silicon
                            "/usr/bin/docker"                  // Linux standard
                        };

                        foreach (var path in commonPaths)
                        {
                            if (System.IO.File.Exists(path))
                            {
                                executablePath = path;
                                MCPLogger.LogVerbose($"[MCPServerManager] Found docker at: {path}");
                                break;
                            }
                        }
                    }

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = executablePath,
                        Arguments = arguments,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(processInfo))
                    {
                        if (process == null)
                            return (-1, "Failed to start process");

                        var output = process.StandardOutput.ReadToEnd();
                        var error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        var result = !string.IsNullOrEmpty(error) ? error : output;
                        return (process.ExitCode, result.Trim());
                    }
                }
                catch (Exception ex)
                {
                    return (-1, ex.Message);
                }
            });
        }
    }
}
