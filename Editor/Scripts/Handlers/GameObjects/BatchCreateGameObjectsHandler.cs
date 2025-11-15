using UnityEngine;
using System;

using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to batch create multiple GameObjects.
    /// </summary>
    public static class BatchCreateGameObjectsHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<BatchCreateData>(json);

                MCPOperationTracker.StartOperation("Batch Create GameObjects", config.maxOperationLogEntries, config.verboseLogging, data);

                var gameObjects = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGameObjectData[]>(data.gameObjectsJson);

                foreach (var goData in gameObjects)
                {
                    var go = new GameObject(goData.name);

                    if (goData.position != null)
                    {
                        go.transform.position = new Vector3(goData.position.x, goData.position.y, goData.position.z);
                    }

                    if (goData.components != null)
                    {
                        foreach (var componentName in goData.components)
                        {
                            var componentType = Type.GetType($"UnityEngine.{componentName}, UnityEngine");
                            if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                            {
                                go.AddComponent(componentType);
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(goData.parent))
                    {
                        var parentObj = GameObject.Find(goData.parent);
                        if (parentObj != null)
                        {
                            go.transform.SetParent(parentObj.transform);
                        }
                    }
                }

                Debug.Log($"[BatchCreateGameObjectsHandler] Batch created {gameObjects.Length} GameObjects");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BatchCreateGameObjectsHandler] Error batch creating GameObjects: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
