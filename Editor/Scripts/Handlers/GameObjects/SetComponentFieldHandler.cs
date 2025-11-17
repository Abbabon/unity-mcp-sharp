using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to set a field or property value on a component.
    /// </summary>
    public static class SetComponentFieldHandler
    {
        public static void Handle(string requestId, object parameters, MCPClient client, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SetComponentFieldData>(json);

                // Start operation tracking
                MCPOperationTracker.StartOperation("Set Component Field", config.maxOperationLogEntries, config.verboseLogging, data);

                var go = GameObject.Find(data.gameObjectName);
                if (go == null)
                {
                    var errorMsg = $"GameObject not found: {data.gameObjectName}";
                    Debug.LogError($"[SetComponentFieldHandler] {errorMsg}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
                    return;
                }

                // Find the component type
                var componentType = Type.GetType($"UnityEngine.{data.componentType}, UnityEngine");
                if (componentType == null)
                {
                    componentType = Type.GetType(data.componentType);
                }
                if (componentType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        componentType = assembly.GetType(data.componentType);
                        if (componentType != null)
                            break;
                    }
                }

                if (componentType == null)
                {
                    var errorMsg = $"Component type not found: {data.componentType}";
                    Debug.LogError($"[SetComponentFieldHandler] {errorMsg}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
                    return;
                }

                var component = go.GetComponent(componentType);
                if (component == null)
                {
                    var errorMsg = $"Component {data.componentType} not found on {data.gameObjectName}";
                    Debug.LogError($"[SetComponentFieldHandler] {errorMsg}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
                    return;
                }

                // Try to set the field or property
                var field = componentType.GetField(data.fieldName, BindingFlags.Public | BindingFlags.Instance);
                var property = componentType.GetProperty(data.fieldName, BindingFlags.Public | BindingFlags.Instance);

                if (field != null)
                {
                    object convertedValue = ConvertValue(data.value, data.valueType, field.FieldType);
                    field.SetValue(component, convertedValue);
                    var successMsg = $"Set field {data.componentType}.{data.fieldName} = {data.value}";
                    Debug.Log($"[SetComponentFieldHandler] {successMsg}");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = true, message = successMsg });
                }
                else if (property != null && property.CanWrite)
                {
                    object convertedValue = ConvertValue(data.value, data.valueType, property.PropertyType);
                    property.SetValue(component, convertedValue);
                    var successMsg = $"Set property {data.componentType}.{data.fieldName} = {data.value}";
                    Debug.Log($"[SetComponentFieldHandler] {successMsg}");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = true, message = successMsg });
                }
                else
                {
                    var errorMsg = $"Field or property {data.fieldName} not found or not writable on {data.componentType}";
                    Debug.LogError($"[SetComponentFieldHandler] {errorMsg}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error setting component field: {ex.Message}";
                Debug.LogError($"[SetComponentFieldHandler] {errorMsg}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
            }
        }

        private static object ConvertValue(string value, string valueType, Type targetType)
        {
            object converted = valueType.ToLower() switch
            {
                "int" => int.Parse(value),
                "float" => float.Parse(value),
                "bool" => bool.Parse(value),
                "asset" => AssetDatabase.LoadAssetAtPath(value, targetType),
                "gameobject" => GameObject.Find(value),
                _ => value // string by default
            };

            // Validate type compatibility
            if (converted != null && !targetType.IsInstanceOfType(converted))
            {
                throw new InvalidCastException($"[SetComponentFieldHandler] Cannot assign {converted.GetType().Name} to {targetType.Name}. Value: '{value}', ValueType: '{valueType}'");
            }

            return converted;
        }
    }
}
