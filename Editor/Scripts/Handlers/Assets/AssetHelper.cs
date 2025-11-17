using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;

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
        /// Convert a value to a specific type with support for Unity types.
        /// </summary>
        public static object ConvertValue(object value, Type targetType)
        {
            if (value == null)
                return null;

            // If already correct type, return as-is
            if (targetType.IsInstanceOfType(value))
                return value;

            // Handle string inputs for Unity types
            if (value is string str)
            {
                // Vector3 from "x,y,z" format
                if (targetType == typeof(Vector3))
                {
                    var parts = str.Split(',');
                    if (parts.Length == 3 &&
                        float.TryParse(parts[0].Trim(), out float x) &&
                        float.TryParse(parts[1].Trim(), out float y) &&
                        float.TryParse(parts[2].Trim(), out float z))
                    {
                        return new Vector3(x, y, z);
                    }
                }

                // Vector2 from "x,y" format
                if (targetType == typeof(Vector2))
                {
                    var parts = str.Split(',');
                    if (parts.Length == 2 &&
                        float.TryParse(parts[0].Trim(), out float x) &&
                        float.TryParse(parts[1].Trim(), out float y))
                    {
                        return new Vector2(x, y);
                    }
                }

                // Color from hex "#RRGGBB" or "r,g,b,a" format
                if (targetType == typeof(Color))
                {
                    if (str.StartsWith("#"))
                    {
                        if (ColorUtility.TryParseHtmlString(str, out Color color))
                            return color;
                    }
                    else
                    {
                        var parts = str.Split(',');
                        if (parts.Length >= 3 &&
                            float.TryParse(parts[0].Trim(), out float r) &&
                            float.TryParse(parts[1].Trim(), out float g) &&
                            float.TryParse(parts[2].Trim(), out float b))
                        {
                            float a = 1f;
                            if (parts.Length == 4)
                                float.TryParse(parts[3].Trim(), out a);
                            return new Color(r, g, b, a);
                        }
                    }
                }

                // Quaternion from "x,y,z,w" format
                if (targetType == typeof(Quaternion))
                {
                    var parts = str.Split(',');
                    if (parts.Length == 4 &&
                        float.TryParse(parts[0].Trim(), out float x) &&
                        float.TryParse(parts[1].Trim(), out float y) &&
                        float.TryParse(parts[2].Trim(), out float z) &&
                        float.TryParse(parts[3].Trim(), out float w))
                    {
                        return new Quaternion(x, y, z, w);
                    }
                }

                // Bounds from "center(x,y,z);size(x,y,z)" format
                if (targetType == typeof(Bounds))
                {
                    var mainParts = str.Split(';');
                    if (mainParts.Length == 2)
                    {
                        var centerStr = mainParts[0].Replace("center(", "").Replace(")", "");
                        var sizeStr = mainParts[1].Replace("size(", "").Replace(")", "");

                        var centerParts = centerStr.Split(',');
                        var sizeParts = sizeStr.Split(',');

                        if (centerParts.Length == 3 && sizeParts.Length == 3 &&
                            float.TryParse(centerParts[0].Trim(), out float cx) &&
                            float.TryParse(centerParts[1].Trim(), out float cy) &&
                            float.TryParse(centerParts[2].Trim(), out float cz) &&
                            float.TryParse(sizeParts[0].Trim(), out float sx) &&
                            float.TryParse(sizeParts[1].Trim(), out float sy) &&
                            float.TryParse(sizeParts[2].Trim(), out float sz))
                        {
                            return new Bounds(new Vector3(cx, cy, cz), new Vector3(sx, sy, sz));
                        }
                    }
                }

                // Rect from "x,y,width,height" format
                if (targetType == typeof(Rect))
                {
                    var parts = str.Split(',');
                    if (parts.Length == 4 &&
                        float.TryParse(parts[0].Trim(), out float x) &&
                        float.TryParse(parts[1].Trim(), out float y) &&
                        float.TryParse(parts[2].Trim(), out float w) &&
                        float.TryParse(parts[3].Trim(), out float h))
                    {
                        return new Rect(x, y, w, h);
                    }
                }

                // Asset reference from path
                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    return AssetDatabase.LoadAssetAtPath(str, targetType);
                }
            }

            // Handle JObject for nested structures
            if (value is JObject jObj)
            {
                // Vector3
                if (targetType == typeof(Vector3) &&
                    jObj.TryGetValue("x", out var jx) &&
                    jObj.TryGetValue("y", out var jy) &&
                    jObj.TryGetValue("z", out var jz))
                {
                    return new Vector3(jx.ToObject<float>(), jy.ToObject<float>(), jz.ToObject<float>());
                }

                // Vector2
                if (targetType == typeof(Vector2) &&
                    jObj.TryGetValue("x", out var jx2) &&
                    jObj.TryGetValue("y", out var jy2))
                {
                    return new Vector2(jx2.ToObject<float>(), jy2.ToObject<float>());
                }

                // Color
                if (targetType == typeof(Color) &&
                    jObj.TryGetValue("r", out var jr) &&
                    jObj.TryGetValue("g", out var jg) &&
                    jObj.TryGetValue("b", out var jb))
                {
                    float a = jObj.TryGetValue("a", out var ja) ? ja.ToObject<float>() : 1f;
                    return new Color(jr.ToObject<float>(), jg.ToObject<float>(), jb.ToObject<float>(), a);
                }

                // Quaternion
                if (targetType == typeof(Quaternion) &&
                    jObj.TryGetValue("x", out var qx) &&
                    jObj.TryGetValue("y", out var qy) &&
                    jObj.TryGetValue("z", out var qz) &&
                    jObj.TryGetValue("w", out var qw))
                {
                    return new Quaternion(qx.ToObject<float>(), qy.ToObject<float>(), qz.ToObject<float>(), qw.ToObject<float>());
                }

                // For custom types, try to deserialize
                try
                {
                    return jObj.ToObject(targetType);
                }
                catch
                {
                    // Fall through to standard conversion
                }
            }

            // Standard conversion for primitives
            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch
            {
                Debug.LogWarning($"[AssetHelper] Could not convert value '{value}' to type {targetType.Name}");
                return null;
            }
        }

        /// <summary>
        /// Set properties on a Unity Object from JSON string using reflection (legacy method).
        /// For complex nested structures and arrays, use SetPropertiesFromJson_Advanced instead.
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
                        var convertedValue = ConvertValue(kvp.Value, property.PropertyType);
                        if (convertedValue != null)
                            property.SetValue(obj, convertedValue);
                    }
                    else
                    {
                        // Try field
                        var field = type.GetField(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                        if (field != null)
                        {
                            var convertedValue = ConvertValue(kvp.Value, field.FieldType);
                            if (convertedValue != null)
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
        /// Set properties on a Unity Object from JSON string using SerializedObject API.
        /// This method properly handles complex nested structures, arrays, and lists.
        /// </summary>
        public static void SetPropertiesFromJson_Advanced(UnityEngine.Object obj, string propertiesJson)
        {
            try
            {
                var jObject = JObject.Parse(propertiesJson);
                var serializedObject = new SerializedObject(obj);

                foreach (var kvp in jObject)
                {
                    var propertyPath = kvp.Key;
                    var value = kvp.Value;

                    var serializedProperty = serializedObject.FindProperty(propertyPath);
                    if (serializedProperty != null)
                    {
                        SetSerializedProperty(serializedProperty, value);
                    }
                    else
                    {
                        // Fall back to reflection for properties not found via SerializedProperty
                        TrySetViaReflection(obj, propertyPath, value);
                    }
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetHelper] Error in SetPropertiesFromJson_Advanced: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Set a SerializedProperty value from a JToken.
        /// </summary>
        private static void SetSerializedProperty(SerializedProperty prop, JToken value, int depth = 0)
        {
            const int MAX_DEPTH = 50;
            if (depth > MAX_DEPTH)
            {
                Debug.LogWarning($"[AssetHelper] Max recursion depth ({MAX_DEPTH}) reached for property {prop.name}");
                return;
            }

            if (value == null || value.Type == JTokenType.Null)
                return;

            switch (prop.propertyType)
            {
                case SerializedPropertyType.Integer:
                    prop.intValue = value.ToObject<int>();
                    break;

                case SerializedPropertyType.Boolean:
                    prop.boolValue = value.ToObject<bool>();
                    break;

                case SerializedPropertyType.Float:
                    prop.floatValue = value.ToObject<float>();
                    break;

                case SerializedPropertyType.String:
                    prop.stringValue = value.ToString();
                    break;

                case SerializedPropertyType.Color:
                    if (value is JObject colorObj)
                    {
                        prop.colorValue = new Color(
                            colorObj["r"]?.ToObject<float>() ?? 0,
                            colorObj["g"]?.ToObject<float>() ?? 0,
                            colorObj["b"]?.ToObject<float>() ?? 0,
                            colorObj["a"]?.ToObject<float>() ?? 1
                        );
                    }
                    break;

                case SerializedPropertyType.Vector2:
                    if (value is JObject vec2Obj)
                    {
                        prop.vector2Value = new Vector2(
                            vec2Obj["x"]?.ToObject<float>() ?? 0,
                            vec2Obj["y"]?.ToObject<float>() ?? 0
                        );
                    }
                    break;

                case SerializedPropertyType.Vector3:
                    if (value is JObject vec3Obj)
                    {
                        prop.vector3Value = new Vector3(
                            vec3Obj["x"]?.ToObject<float>() ?? 0,
                            vec3Obj["y"]?.ToObject<float>() ?? 0,
                            vec3Obj["z"]?.ToObject<float>() ?? 0
                        );
                    }
                    break;

                case SerializedPropertyType.Vector4:
                    if (value is JObject vec4Obj)
                    {
                        prop.vector4Value = new Vector4(
                            vec4Obj["x"]?.ToObject<float>() ?? 0,
                            vec4Obj["y"]?.ToObject<float>() ?? 0,
                            vec4Obj["z"]?.ToObject<float>() ?? 0,
                            vec4Obj["w"]?.ToObject<float>() ?? 0
                        );
                    }
                    break;

                case SerializedPropertyType.Quaternion:
                    if (value is JObject quatObj)
                    {
                        prop.quaternionValue = new Quaternion(
                            quatObj["x"]?.ToObject<float>() ?? 0,
                            quatObj["y"]?.ToObject<float>() ?? 0,
                            quatObj["z"]?.ToObject<float>() ?? 0,
                            quatObj["w"]?.ToObject<float>() ?? 1
                        );
                    }
                    break;

                case SerializedPropertyType.Rect:
                    if (value is JObject rectObj)
                    {
                        prop.rectValue = new Rect(
                            rectObj["x"]?.ToObject<float>() ?? 0,
                            rectObj["y"]?.ToObject<float>() ?? 0,
                            rectObj["width"]?.ToObject<float>() ?? 0,
                            rectObj["height"]?.ToObject<float>() ?? 0
                        );
                    }
                    break;

                case SerializedPropertyType.Bounds:
                    if (value is JObject boundsObj)
                    {
                        var center = boundsObj["center"] as JObject;
                        var size = boundsObj["size"] as JObject;
                        if (center != null && size != null)
                        {
                            prop.boundsValue = new Bounds(
                                new Vector3(
                                    center["x"]?.ToObject<float>() ?? 0,
                                    center["y"]?.ToObject<float>() ?? 0,
                                    center["z"]?.ToObject<float>() ?? 0
                                ),
                                new Vector3(
                                    size["x"]?.ToObject<float>() ?? 0,
                                    size["y"]?.ToObject<float>() ?? 0,
                                    size["z"]?.ToObject<float>() ?? 0
                                )
                            );
                        }
                    }
                    break;

                case SerializedPropertyType.ObjectReference:
                    if (value.Type == JTokenType.String)
                    {
                        var path = value.ToString();
                        prop.objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    }
                    break;

                case SerializedPropertyType.Enum:
                    if (value.Type == JTokenType.Integer)
                    {
                        prop.enumValueIndex = value.ToObject<int>();
                    }
                    else if (value.Type == JTokenType.String)
                    {
                        var enumNames = prop.enumNames;
                        var enumStr = value.ToString();
                        for (int i = 0; i < enumNames.Length; i++)
                        {
                            if (enumNames[i] == enumStr)
                            {
                                prop.enumValueIndex = i;
                                break;
                            }
                        }
                    }
                    break;

                case SerializedPropertyType.Generic:
                    if (prop.isArray)
                    {
                        if (value is JArray jArray)
                        {
                            SetArrayProperty(prop, jArray, depth + 1);
                        }
                    }
                    else
                    {
                        // Handle nested objects recursively
                        if (value is JObject nestedObj)
                        {
                            SetNestedObject(prop, nestedObj, depth + 1);
                        }
                    }
                    break;

                default:
                    Debug.LogWarning($"[AssetHelper] Unsupported SerializedPropertyType: {prop.propertyType} for property {prop.name}");
                    break;
            }
        }

        /// <summary>
        /// Set an array or list property from a JArray.
        /// </summary>
        private static void SetArrayProperty(SerializedProperty arrayProp, JArray jArray, int depth = 0)
        {
            arrayProp.arraySize = jArray.Count;

            for (int i = 0; i < jArray.Count; i++)
            {
                var elementProp = arrayProp.GetArrayElementAtIndex(i);
                SetSerializedProperty(elementProp, jArray[i], depth);
            }
        }

        /// <summary>
        /// Set nested object properties from a JObject.
        /// </summary>
        private static void SetNestedObject(SerializedProperty parentProp, JObject jObject, int depth = 0)
        {
            foreach (var kvp in jObject)
            {
                var childProp = parentProp.FindPropertyRelative(kvp.Key);
                if (childProp != null)
                {
                    SetSerializedProperty(childProp, kvp.Value, depth);
                }
            }
        }

        /// <summary>
        /// Try to set a property via reflection (fallback method).
        /// </summary>
        private static void TrySetViaReflection(object obj, string propertyPath, JToken value)
        {
            try
            {
                var type = obj.GetType();
                var property = type.GetProperty(propertyPath, BindingFlags.Public | BindingFlags.Instance);

                if (property != null && property.CanWrite)
                {
                    var convertedValue = ConvertValue(value, property.PropertyType);
                    if (convertedValue != null)
                        property.SetValue(obj, convertedValue);
                }
                else
                {
                    var field = type.GetField(propertyPath, BindingFlags.Public | BindingFlags.Instance);
                    if (field != null)
                    {
                        var convertedValue = ConvertValue(value, field.FieldType);
                        if (convertedValue != null)
                            field.SetValue(obj, convertedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AssetHelper] Could not set property {propertyPath} via reflection: {ex.Message}");
            }
        }

        /// <summary>
        /// Set properties using property path notation (e.g., "primitives.Array.data[0].color.r").
        /// </summary>
        public static void SetPropertiesFromPropertyPaths(UnityEngine.Object obj, Dictionary<string, object> propertyPaths)
        {
            try
            {
                var serializedObject = new SerializedObject(obj);

                foreach (var kvp in propertyPaths)
                {
                    var prop = serializedObject.FindProperty(kvp.Key);
                    if (prop != null)
                    {
                        var jValue = JToken.FromObject(kvp.Value);
                        SetSerializedProperty(prop, jValue);
                    }
                    else
                    {
                        Debug.LogWarning($"[AssetHelper] Property path not found: {kvp.Key}");
                    }
                }

                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(obj);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AssetHelper] Error in SetPropertiesFromPropertyPaths: {ex.Message}");
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
