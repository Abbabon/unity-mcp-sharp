using UnityEditor;
using UnityEngine;

namespace UnityMCPSharp.Editor
{
    /// <summary>
    /// Unity Editor menu items for MCP Server control
    /// </summary>
    public static class MCPMenuItems
    {
        private const string MenuRoot = "Tools/Unity MCP Server/";

        [MenuItem(MenuRoot + "Dashboard", priority = 0)]
        public static void OpenDashboard()
        {
            MCPDashboard.ShowWindow();
        }

        [MenuItem(MenuRoot + "Quick Start Guide", priority = 100)]
        public static void OpenQuickStartGuide()
        {
            Application.OpenURL("https://github.com/Abbabon/unity-mcp-sharp#quick-start");
        }

        [MenuItem(MenuRoot + "Documentation", priority = 101)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/Abbabon/unity-mcp-sharp/blob/main/Documentation~/Installation.md");
        }

        [MenuItem(MenuRoot + "Report Issue", priority = 102)]
        public static void ReportIssue()
        {
            Application.OpenURL("https://github.com/Abbabon/unity-mcp-sharp/issues");
        }

        [MenuItem(MenuRoot + "About", priority = 200)]
        public static void About()
        {
            EditorUtility.DisplayDialog(
                "Unity MCP Server",
                "Unity MCP Server v0.1.0\n\n" +
                "Model Context Protocol (MCP) integration for Unity Editor.\n\n" +
                "Enables AI assistants to interact with Unity through console logs, " +
                "compilation, and scene manipulation.\n\n" +
                "© 2025 AmitN\n" +
                "Licensed under MIT License",
                "OK"
            );
        }

        [MenuItem(MenuRoot + "Create MCP Configuration", priority = 300)]
        public static void CreateConfiguration()
        {
            var config = ScriptableObject.CreateInstance<MCPConfiguration>();

            var path = "Assets/Resources";
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            var assetPath = $"{path}/MCPConfiguration.asset";

            // Check if already exists
            if (System.IO.File.Exists(assetPath))
            {
                if (!EditorUtility.DisplayDialog(
                    "Configuration Exists",
                    "MCPConfiguration already exists. Do you want to replace it?",
                    "Replace",
                    "Cancel"))
                {
                    return;
                }
            }

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;

            Debug.Log($"[MCPMenuItems] Created MCPConfiguration at {assetPath}");
        }
    }
}
