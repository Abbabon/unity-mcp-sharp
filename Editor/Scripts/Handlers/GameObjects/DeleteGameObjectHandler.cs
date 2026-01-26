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

        public static void Handle(string requestId, object parameters, MCPClient client, MCPConfiguration config)
        {
            try
            {
                MCPOperationTracker.StartOperation("Delete GameObject", config.maxOperationLogEntries, config.verboseLogging, null);

                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<DeleteGameObjectParams>(json);

                if (string.IsNullOrEmpty(data?.name))
                {
                    var errorMsg = "GameObject name is required";
                    Debug.LogError($"[DeleteGameObjectHandler] {errorMsg}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
                    return;
                }

                var gameObject = GameObject.Find(data.name);
                if (gameObject == null)
                {
                    var errorMsg = $"GameObject '{data.name}' not found";
                    Debug.LogError($"[DeleteGameObjectHandler] {errorMsg}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
                    return;
                }

                var objectName = gameObject.name;
                Object.DestroyImmediate(gameObject);

                var successMsg = $"GameObject '{objectName}' deleted from scene";
                MCPLogger.Log($"[DeleteGameObjectHandler] {successMsg}");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
                _ = client.SendResponseAsync(requestId, new { success = true, message = successMsg });
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error deleting GameObject: {ex.Message}";
                Debug.LogError($"[DeleteGameObjectHandler] {errorMsg}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                _ = client.SendResponseAsync(requestId, new { success = false, message = errorMsg });
            }
        }
    }
}
