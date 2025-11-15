using System.Collections.Generic;
using System;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMCPSharp;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to list all GameObjects in the active scene.
    /// </summary>
    public static class ListSceneObjectsHandler
    {
        public static void Handle(string requestId, object parameters, MCPClient client)
        {
            try
            {
                var scene = SceneManager.GetActiveScene();
                var rootObjects = scene.GetRootGameObjects();

                var sceneObjects = new List<SceneObject>();
                foreach (var root in rootObjects)
                {
                    BuildHierarchy(root.transform, sceneObjects, 0);
                }

                var response = new { objects = sceneObjects };
                _ = client.SendResponseAsync(requestId, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ListSceneObjectsHandler] Error: {ex.Message}");
            }
        }

        private static void BuildHierarchy(Transform transform, List<SceneObject> sceneObjects, int depth)
        {
            sceneObjects.Add(new SceneObject
            {
                name = transform.name,
                isActive = transform.gameObject.activeInHierarchy,
                depth = depth
            });

            foreach (Transform child in transform)
            {
                BuildHierarchy(child, sceneObjects, depth + 1);
            }
        }
    }
}
