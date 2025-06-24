using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using UnityMCPSharp.Orchestrator.Models;
using UnityMCPSharp.Orchestrator.Options;
using ContainerStatus = UnityMCPSharp.Orchestrator.Models.ContainerStatus;

namespace UnityMCPSharp.Orchestrator
{
    /// <summary>
    /// Provides static methods for managing Docker containers for Unity MCP Sharp
    /// </summary>
    public static class DockerContainerManager
    {
        public static Task<ContainerOperationResult> RunStartAndReturnExitCode(OrchestratorStartOptions opts)
        {
            try
            {
                var result = StartContainerAsync
                (
                    opts.ContainerName,
                    opts.ImageName,
                    opts.ServerPort,
                    opts.UnityBridgePort,
                    null // No progress callback to avoid console logging
                );
            
                return result;
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ContainerOperationResult
                {
                    Status = ContainerStatus.Error,
                    ErrorMessage = $"Error starting container: {ex.Message}"
                });
            }
        }

        public static Task<ContainerOperationResult> RunStopAndReturnExitCode(OrchestratorStopOptions opts)
        {
            try
            {
                var result = StopContainerAsync(
                    opts.ContainerName,
                    null // No progress callback to avoid console logging
                );
            
                return result;
            }
            catch (Exception ex)
            {
                return Task.FromResult(new ContainerOperationResult
                {
                    Status = ContainerStatus.Error,
                    ErrorMessage = $"Error stopping container: {ex.Message}"
                });
            }
        }
    
        /// <summary>
        /// Creates a Docker client appropriate for the current operating system
        /// </summary>
        private static DockerClient CreateDockerClient()
        {
            string dockerApiUri;
        
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                dockerApiUri = "npipe://./pipe/docker_engine";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || 
                     RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                dockerApiUri = "unix:/var/run/docker.sock";
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported operating system");
            }

