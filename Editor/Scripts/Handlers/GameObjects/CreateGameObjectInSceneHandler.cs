using UnityEngine;
using System;

using UnityEngine.SceneManagement;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to create a GameObject in a specific scene.
    /// </summary>
    public static class CreateGameObjectInSceneHandler
    {
        public static void Handle(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateGameObjectInSceneData>(json);

                // Find or load the target scene
                Scene targetScene = default;
                bool sceneFound = false;

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.path == data.scenePath)
                    {
                        targetScene = scene;
                        sceneFound = true;
                        break;
                    }
                }

                // If scene not loaded, open it additively
                if (!sceneFound)
                {
                    targetScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(data.scenePath, UnityEditor.SceneManagement.OpenSceneMode.Additive);
                    Debug.Log($"[CreateGameObjectInSceneHandler] Loaded scene additively: {data.scenePath}");
                }

                // Temporarily set as active scene to create GameObject in it
                var previousActiveScene = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(targetScene);

                // Create GameObject
                var go = new GameObject(data.name);

                if (data.position != null)
                {
                    go.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
                }

                if (data.components != null)
                {
                    foreach (var componentName in data.components)
                    {
                        var componentType = Type.GetType($"UnityEngine.{componentName}, UnityEngine");
                        if (componentType != null && typeof(Component).IsAssignableFrom(componentType))
                        {
                            go.AddComponent(componentType);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(data.parent))
                {
                    var parentObj = GameObject.Find(data.parent);
                    if (parentObj != null)
                    {
                        go.transform.SetParent(parentObj.transform);
                    }
                }

                // Restore previous active scene
                SceneManager.SetActiveScene(previousActiveScene);

                Debug.Log($"[CreateGameObjectInSceneHandler] Created GameObject '{data.name}' in scene '{data.scenePath}'");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateGameObjectInSceneHandler] Error creating GameObject in scene: {ex.Message}");
            }
        }
    }
}
