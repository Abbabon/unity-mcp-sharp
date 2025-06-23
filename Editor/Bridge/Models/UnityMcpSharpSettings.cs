using System;
using System.IO;
using Editor.Utils;
using UnityEngine;

namespace Editor.Bridge.Models
{
    /// <summary>
    /// Handles persistence of Unity MCP Sharp settings
    /// </summary>
    [Serializable]
    public class UnityMcpSharpSettings
    {
        // Constants
        public const string ServerVersion = "1.0.0";
        public const string PackageName = "com.abbabon.unity-mcp-sharp";
        public const int RequestTimeoutMinimum = 10;
        
        // Paths
        private const string SettingsPath = "ProjectSettings/UnityMcpSharpSettings.json";
        
        private static UnityMcpSharpSettings _instance;

        [Tooltip("Port number for MCP server")]
        public int Port = 8090;
        
        [Tooltip("Timeout in seconds for request")]
        public int RequestTimeoutSeconds = RequestTimeoutMinimum;
        
        [Tooltip("Whether to automatically start the MCP server when Unity opens")]
        public bool AutoStartServer = true;
        
        /// <summary>
        /// Singleton instance of settings
        /// </summary>
        public static UnityMcpSharpSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnityMcpSharpSettings();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Private constructor for singleton
        /// </summary>
        private UnityMcpSharpSettings() 
        { 
            LoadSettings();
        }

        /// <summary>
        /// Load settings from disk
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                // Load settings from file
                if (File.Exists(SettingsPath))
                {
                    var json = File.ReadAllText(SettingsPath);
                    JsonUtility.FromJsonOverwrite(json, this);
                }
                else
                {
                    // Create default settings file on the first time initialization
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to load settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Save settings to disk
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Save settings to McpUnitySettings.json
                var json = JsonUtility.ToJson(this, true);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                UnityMcpSharpLogger.LogError($"Failed to save settings: {ex.Message}");
            }
        }
    }
}
