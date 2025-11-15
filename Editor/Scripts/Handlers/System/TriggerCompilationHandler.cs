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
        public static void Handle(object parameters)
        {
            try
            {
                Debug.Log("[TriggerCompilationHandler] Triggering script compilation...");
                CompilationPipeline.RequestScriptCompilation();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TriggerCompilationHandler] Error triggering compilation: {ex.Message}");
            }
        }
    }
}
