using UnityEditor;
using System;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityMCPSharp;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to get Unity project information.
    /// </summary>
    public static class GetProjectInfoHandler
    {
        public static void Handle(string requestId, MCPClient client)
        {
            try
            {
                var response = new
                {
                    projectName = Application.productName,
                    unityVersion = Application.unityVersion,
                    platform = Application.platform.ToString(),
                    activeScene = SceneManager.GetActiveScene().name,
                    scenePath = SceneManager.GetActiveScene().path,
                    dataPath = Application.dataPath,
                    isPlaying = EditorApplication.isPlaying,
                    isPaused = EditorApplication.isPaused
                };

                _ = client.SendResponseAsync(requestId, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetProjectInfoHandler] Error: {ex.Message}");
            }
        }
    }
}
