using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor.Handlers.Prefabs
{
    public static class SavePrefabHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(parameters);
                var data = JsonConvert.DeserializeObject<SavePrefabData>(json);

                // Check if we're in prefab mode
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                if (!string.IsNullOrEmpty(data.prefabPath))
                {
                    // Save specific prefab by path
                    var assetPath = data.prefabPath.StartsWith("Assets/")
                        ? data.prefabPath
                        : $"Assets/{data.prefabPath}";

                    if (!assetPath.EndsWith(".prefab"))
                    {
                        assetPath += ".prefab";
                    }

                    var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (prefabAsset == null)
                    {
                        Debug.LogError($"[SavePrefabHandler] Prefab not found at '{assetPath}'");
                        return;
                    }

                    // Find instances in the scene and apply overrides
                    var allObjects = GameObject.FindObjectsOfType<GameObject>();
                    foreach (var obj in allObjects)
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(obj))
                        {
                            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                            if (prefabRoot != null)
                            {
                                var correspondingAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                                if (correspondingAsset != null)
                                {
                                    var instanceAssetPath = AssetDatabase.GetAssetPath(correspondingAsset);
                                    if (instanceAssetPath == assetPath)
                                    {
                                        // Apply overrides to this prefab
                                        PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
                                        Debug.Log($"[SavePrefabHandler] Applied overrides from instance '{prefabRoot.name}' to prefab '{assetPath}'");
                                    }
                                }
                            }
                        }
                    }

                    AssetDatabase.SaveAssets();
                    Debug.Log($"[SavePrefabHandler] Saved prefab '{assetPath}'");
                }
                else if (prefabStage != null)
                {
                    // Save the currently open prefab stage
                    EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                    PrefabUtility.SaveAsPrefabAsset(prefabStage.prefabContentsRoot, prefabStage.assetPath);
                    Debug.Log($"[SavePrefabHandler] Saved prefab stage '{prefabStage.assetPath}'");
                }
                else
                {
                    Debug.LogWarning("[SavePrefabHandler] No prefab stage is open and no prefab path was specified");
                    return;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SavePrefabHandler] Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private class SavePrefabData
        {
            public string prefabPath;
        }
    }
}
