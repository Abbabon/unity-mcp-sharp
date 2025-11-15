using UnityEngine;
using System;

using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.Scenes
{
    /// <summary>
    /// Handles requests to open a Unity scene.
    /// </summary>
    public static class OpenSceneHandler
    {
        public static void Handle(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenSceneData>(json);

                var mode = data.additive ? UnityEditor.SceneManagement.OpenSceneMode.Additive : UnityEditor.SceneManagement.OpenSceneMode.Single;

                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(data.scenePath, mode);

                Debug.Log($"[OpenSceneHandler] Opened scene: {data.scenePath} (additive: {data.additive})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenSceneHandler] Error opening scene: {ex.Message}");
            }
        }
    }
}
