using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.System;

[McpServerToolType]
public class GetPlayModeStateTool(ILogger<GetPlayModeStateTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<GetPlayModeStateTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Check if Unity is in play mode, paused, or stopped.")]
    [return: Description("Play mode state: Playing, Paused, or Stopped")]
    public async Task<string> UnityGetPlayModeStateAsync()
    {
        _logger.LogInformation("Requesting play mode state from Unity...");

        try
        {
            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<PlayModeStateResponse>("unity.getPlayModeState", null);
            if (response != null)
            {
                return $"Play Mode State: {response.State}";
            }
            return "Unable to retrieve play mode state.";
        }
        catch (TimeoutException)
        {
            return "Request timed out. Make sure Unity Editor is running and connected.";
        }
        catch (InvalidOperationException ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}
