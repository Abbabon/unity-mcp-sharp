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
        public static void Handle(object parameters, MCPConfiguration config)
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
                    Debug.LogError($"[SetComponentFieldHandler] GameObject not found: {data.gameObjectName}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
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
                    Debug.LogError($"[SetComponentFieldHandler] Component type not found: {data.componentType}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                var component = go.GetComponent(componentType);
                if (component == null)
                {
                    Debug.LogError($"[SetComponentFieldHandler] Component {data.componentType} not found on {data.gameObjectName}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                // Try to set the field or property
                var field = componentType.GetField(data.fieldName, BindingFlags.Public | BindingFlags.Instance);
                var property = componentType.GetProperty(data.fieldName, BindingFlags.Public | BindingFlags.Instance);

                if (field != null)
                {
                    object convertedValue = ConvertValue(data.value, data.valueType, field.FieldType);
                    field.SetValue(component, convertedValue);
                    Debug.Log($"[SetComponentFieldHandler] Set field {data.componentType}.{data.fieldName} = {data.value}");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                }
                else if (property != null && property.CanWrite)
                {
                    object convertedValue = ConvertValue(data.value, data.valueType, property.PropertyType);
                    property.SetValue(component, convertedValue);
                    Debug.Log($"[SetComponentFieldHandler] Set property {data.componentType}.{data.fieldName} = {data.value}");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                }
                else
                {
                    Debug.LogError($"[SetComponentFieldHandler] Field or property {data.fieldName} not found or not writable on {data.componentType}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SetComponentFieldHandler] Error setting component field: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
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
