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
        public static void Handle(MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Exit Play Mode", config.maxOperationLogEntries, config.verboseLogging, null);

                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    Debug.Log("[ExitPlayModeHandler] Exiting play mode");
                    MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                }
                else
                {
                    Debug.LogWarning("[ExitPlayModeHandler] Already stopped");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ExitPlayModeHandler] Error exiting play mode: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
