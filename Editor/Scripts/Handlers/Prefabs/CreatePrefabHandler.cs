using System;
using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;
using UnityMCPSharp.Editor.Utilities;
using Object = UnityEngine.Object;

namespace UnityMCPSharp.Editor.Handlers.Prefabs
{
    public static class CreatePrefabHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(parameters);
                var data = JsonConvert.DeserializeObject<CreatePrefabData>(json);

                if (string.IsNullOrEmpty(data.gameObjectName))
                {
                    Debug.LogError("[CreatePrefabHandler] GameObject name is required");
                    return;
                }

                if (string.IsNullOrEmpty(data.assetFolderPath))
                {
                    Debug.LogError("[CreatePrefabHandler] Asset folder path is required");
                    return;
                }

                // Find the GameObject in the scene (searches root and all children, including inactive)
                var gameObject = GameObjectFinder.FindByName(data.gameObjectName);
                if (gameObject == null)
                {
                    Debug.LogError($"[CreatePrefabHandler] GameObject '{data.gameObjectName}' not found in scene");
                    return;
                }

                // Construct full asset path
                var assetPath = $"Assets/{data.assetFolderPath}/{data.prefabName}.prefab";

                // Ensure the folder exists
                var folderPath = $"Assets/{data.assetFolderPath}";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    // Create folder structure recursively (RemoveEmptyEntries handles edge cases like "Prefabs//Characters")
                    var folders = data.assetFolderPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    var currentPath = "Assets";
                    foreach (var folder in folders)
                    {
                        var newPath = $"{currentPath}/{folder}";
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folder);
                        }
                        currentPath = newPath;
                    }
                }

                // Create the prefab
                bool success;
                if (data.createVariant)
                {
                    // Check if the source is a prefab instance
                    if (!PrefabUtility.IsPartOfPrefabInstance(gameObject))
                    {
                        Debug.LogError($"[CreatePrefabHandler] GameObject '{data.gameObjectName}' must be a prefab instance to create a variant");
                        return;
                    }

                    // Get the source prefab that this instance is based on
                    var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                    var sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                    
                    if (sourcePrefab == null)
                    {
                        Debug.LogError($"[CreatePrefabHandler] Could not find source prefab for '{data.gameObjectName}'");
                        return;
                    }

                    // VARIANT CREATION APPROACH:
                    // We can't simply call SaveAsPrefabAsset on the scene instance because that creates
                    // a disconnected copy. Instead, we:
                    // 1. InstantiatePrefab from the source - this creates an instance with proper prefab link
                    // 2. Copy transforms to preserve scene modifications
                    // 3. SaveAsPrefabAsset - because the instance has a prefab link, Unity creates a variant
                    // 4. Destroy the temp instance
                    // This ensures the new prefab maintains the variant relationship with its base prefab.
                    var variantInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
                    
                    // Copy transforms from the scene object to preserve any modifications
                    variantInstance.transform.position = gameObject.transform.position;
                    variantInstance.transform.rotation = gameObject.transform.rotation;
                    variantInstance.transform.localScale = gameObject.transform.localScale;
                    variantInstance.name = data.prefabName;

                    // Save as prefab - this creates a variant because the instance is linked to a base prefab
                    var prefabAsset = PrefabUtility.SaveAsPrefabAsset(variantInstance, assetPath, out success);
                    
                    // Clean up the temporary instance
                    Object.DestroyImmediate(variantInstance);
                    
                    if (success)
                    {
                        Debug.Log($"[CreatePrefabHandler] Created prefab variant '{assetPath}' from '{data.gameObjectName}'");
                    }
                }
                else
                {
                    // Create regular prefab and connect the scene instance to it
                    var prefabAsset = PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, assetPath, InteractionMode.AutomatedAction, out success);
                    if (success)
                    {
                        Debug.Log($"[CreatePrefabHandler] Created prefab '{assetPath}' from '{data.gameObjectName}'");
                    }
                }

                if (!success)
                {
                    Debug.LogError($"[CreatePrefabHandler] Failed to create prefab at '{assetPath}'");
                    return;
                }

                // Mark scene as dirty after creating prefab from scene object
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);

                // Refresh asset database
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreatePrefabHandler] Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private class CreatePrefabData
        {
            public string gameObjectName;
            public string assetFolderPath;
            public string prefabName;
            public bool createVariant;
        }
    }
}
