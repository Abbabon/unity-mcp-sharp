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
    [Description("Create a C# script file. Triggers automatic recompilation.")]
    [return: Description("Confirmation with script path")]
    public async Task<string> UnityCreateScriptAsync(
        [Description("Script name (without .cs)")] string scriptName,
        [Description("Folder path in Assets (e.g., 'Scripts')")] string folderPath,
        [Description("Full C# script content")] string scriptContent)
    {
        _logger.LogInformation("Creating script: {ScriptName} in {FolderPath}", scriptName, folderPath);

        var parameters = new
        {
            scriptName,
            folderPath,
            scriptContent
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.createScript", parameters);

        return $"Script '{scriptName}.cs' created in Assets/{folderPath}/. Unity will now recompile scripts.";
    }
}
