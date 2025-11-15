using UnityEngine;
using System;

using UnityEngine.SceneManagement;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.Scenes
{
    /// <summary>
    /// Handles requests to save Unity scenes.
    /// </summary>
    public static class SaveSceneHandler
    {
        public static void Handle(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SaveSceneData>(json);

                if (data.saveAll)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                    Debug.Log("[SaveSceneHandler] Saved all open scenes");
                }
                else if (!string.IsNullOrEmpty(data.scenePath))
                {
                    // Find and save specific scene
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        var scene = SceneManager.GetSceneAt(i);
                        if (scene.path == data.scenePath)
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                            Debug.Log($"[SaveSceneHandler] Saved scene: {data.scenePath}");
                            return;
                        }
                    }
                    Debug.LogWarning($"[SaveSceneHandler] Scene not found: {data.scenePath}");
                }
                else
                {
                    // Save active scene
                    var activeScene = SceneManager.GetActiveScene();
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);
                    Debug.Log($"[SaveSceneHandler] Saved active scene: {activeScene.name}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveSceneHandler] Error saving scene: {ex.Message}");
            }
        }
    }
}
