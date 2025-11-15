using System.Collections.Generic;
using System;

using UnityEditor;
using UnityEngine;
using UnityMCPSharp;

namespace UnityMCPSharp.Editor.Handlers.Scenes
{
    /// <summary>
    /// Handles requests to list all scene files in the project.
    /// </summary>
    public static class ListScenesHandler
    {
        public static void Handle(string requestId, MCPClient client)
        {
            try
            {
                var sceneGuids = AssetDatabase.FindAssets("t:Scene");
                var scenes = new List<string>();

                foreach (var guid in sceneGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    scenes.Add(path);
                }

                var response = new { scenes };
                _ = client.SendResponseAsync(requestId, response);

                Debug.Log($"[ListScenesHandler] Found {scenes.Count} scenes");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ListScenesHandler] Error listing scenes: {ex.Message}");
            }
        }
    }
}
