using UnityEditor;
using System;

using UnityEngine;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to create a GameObject in the active scene.
    /// </summary>
    public static class CreateGameObjectHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                // Parse parameters using Newtonsoft.Json for proper deserialization
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                Debug.Log($"[CreateGameObjectHandler] Received JSON: {json}");
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGameObjectData>(json);

                // Start operation tracking with parameters
                MCPOperationTracker.StartOperation("Create GameObject", config.maxOperationLogEntries, config.verboseLogging, data);
                Debug.Log($"[CreateGameObjectHandler] Parsed name: '{data.name}', components: {data.components?.Length ?? 0}");

                var go = new GameObject(data.name);

                if (data.position != null)
                {
                    go.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
                }

                // Add components if specified
                if (data.components != null)
                {
                    foreach (var componentName in data.components)
                    {
                        var componentType = Type.GetType($"UnityEngine.{componentName}, UnityEngine");
                        if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                        {
                            go.AddComponent(componentType);
                        }
                        else
                        {
                            Debug.LogWarning($"[CreateGameObjectHandler] Unknown component type: {componentName}");
                        }
                    }
                }

                // Set parent if specified
                if (!string.IsNullOrEmpty(data.parent))
                {
                    var parentObj = GameObject.Find(data.parent);
                    if (parentObj != null)
                    {
                        go.transform.SetParent(parentObj.transform);
                    }
                }

                Selection.activeGameObject = go;
                EditorGUIUtility.PingObject(go);

                Debug.Log($"[CreateGameObjectHandler] Created GameObject: {data.name}");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateGameObjectHandler] Error creating GameObject: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
