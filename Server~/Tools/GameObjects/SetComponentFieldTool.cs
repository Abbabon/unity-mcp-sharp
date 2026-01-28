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
    [Description("Set a field value on a component (primitives or asset references).")]
    [return: Description("Confirmation or error message")]
    public async Task<string> UnitySetComponentFieldAsync(
        [Description("Target GameObject name")] string gameObjectName,
        [Description("Component type name")] string componentType,
        [Description("Field or property name")] string fieldName,
        [Description("Value (primitive or asset path)")] string value,
        [Description("'string', 'int', 'float', 'bool', 'asset', 'gameObject'")] string valueType = "string")
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
