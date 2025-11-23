using UnityEditor;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor.Handlers.Prefabs
{
    public static class InstantiatePrefabHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(parameters);
                var data = JsonConvert.DeserializeObject<InstantiatePrefabData>(json);

                if (string.IsNullOrEmpty(data.prefabPath))
                {
                    Debug.LogError("[InstantiatePrefabHandler] Prefab path is required");
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
                    Debug.LogError($"[InstantiatePrefabHandler] Prefab not found at '{assetPath}'");
                    return;
                }

                // Instantiate the prefab
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
                if (instance == null)
                {
                    Debug.LogError($"[InstantiatePrefabHandler] Failed to instantiate prefab from '{assetPath}'");
                    return;
                }

                // Set transform properties
                instance.transform.position = new Vector3(data.position.x, data.position.y, data.position.z);
                instance.transform.rotation = Quaternion.Euler(data.rotation.x, data.rotation.y, data.rotation.z);
                instance.transform.localScale = new Vector3(data.scale.x, data.scale.y, data.scale.z);

                // Set custom name if provided
                if (!string.IsNullOrEmpty(data.instanceName))
                {
                    instance.name = data.instanceName;
                }

                // Set parent if specified
                if (!string.IsNullOrEmpty(data.parent))
                {
                    var parentObj = GameObject.Find(data.parent);
                    if (parentObj != null)
                    {
                        instance.transform.SetParent(parentObj.transform);
                    }
                    else
                    {
                        Debug.LogWarning($"[InstantiatePrefabHandler] Parent GameObject '{data.parent}' not found, instantiated at root level");
                    }
                }

                // Select the new instance
                Selection.activeGameObject = instance;

                // Mark scene as dirty
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());

                Debug.Log($"[InstantiatePrefabHandler] Instantiated prefab '{instance.name}' from '{assetPath}' at ({data.position.x}, {data.position.y}, {data.position.z})");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[InstantiatePrefabHandler] Error: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private class InstantiatePrefabData
        {
            public string prefabPath;
            public PositionData position;
            public RotationData rotation;
            public ScaleData scale;
            public string parent;
            public string instanceName;
        }

        private class PositionData
        {
            public float x;
            public float y;
            public float z;
        }

        private class RotationData
        {
            public float x;
            public float y;
            public float z;
        }

        private class ScaleData
        {
            public float x;
            public float y;
            public float z;
        }
    }
}
