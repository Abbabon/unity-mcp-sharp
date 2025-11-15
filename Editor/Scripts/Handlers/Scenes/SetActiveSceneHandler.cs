using UnityEngine;
using System;

using UnityEngine.SceneManagement;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.Scenes
{
    /// <summary>
    /// Handles requests to set the active scene.
    /// </summary>
    public static class SetActiveSceneHandler
    {
        public static void Handle(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SetActiveSceneData>(json);

                // Find scene by name or path
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == data.sceneIdentifier || scene.path == data.sceneIdentifier)
                    {
                        SceneManager.SetActiveScene(scene);
                        Debug.Log($"[SetActiveSceneHandler] Set active scene: {data.sceneIdentifier}");
                        return;
                    }
                }

                Debug.LogWarning($"[SetActiveSceneHandler] Scene not found: {data.sceneIdentifier}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SetActiveSceneHandler] Error setting active scene: {ex.Message}");
            }
        }
    }
}
