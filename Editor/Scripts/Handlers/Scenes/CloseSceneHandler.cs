using UnityEngine;
using System;

using UnityEngine.SceneManagement;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.Scenes
{
    /// <summary>
    /// Handles requests to close a Unity scene.
    /// </summary>
    public static class CloseSceneHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CloseSceneData>(json);

                MCPOperationTracker.StartOperation("Close Scene", config.maxOperationLogEntries, config.verboseLogging, data);

                // Find scene by name or path
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == data.sceneIdentifier || scene.path == data.sceneIdentifier)
                    {
                        UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                        Debug.Log($"[CloseSceneHandler] Closed scene: {data.sceneIdentifier}");
                        MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                        return;
                    }
                }

                Debug.LogWarning($"[CloseSceneHandler] Scene not found: {data.sceneIdentifier}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CloseSceneHandler] Error closing scene: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
