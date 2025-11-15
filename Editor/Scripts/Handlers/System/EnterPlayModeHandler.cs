using UnityEditor;
using System;

using UnityEngine;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to enter Unity play mode.
    /// </summary>
    public static class EnterPlayModeHandler
    {
        public static void Handle()
        {
            try
            {
                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = true;
                    Debug.Log("[EnterPlayModeHandler] Entering play mode");
                }
                else
                {
                    Debug.LogWarning("[EnterPlayModeHandler] Already in play mode");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnterPlayModeHandler] Error entering play mode: {ex.Message}");
            }
        }
    }
}
