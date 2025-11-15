using UnityEngine;
using System;

using UnityEngine.SceneManagement;
using UnityMCPSharp;

namespace UnityMCPSharp.Editor.Handlers.Scenes
{
    /// <summary>
    /// Handles requests to get information about the active scene.
    /// </summary>
    public static class GetActiveSceneHandler
    {
        public static void Handle(string requestId, MCPClient client, MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Get Active Scene", config.maxOperationLogEntries, config.verboseLogging, null);

                var scene = SceneManager.GetActiveScene();

                var response = new
                {
                    name = scene.name,
                    path = scene.path,
                    isDirty = scene.isDirty,
                    rootCount = scene.rootCount,
                    isLoaded = scene.isLoaded
                };

                _ = client.SendResponseAsync(requestId, response);

                Debug.Log($"[GetActiveSceneHandler] Active scene: {scene.name}");

                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetActiveSceneHandler] Error getting active scene: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
