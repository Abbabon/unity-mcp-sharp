using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityMCPSharp.Editor.Utilities
{
    /// <summary>
    /// Utility class for finding GameObjects in the scene hierarchy.
    /// Overcomes limitations of GameObject.Find which only finds root-level active objects.
    /// </summary>
    public static class GameObjectFinder
    {
        /// <summary>
        /// Find a GameObject by name, searching all root objects and their children (including inactive).
        /// </summary>
        /// <param name="name">Name of the GameObject to find</param>
        /// <returns>The found GameObject, or null if not found</returns>
        public static GameObject FindByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

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
