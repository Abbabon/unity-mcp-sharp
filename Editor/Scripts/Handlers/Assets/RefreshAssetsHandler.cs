using UnityEditor;
using System;

using UnityEngine;

namespace UnityMCPSharp.Editor.Handlers.Assets
{
    /// <summary>
    /// Handles requests to refresh the Unity Asset Database.
    /// </summary>
    public static class RefreshAssetsHandler
    {
        public static void Handle(MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Refresh Assets", config.maxOperationLogEntries, config.verboseLogging, null);

                Debug.Log("[RefreshAssetsHandler] Refreshing Asset Database");
                AssetDatabase.Refresh();

                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RefreshAssetsHandler] Error refreshing assets: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
