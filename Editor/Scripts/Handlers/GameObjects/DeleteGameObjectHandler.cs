using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityMCPSharp.Editor.Handlers.GameObjects
{
    /// <summary>
    /// Handles requests to delete a GameObject from the scene.
    /// </summary>
    public static class DeleteGameObjectHandler
    {
        [Serializable]
        private class DeleteGameObjectParams
        {
            public string name;
        }

        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Delete GameObject", config.maxOperationLogEntries, config.verboseLogging, null);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteGameObjectParams>(json);

                if (string.IsNullOrEmpty(data?.name))
                {
                    Debug.LogError("[DeleteGameObjectHandler] GameObject name is required");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                var gameObject = GameObject.Find(data.name);
                if (gameObject == null)
                {
                    Debug.LogError($"[DeleteGameObjectHandler] GameObject '{data.name}' not found");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                var objectName = gameObject.name;
                Object.DestroyImmediate(gameObject);

                MCPLogger.Log($"[DeleteGameObjectHandler] Deleted GameObject: {objectName}");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DeleteGameObjectHandler] Error: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
