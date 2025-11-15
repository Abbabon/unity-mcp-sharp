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
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<OpenSceneData>(json);

                MCPOperationTracker.StartOperation("Open Scene", config.maxOperationLogEntries, config.verboseLogging, data);

                var mode = data.additive ? UnityEditor.SceneManagement.OpenSceneMode.Additive : UnityEditor.SceneManagement.OpenSceneMode.Single;

                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(data.scenePath, mode);

                Debug.Log($"[OpenSceneHandler] Opened scene: {data.scenePath} (additive: {data.additive})");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[OpenSceneHandler] Error opening scene: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
