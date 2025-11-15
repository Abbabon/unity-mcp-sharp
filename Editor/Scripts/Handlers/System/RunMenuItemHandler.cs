using System;
using UnityEditor;
using UnityEngine;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.System
{
    /// <summary>
    /// Handles requests to execute Unity Editor menu items.
    /// </summary>
    public static class RunMenuItemHandler
    {
        [Serializable]
        public class RunMenuItemData
        {
            [Newtonsoft.Json.JsonProperty("menuPath")]
            public string menuPath;
        }

        public static void Handle(object parameters)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<RunMenuItemData>(json);

                if (string.IsNullOrEmpty(data.menuPath))
                {
                    Debug.LogError("[RunMenuItemHandler] Menu path is required");
                    return;
                }

                Debug.Log($"[RunMenuItemHandler] Executing menu item: {data.menuPath}");

                // Execute the menu item
                EditorApplication.ExecuteMenuItem(data.menuPath);

                Debug.Log($"[RunMenuItemHandler] Successfully executed menu item: {data.menuPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RunMenuItemHandler] Error executing menu item: {ex.Message}");
            }
        }
    }
}
