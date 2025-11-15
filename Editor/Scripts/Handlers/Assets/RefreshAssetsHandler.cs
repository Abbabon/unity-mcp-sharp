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
        public static void Handle()
        {
            try
            {
                Debug.Log("[RefreshAssetsHandler] Refreshing Asset Database");
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RefreshAssetsHandler] Error refreshing assets: {ex.Message}");
            }
        }
    }
}
