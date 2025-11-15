using UnityEditor;
using System;

using UnityEditor.Compilation;
using UnityEngine;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to trigger script compilation in Unity.
    /// </summary>
    public static class TriggerCompilationHandler
    {
        public static void Handle(MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Trigger Compilation", config.maxOperationLogEntries, config.verboseLogging, null);

                Debug.Log("[TriggerCompilationHandler] Triggering script compilation...");
                CompilationPipeline.RequestScriptCompilation();

                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TriggerCompilationHandler] Error triggering compilation: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
