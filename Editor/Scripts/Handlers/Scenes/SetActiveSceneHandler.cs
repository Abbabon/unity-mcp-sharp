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
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<SetActiveSceneData>(json);

                MCPOperationTracker.StartOperation("Set Active Scene", config.maxOperationLogEntries, config.verboseLogging, data);

                // Find scene by name or path
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (scene.name == data.sceneIdentifier || scene.path == data.sceneIdentifier)
                    {
                        SceneManager.SetActiveScene(scene);
                        Debug.Log($"[SetActiveSceneHandler] Set active scene: {data.sceneIdentifier}");
                        MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                        return;
                    }
                }

                Debug.LogWarning($"[SetActiveSceneHandler] Scene not found: {data.sceneIdentifier}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SetActiveSceneHandler] Error setting active scene: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
