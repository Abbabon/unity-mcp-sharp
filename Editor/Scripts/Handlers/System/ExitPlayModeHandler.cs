using UnityEditor;
using System;

using UnityEngine;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to exit Unity play mode.
    /// </summary>
    public static class ExitPlayModeHandler
    {
        public static void Handle()
        {
            try
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    Debug.Log("[ExitPlayModeHandler] Exiting play mode");
                }
                else
                {
                    Debug.LogWarning("[ExitPlayModeHandler] Already stopped");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExitPlayModeHandler] Error exiting play mode: {ex.Message}");
            }
        }
    }
}
