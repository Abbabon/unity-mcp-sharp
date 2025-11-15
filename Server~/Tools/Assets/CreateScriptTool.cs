using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Assets;

[McpServerToolType]
public class CreateScriptTool(ILogger<CreateScriptTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<CreateScriptTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Create a new C# MonoBehaviour script file in the Unity project. The script will be created in the Assets folder and will trigger automatic Unity recompilation.")]
    public async Task UnityCreateScriptAsync(
        [Description("Name of the script (without .cs extension)")] string scriptName,
        [Description("Relative path within Assets folder (e.g., 'Scripts' or 'Scripts/Player')")] string folderPath,
        [Description("C# script content (full MonoBehaviour class code)")] string scriptContent)
    {
        _logger.LogInformation("Creating script: {ScriptName} in {FolderPath}", scriptName, folderPath);

        var parameters = new
        {
            scriptName,
            folderPath,
            scriptContent
        };

        await _webSocketService.BroadcastNotificationAsync("unity.createScript", parameters);
    }
}
