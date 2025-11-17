using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using UnityMcpServer.Services;

namespace UnityMcpServer.Tools.GameObjects;

[McpServerToolType]
public class SetComponentFieldTool(ILogger<SetComponentFieldTool> logger, UnityWebSocketService webSocketService)
{
    private readonly ILogger<SetComponentFieldTool> _logger = logger;
    private readonly UnityWebSocketService _webSocketService = webSocketService;

    [McpServerTool]
    [Description("Set a field or property value on a component attached to a GameObject. Can set primitive values (int, float, bool, string) or references to other GameObjects/assets by path. Use unity_find_game_object to see available components and fields first.")]
    [return: Description("Confirmation that the field was set successfully")]
    public async Task<string> UnitySetComponentFieldAsync(
        [Description("Name of the GameObject that has the component")] string gameObjectName,
        [Description("Type name of the component (e.g., 'Transform', 'Rigidbody', 'PrimitiveSpawner')")] string componentType,
        [Description("Name of the field or property to set (e.g., 'enabled', 'mass', 'config')")] string fieldName,
        [Description("Value to set - can be a primitive (number, bool, string) or asset path for references (e.g., 'Assets/ScriptableObjects/DemoPrimitives.asset')")] string value,
        [Description("Type of the value: 'string', 'int', 'float', 'bool', 'asset', 'gameObject' (default: 'string')")] string valueType = "string")
    {
        _logger.LogInformation("Setting field {FieldName} on component {ComponentType} of GameObject {GameObjectName} to {Value}",
            fieldName, componentType, gameObjectName, value);

        await _webSocketService.BroadcastNotificationAsync("unity.setComponentField", new
        {
            gameObjectName,
            componentType,
            fieldName,
            value,
            valueType
        });

        return $"Set {componentType}.{fieldName} = {value} on GameObject '{gameObjectName}'";
    }
}
