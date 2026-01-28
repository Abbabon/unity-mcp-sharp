using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.Assets;

[McpServerToolType]
public class CreateAssetTool(ILogger<CreateAssetTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<CreateAssetTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Create a Unity asset (Material, ScriptableObject, etc.) with optional properties.")]
    [return: Description("Confirmation with asset path")]
    public async Task<string> UnityCreateAssetAsync(
        [Description("Asset name (without extension)")] string assetName,
        [Description("Folder in Assets (e.g., 'Materials')")] string folderPath,
        [Description("Type name (e.g., 'UnityEngine.Material')")] string assetTypeName,
        [Description("JSON properties object")] string? propertiesJson = null)
    {
        _logger.LogInformation("Creating asset: {AssetName} of type {AssetTypeName} in {FolderPath}", assetName, assetTypeName, folderPath);

        var parameters = new
        {
            assetName,
            folderPath,
            assetTypeName,
            propertiesJson
        };

        await _webSocketService.SendToCurrentSessionEditorAsync("unity.createAsset", parameters);

        var propsInfo = propertiesJson != null ? $" with properties: {propertiesJson}" : "";
        return $"Asset '{assetName}' of type '{assetTypeName}' created in Assets/{folderPath}/{propsInfo}";
    }
}
