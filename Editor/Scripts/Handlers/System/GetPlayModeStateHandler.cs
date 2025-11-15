using UnityEditor;
using System;

using UnityEngine;
using UnityMCPSharp;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to get the current play mode state.
    /// </summary>
    public static class GetPlayModeStateHandler
    {
        public static void Handle(string requestId, MCPClient client)
        {
            try
            {
                string state;
                if (EditorApplication.isPlaying && EditorApplication.isPaused)
                {
                    state = "Paused";
                }
                else if (EditorApplication.isPlaying)
                {
                    state = "Playing";
                }
                else
                {
                    state = "Stopped";
                }

                var response = new { state };
                _ = client.SendResponseAsync(requestId, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetPlayModeStateHandler] Error: {ex.Message}");
            }
        }
    }
}
