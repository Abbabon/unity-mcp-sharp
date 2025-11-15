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
        public static void Handle(MCPConfiguration config)
        {
            try
            {
                // Start operation tracking
                MCPOperationTracker.StartOperation("Enter Play Mode", config.maxOperationLogEntries, config.verboseLogging, null);

                if (!EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = true;
                    Debug.Log("[EnterPlayModeHandler] Entering play mode");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                }
                else
                {
                    Debug.LogWarning("[EnterPlayModeHandler] Already in play mode");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EnterPlayModeHandler] Error entering play mode: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
