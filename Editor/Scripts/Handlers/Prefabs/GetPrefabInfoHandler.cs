using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor.Handlers.Prefabs
{
    public static class GetPrefabInfoHandler
    {
        public static void Handle(string requestId, object parameters, MCPClient client, MCPConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(parameters);
                var data = JsonConvert.DeserializeObject<GetPrefabInfoData>(json);

                if (string.IsNullOrEmpty(data.gameObjectNameOrPath))
                {
                    Debug.LogError("[GetPrefabInfoHandler] GameObject name or path is required");
                    _ = client.SendResponseAsync(requestId, new { error = "GameObject name or path is required" });
                    return;
                }

                GameObject gameObject = null;
                bool isAssetPath = data.gameObjectNameOrPath.StartsWith("Assets/");

                if (isAssetPath)
                {
                    // Load as asset
                    gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(data.gameObjectNameOrPath);
                }
                else
                {
                    // Find in scene
                    gameObject = GameObject.Find(data.gameObjectNameOrPath);
                }

                if (gameObject == null)
                {
                    Debug.LogWarning($"[GetPrefabInfoHandler] GameObject '{data.gameObjectNameOrPath}' not found");
                    _ = client.SendResponseAsync(requestId, new
                    {
                        found = false,
                        gameObjectName = data.gameObjectNameOrPath
                    });
                    return;
                }

                // Get prefab info
                var isPrefabAsset = PrefabUtility.IsPartOfPrefabAsset(gameObject);
                var isPrefabInstance = PrefabUtility.IsPartOfPrefabInstance(gameObject);
                var isPrefabVariant = PrefabUtility.IsPartOfVariantPrefab(gameObject);
                var prefabAssetType = PrefabUtility.GetPrefabAssetType(gameObject);
                var prefabInstanceStatus = PrefabUtility.GetPrefabInstanceStatus(gameObject);

                string assetPath = null;
                bool isModified = false;

                if (isPrefabInstance)
                {
                    // Get the prefab asset path for instances
                    var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject);
                    if (prefabRoot != null)
                    {
                        var correspondingAsset = PrefabUtility.GetCorrespondingObjectFromSource(prefabRoot);
                        if (correspondingAsset != null)
                        {
                            assetPath = AssetDatabase.GetAssetPath(correspondingAsset);
                        }
                        
                        // Check if the instance has modifications - must be called on prefabRoot for accurate results
                        isModified = PrefabUtility.HasPrefabInstanceAnyOverrides(prefabRoot, false);
                    }
                }
                else if (isPrefabAsset)
                {
                    assetPath = AssetDatabase.GetAssetPath(gameObject);
                }

                var result = new
                {
                    found = true,
                    gameObjectName = gameObject.name,
                    isPrefabAsset,
                    isPrefabInstance,
                    isPrefabVariant,
                    assetPath,
                    prefabAssetType = prefabAssetType.ToString(),
                    prefabInstanceStatus = prefabInstanceStatus.ToString(),
                    isModified,
                    isPartOfAnyPrefab = PrefabUtility.IsPartOfAnyPrefab(gameObject)
                };

                _ = client.SendResponseAsync(requestId, result);

                if (config.verboseLogging)
                {
                    Debug.Log($"[GetPrefabInfoHandler] Retrieved prefab info for '{gameObject.name}': Asset={isPrefabAsset}, Instance={isPrefabInstance}, Variant={isPrefabVariant}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GetPrefabInfoHandler] Error: {ex.Message}\n{ex.StackTrace}");
                _ = client.SendResponseAsync(requestId, new { error = ex.Message });
            }
        }

        private class GetPrefabInfoData
        {
            public string gameObjectNameOrPath;
        }
    }
}