            return new DockerClientConfiguration(new Uri(dockerApiUri))
                .CreateClient();
        }

        /// <summary>
        /// Starts a Docker container with the Unity MCP Sharp server
        /// </summary>
        /// <param name="containerName">Name for the container</param>
        /// <param name="imageName">Docker image to use</param>
        /// <param name="serverPort">Port to expose the MCP server on</param>
        /// <param name="unityBridgePort">Port to expose the Unity Bridge on</param>
        /// <param name="progressCallback">Optional callback for progress updates</param>
        private static async Task<ContainerOperationResult> StartContainerAsync(
            string containerName, 
            string imageName,
            int serverPort = 3001,
            int unityBridgePort = 8090,
            Action<string>? progressCallback = null)
        {
            var result = new ContainerOperationResult();
        
            try
            {
                var dockerClient = CreateDockerClient();
                LogProgress(progressCallback, $"Connected to Docker daemon");

                // Check if image exists and pull if necessary
                var images = await dockerClient.Images.ListImagesAsync(new ImagesListParameters
                {
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["reference"] = new Dictionary<string, bool> { [imageName] = true }
                    }
                });

                if (images.Count == 0)
                {
                    LogProgress(progressCallback, $"Pulling image {imageName}...");
                    await PullImageAsync(dockerClient, imageName, progressCallback);
                }
                else
                {
                    LogProgress(progressCallback, $"Using existing image {imageName}");
                }

                // Check if container with the same name exists
                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["name"] = new Dictionary<string, bool> { [containerName] = true }
                    }
                });

                if (containers.Count > 0)
                {
                    var container = containers[0];
                    result.ContainerId = container.ID;
                
                    if (container.State == "running")
                    {
                        LogProgress(progressCallback, $"Container {containerName} is already running");
                        result.Status = ContainerStatus.AlreadyRunning;
                        return result;
                    }
                
                    LogProgress(progressCallback, $"Container {containerName} exists but is not running. Starting it...");
                    await dockerClient.Containers.StartContainerAsync(container.ID, new ContainerStartParameters());
                    LogProgress(progressCallback, $"Container {containerName} started successfully");
                    result.Status = ContainerStatus.Started;
                    return result;
                }
            
                // Create and start a new container
                LogProgress(progressCallback, $"Creating new container {containerName}...");
            
                // Port bindings for both MCP and UnityBridge ports
                var portBindings = new Dictionary<string, IList<PortBinding>>
                {
                    {
                        $"{serverPort}/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostIP = "0.0.0.0",
                                HostPort = serverPort.ToString()
                            }
                        }
                    },
                    {
                        $"{unityBridgePort}/tcp",
                        new List<PortBinding>
                        {
                            new PortBinding
                            {
                                HostIP = "0.0.0.0",
                                HostPort = unityBridgePort.ToString()
                            }
                        }
                    }
                };
            
                var createResponse = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
                {
                    Name = containerName,
                    Image = imageName,
                    ExposedPorts = new Dictionary<string, EmptyStruct>
                    {
                        { $"{serverPort}/tcp", default },
                        { $"{unityBridgePort}/tcp", default }
                    },
                    HostConfig = new HostConfig
                    {
                        PortBindings = portBindings,
                        PublishAllPorts = false
                    },
                    Env = new List<string>
                    {
                        $"SERVER_PORT={serverPort}",
                        $"UNITY_BRIDGE_PORT={unityBridgePort}"
                    }
                });
            
                result.ContainerId = createResponse.ID;
            
                LogProgress(progressCallback, $"Starting container {containerName}...");
                await dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters());
            
                LogProgress(progressCallback, $"Container {containerName} started successfully. MCP Server at port {serverPort}, Unity Bridge at port {unityBridgePort}");
                result.Status = ContainerStatus.Started;
                return result;
            }
            catch (Exception ex)
            {
                LogProgress(progressCallback, $"Error: {ex.Message}");
                result.Status = ContainerStatus.Error;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Stops a running Docker container
        /// </summary>
        /// <param name="containerName">Name of the container to stop</param>
        /// <param name="progressCallback">Optional callback for progress updates</param>
        private static async Task<ContainerOperationResult> StopContainerAsync(
            string containerName, 
            Action<string>? progressCallback = null)
        {
            var result = new ContainerOperationResult();
        
            try
            {
                var dockerClient = CreateDockerClient();
                LogProgress(progressCallback, $"Connected to Docker daemon");

                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["name"] = new Dictionary<string, bool> { [containerName] = true }
                    }
                });

                if (containers.Count == 0)
                {
                    LogProgress(progressCallback, $"Container {containerName} does not exist");
                    result.Status = ContainerStatus.NotFound;
                    return result;
                }

                var container = containers[0];
                result.ContainerId = container.ID;
            
                if (container.State != "running")
                {
                    LogProgress(progressCallback, $"Container {containerName} is not running");
                    result.Status = ContainerStatus.NotRunning;
                    return result;
                }

                LogProgress(progressCallback, $"Stopping container {containerName}...");
                await dockerClient.Containers.StopContainerAsync(container.ID, new ContainerStopParameters
                {
                    WaitBeforeKillSeconds = 10
                });
            
                LogProgress(progressCallback, $"Container {containerName} stopped successfully");
                result.Status = ContainerStatus.Stopped;
                return result;
            }
            catch (Exception ex)
            {
                LogProgress(progressCallback, $"Error: {ex.Message}");
                result.Status = ContainerStatus.Error;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }
    
        /// <summary>
        /// Gets information about a container
        /// </summary>
        /// <param name="containerName">Name of the container</param>
        private static async Task<ContainerInfo?> GetContainerInfoAsync(string containerName)
        {
            try
            {
                var dockerClient = CreateDockerClient();
            
                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters
                {
                    All = true,
                    Filters = new Dictionary<string, IDictionary<string, bool>>
                    {
                        ["name"] = new Dictionary<string, bool> { [containerName] = true }
                    }
                });

                if (containers.Count == 0)
                {
                    return null;
                }

                var container = containers[0];
                return new ContainerInfo
                {
                    Id = container.ID,
                    Name = containerName,
                    Image = container.Image,
                    Status = container.Status,
                    State = container.State
                };
            }
            catch
            {
                return null;
            }
        }

        private static async Task PullImageAsync(
            DockerClient dockerClient, 
            string imageName, 
            Action<string>? progressCallback = null)
        {
            var progress = new Progress<JSONMessage>(message =>
            {
                if (!string.IsNullOrEmpty(message.Status))
                {
                    var progressMessage = message.Progress != null && 
                                          !string.IsNullOrEmpty(message.Progress.ToString()) ? 
                        $"{message.Status} {message.Progress}" : 
                        message.Status;
                    
                    LogProgress(progressCallback, progressMessage);
                }
            });

            await dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters
                {
                    FromImage = imageName
                },
                null,
                progress
            );

            LogProgress(progressCallback, $"Image {imageName} pulled successfully");
        }
    
        private static void LogProgress(Action<string>? progressCallback, string message)
        {
            progressCallback?.Invoke(message);
        }
    }
}