using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;
using UnityMcpServer.Models;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class SetComponentFieldTool(ILogger<SetComponentFieldTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<SetComponentFieldTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Set a field or property value on a component attached to a GameObject. Can set primitive values (int, float, bool, string) or references to other GameObjects/assets by path. Use unity_find_game_object to see available components and fields first.")]
    [return: Description("Confirmation that the field was set successfully, or error message if it failed")]
    public async Task<string> UnitySetComponentFieldAsync(
        [Description("Name of the GameObject that has the component")] string gameObjectName,
        [Description("Type name of the component (e.g., 'Transform', 'Rigidbody', 'PrimitiveSpawner')")] string componentType,
        [Description("Name of the field or property to set (e.g., 'enabled', 'mass', 'config')")] string fieldName,
        [Description("Value to set - can be a primitive (number, bool, string) or asset path for references (e.g., 'Assets/ScriptableObjects/DemoPrimitives.asset')")] string value,
        [Description("Type of the value: 'string', 'int', 'float', 'bool', 'asset', 'gameObject' (default: 'string')")] string valueType = "string")
    {
        _logger.LogInformation("Setting field {FieldName} on component {ComponentType} of GameObject {GameObjectName} to {Value}",
            fieldName, componentType, gameObjectName, value);

        try
        {
            var parameters = new
            {
                gameObjectName,
                componentType,
                fieldName,
                value,
                valueType
            };

            var response = await _webSocketService.SendRequestToCurrentSessionEditorAsync<OperationResponse>("unity.setComponentField", parameters);
            if (response != null)
            {
                if (response.Success)
                {
                    return $"Successfully set {componentType}.{fieldName} = {value} on GameObject '{gameObjectName}'";
                }
                return $"Failed to set field: {response.Message}";
            }
            return "No response from Unity Editor.";
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
