using UnityEditor;
using System;

using UnityEngine;
using UnityMCPSharp;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to get the current compilation status.
    /// </summary>
    public static class GetCompilationStatusHandler
    {
        public static void Handle(string requestId, MCPClient client, bool isCompiling)
        {
            try
            {
                var response = new
                {
                    isCompiling = isCompiling,
                    lastCompilationSucceeded = !EditorUtility.scriptCompilationFailed
                };

                _ = client.SendResponseAsync(requestId, response);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GetCompilationStatusHandler] Error: {ex.Message}");
            }
        }
    }
}
