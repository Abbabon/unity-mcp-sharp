using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor.Handlers.Prefabs
{
    public static class OpenPrefabHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(parameters);
                var data = JsonConvert.DeserializeObject<OpenPrefabData>(json);

                if (string.IsNullOrEmpty(data.prefabPath))
                {
                    Debug.LogError("[OpenPrefabHandler] Prefab path is required");
                    return;
                }

                // Construct full asset path if not already prefixed
                var assetPath = data.prefabPath.StartsWith("Assets/")
                    ? data.prefabPath
                    : $"Assets/{data.prefabPath}";

                // Add .prefab extension if missing
                if (!assetPath.EndsWith(".prefab"))
                {
                    assetPath += ".prefab";
                }

                // Load the prefab asset
                var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefabAsset == null)
                {
                    Debug.LogError($"[OpenPrefabHandler] Prefab not found at '{assetPath}'");
                    return;
                }

                // Check if a prefab stage is already open
                var currentStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (currentStage != null)
                {
                    Debug.LogWarning($"[OpenPrefabHandler] Closing currently open prefab '{currentStage.assetPath}' before opening '{assetPath}'");
                    StageUtility.GoToMainStage();
                }

                // Open the prefab in prefab mode
                var stageMode = data.inContext
                    ? PrefabStage.Mode.InContext
                    : PrefabStage.Mode.InIsolation;

                var prefabStage = PrefabStageUtility.OpenPrefab(assetPath, stageMode);
                if (prefabStage == null)
                {
                    Debug.LogError($"[OpenPrefabHandler] Failed to open prefab '{assetPath}' in Prefab Mode");
                    return;
                }

                var modeInfo = data.inContext ? "Context" : "Isolation";
                Debug.Log($"[OpenPrefabHandler] Opened prefab '{assetPath}' in {modeInfo} mode");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[OpenPrefabHandler] Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private class OpenPrefabData
        {
            public string prefabPath;
            public bool inContext;
        }
    }
}
