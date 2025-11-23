using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

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

                // Find the GameObject in the scene
                var gameObject = GameObject.Find(data.gameObjectName);
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
                    // Create folder structure recursively
                    var folders = data.assetFolderPath.Split('/');
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
                    if (!PrefabUtility.IsPartOfAnyPrefab(gameObject))
                    {
                        Debug.LogError($"[CreatePrefabHandler] GameObject '{data.gameObjectName}' must be a prefab instance to create a variant");
                        return;
                    }

                    var prefabAsset = PrefabUtility.SaveAsPrefabAsset(gameObject, assetPath, out success);
                    if (success)
                    {
                        Debug.Log($"[CreatePrefabHandler] Created prefab variant '{assetPath}' from '{data.gameObjectName}'");
                    }
                }
                else
                {
                    // Create regular prefab
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

                // Refresh asset database
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            catch (System.Exception ex)
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
