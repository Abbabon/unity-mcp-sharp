using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor.Handlers.Prefabs
{
    public static class ClosePrefabStageHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(parameters);
                var data = JsonConvert.DeserializeObject<ClosePrefabStageData>(json);

                // Check if we're in prefab mode
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null)
                {
                    Debug.LogWarning("[ClosePrefabStageHandler] No prefab stage is currently open");
                    return;
                }

                var prefabPath = prefabStage.assetPath;

                // Save before closing if requested
                if (data.saveBeforeClosing)
                {
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                    PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath);
                    Debug.Log($"[ClosePrefabStageHandler] Saved prefab '{prefabPath}' before closing");
                }

                // Close the prefab stage and return to main stage
                StageUtility.GoToMainStage();

                var saveInfo = data.saveBeforeClosing ? " (saved)" : " (unsaved changes discarded)";
                Debug.Log($"[ClosePrefabStageHandler] Closed prefab stage '{prefabPath}'{saveInfo}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ClosePrefabStageHandler] Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private class ClosePrefabStageData
        {
            public bool saveBeforeClosing;
        }
    }
}
