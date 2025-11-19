using UnityEditor;
using System;

using UnityEngine;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.Assets
{
    /// <summary>
    /// Handles requests to create various types of Unity assets.
    /// </summary>
    public static class CreateAssetHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            MCPOperationTracker.StartOperation("Create Asset", config.maxOperationLogEntries, config.verboseLogging);
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateAssetData>(json);

                // Create folder if it doesn't exist
                string folderPath = $"Assets/{data.folderPath}";
                if (!AssetDatabase.IsValidFolder(folderPath))
                {
                    string[] folders = data.folderPath.Split('/');
                    string currentPath = "Assets";
                    foreach (var folder in folders)
                    {
                        string newPath = $"{currentPath}/{folder}";
                        if (!AssetDatabase.IsValidFolder(newPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, folder);
                        }
                        currentPath = newPath;
                    }
                }

                // Use reflection to find and create the asset type
                Type assetType = AssetHelper.FindType(data.assetTypeName);
                if (assetType == null)
                {
                    Debug.LogError($"[CreateAssetHandler] Could not find type: {data.assetTypeName}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                // Create the asset instance
                UnityEngine.Object assetInstance = null;

                // Special handling for ScriptableObject
                if (typeof(ScriptableObject).IsAssignableFrom(assetType))
                {
                    assetInstance = ScriptableObject.CreateInstance(assetType);
                }
                // Special handling for Material
                else if (assetType == typeof(Material))
                {
                    assetInstance = new Material(Shader.Find("Standard"));
                }
                // Special handling for Texture2D
                else if (assetType == typeof(Texture2D))
                {
                    assetInstance = new Texture2D(256, 256);
                }
                // Try constructor with no parameters
                else if (typeof(UnityEngine.Object).IsAssignableFrom(assetType))
                {
                    var constructor = assetType.GetConstructor(Type.EmptyTypes);
                    if (constructor != null)
                    {
                        assetInstance = constructor.Invoke(null) as UnityEngine.Object;
                    }
                }

                if (assetInstance == null)
                {
                    Debug.LogError($"[CreateAssetHandler] Could not create instance of type: {data.assetTypeName}");
                    MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
                    return;
                }

                // Set properties from JSON if provided
                if (!string.IsNullOrEmpty(data.propertiesJson))
                {
                    AssetHelper.SetPropertiesFromJson_Advanced(assetInstance, data.propertiesJson);
                }

                // Determine file extension
                string extension = AssetHelper.GetAssetExtension(assetType);

                // Save asset
                string assetPath = $"{folderPath}/{data.assetName}.{extension}";
                AssetDatabase.CreateAsset(assetInstance, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log($"[CreateAssetHandler] Created {assetType.Name} asset: {assetPath}");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateAssetHandler] Error creating asset: {ex.Message}\n{ex.StackTrace}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
