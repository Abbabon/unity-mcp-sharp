using UnityEngine;
using System;

using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to add a component to an existing GameObject.
    /// </summary>
    public static class AddComponentHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<AddComponentData>(json);

                // Start operation tracking
                MCPOperationTracker.StartOperation("Add Component", config.maxOperationLogEntries, config.verboseLogging, data);

                var go = GameObject.Find(data.gameObjectName);
                if (go == null)
                {
                    Debug.LogError($"[AddComponentHandler] GameObject not found: {data.gameObjectName}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                // Try to find the component type
                // First try UnityEngine namespace
                var componentType = Type.GetType($"UnityEngine.{data.componentType}, UnityEngine");

                // If not found, try without namespace (for custom scripts)
                if (componentType == null)
                {
                    componentType = Type.GetType(data.componentType);
                }

                // If still not found, search all assemblies
                if (componentType == null)
                {
                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        componentType = assembly.GetType(data.componentType);
                        if (componentType != null)
                            break;
                    }
                }

                if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                {
                    go.AddComponent(componentType);
                    Debug.Log($"[AddComponentHandler] Added component {data.componentType} to {data.gameObjectName}");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                }
                else
                {
                    Debug.LogError($"[AddComponentHandler] Component type not found: {data.componentType}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AddComponentHandler] Error adding component: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
