using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityMCPSharp.Editor.Handlers.Assets
{
    /// <summary>
    /// Helper class for asset-related operations.
    /// Provides utilities for type finding, property setting, and asset extension resolution.
    /// </summary>
    public static class AssetHelper
    {
        /// <summary>
        /// Find a type by name across all loaded assemblies.
        /// </summary>
        public static Type FindType(string typeName)
        {
            // Try to find the type in all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }

            // Try with UnityEngine namespace if not fully qualified
            if (!typeName.Contains("."))
            {
                return Type.GetType($"UnityEngine.{typeName}, UnityEngine") ??
                       Type.GetType($"UnityEditor.{typeName}, UnityEditor");
            }

            return null;
        }

        /// <summary>
        /// Set properties on a Unity Object from JSON string.
        /// </summary>
        public static void SetPropertiesFromJson(UnityEngine.Object obj, string propertiesJson)
        {
            try
            {
                var properties = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(propertiesJson);
                var type = obj.GetType();

                foreach (var kvp in properties)
                {
                    // Handle special cases for Material
                    if (obj is Material material)
                    {
                        if (kvp.Key.ToLower() == "shader" && kvp.Value is string shaderName)
                        {
                            material.shader = Shader.Find(shaderName);
                            continue;
                        }
                        if (kvp.Key.ToLower() == "color" && kvp.Value is string colorHex)
                        {
                            if (ColorUtility.TryParseHtmlString(colorHex, out Color color))
                            {
                                material.color = color;
                            }
                            continue;
                        }
                    }

                    // Handle special cases for Texture2D
                    if (obj is Texture2D texture)
                    {
                        if (kvp.Key.ToLower() == "width" && kvp.Value != null)
                        {
                            // Can't resize after creation, would need to handle in constructor
                            continue;
                        }
                        if (kvp.Key.ToLower() == "height" && kvp.Value != null)
                        {
                            continue;
                        }
                    }

                    // Try to set property via reflection
                    var property = type.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && property.CanWrite)
                    {
                        var convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                        property.SetValue(obj, convertedValue);
                    }
                    else
                    {
                        // Try field
                        var field = type.GetField(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                        if (field != null)
                        {
                            var convertedValue = Convert.ChangeType(kvp.Value, field.FieldType);
                            field.SetValue(obj, convertedValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetHelper] Error setting properties from JSON: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the appropriate file extension for an asset type.
        /// </summary>
        public static string GetAssetExtension(Type assetType)
        {
            if (assetType == typeof(Material)) return "mat";
            if (assetType == typeof(Texture2D)) return "asset";
            if (assetType == typeof(AnimationClip)) return "anim";
            if (typeof(ScriptableObject).IsAssignableFrom(assetType)) return "asset";
            return "asset"; // Default extension
        }
    }
}
