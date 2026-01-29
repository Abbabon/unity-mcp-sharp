using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using System.Collections.Generic;

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
                    // Use GetRootGameObjects + recursive traversal instead of expensive FindObjectsOfType
                    var processedRoots = new HashSet<GameObject>();
                    var activeScene = SceneManager.GetActiveScene();
                    var rootObjects = activeScene.GetRootGameObjects();
                    
                    foreach (var rootObj in rootObjects)
                    {
                        FindAndApplyPrefabOverrides(rootObj, assetPath, processedRoots);
                    }

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

        /// <summary>
        /// Recursively find prefab instances and apply overrides to matching prefab assets.
        /// More efficient than FindObjectsOfType for large scenes.
        /// </summary>
        private static void FindAndApplyPrefabOverrides(GameObject obj, string targetAssetPath, HashSet<GameObject> processedRoots)
        {
            if (obj == null) return;

            // Check if this object is part of a prefab instance
            if (PrefabUtility.IsPartOfPrefabInstance(obj))
            {
                var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
                if (prefabRoot != null && !processedRoots.Contains(prefabRoot))
                {
                    processedRoots.Add(prefabRoot);
                    
                    var correspondingAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                    if (correspondingAsset != null)
                    {
                        var instanceAssetPath = AssetDatabase.GetAssetPath(correspondingAsset);
                        if (instanceAssetPath == targetAssetPath)
                        {
                            // Apply overrides to this prefab
                            PrefabUtility.ApplyPrefabInstance(prefabRoot, InteractionMode.AutomatedAction);
                            Debug.Log($"[SavePrefabHandler] Applied overrides from instance '{prefabRoot.name}' to prefab '{targetAssetPath}'");
                        }
                    }
                }
            }

            // Recursively check children
            foreach (Transform child in obj.transform)
            {
                FindAndApplyPrefabOverrides(child.gameObject, targetAssetPath, processedRoots);
            }
        }
    }
}
