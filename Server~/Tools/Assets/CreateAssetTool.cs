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
    [Description("Create any type of Unity asset (Material, Texture2D, AnimationClip, ScriptableObject, etc.) using reflection. Allows setting properties via JSON. The asset will be saved in the Assets folder with the appropriate extension. After creation, the asset will be imported by Unity automatically.")]
    [return: Description("Confirmation message with asset name, type, and file path")]
    public async Task<string> UnityCreateAssetAsync(
        [Description("Name of the asset (without extension)")] string assetName,
        [Description("Relative path within Assets folder (e.g., 'Materials', 'Textures', 'ScriptableObjects')")] string folderPath,
        [Description("Full type name of the asset to create (e.g., 'UnityEngine.Material', 'UnityEngine.Texture2D', 'YourNamespace.YourScriptableObject')")] string assetTypeName,
        [Description("JSON object with properties to set on the asset. For Material: {\"shader\":\"Standard\",\"color\":\"#FF0000\"}. For Texture2D: {\"width\":256,\"height\":256}. Etc.")] string? propertiesJson = null)
    {
        _logger.LogInformation("Creating asset: {AssetName} of type {AssetTypeName} in {FolderPath}", assetName, assetTypeName, folderPath);

        var parameters = new
        {
            assetName,
            folderPath,
            assetTypeName,
            propertiesJson
        };

        await _webSocketService.BroadcastNotificationAsync("unity.createAsset", parameters);

        var propsInfo = propertiesJson != null ? $" with properties: {propertiesJson}" : "";
        return $"Asset '{assetName}' of type '{assetTypeName}' created in Assets/{folderPath}/{propsInfo}";
    }
}
