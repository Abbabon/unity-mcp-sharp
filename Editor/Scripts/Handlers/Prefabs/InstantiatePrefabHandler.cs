using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
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

                // Set parent if specified (uses recursive search to find nested/inactive objects)
                if (!string.IsNullOrEmpty(data.parent))
                {
                    var parentObj = FindGameObjectByName(data.parent);
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

                // Mark the instance's scene as dirty (supports multi-scene editing)
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);

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

        /// <summary>
        /// Find a GameObject by name, searching all root objects and their children (including inactive).
        /// This overcomes the limitation of GameObject.Find which only finds root-level active objects.
        /// </summary>
        private static GameObject FindGameObjectByName(string name)
        {
            // First try the fast path - root-level active objects
            var result = GameObject.Find(name);
            if (result != null) return result;

            // Search through all scenes and their hierarchies
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootObj in scene.GetRootGameObjects())
                {
                    var found = FindInHierarchy(rootObj.transform, name);
                    if (found != null) return found;
                }
            }

            return null;
        }

        private static GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;

            foreach (Transform child in parent)
            {
                var found = FindInHierarchy(child, name);
                if (found != null) return found;
            }

            return null;
        }
    }
}
