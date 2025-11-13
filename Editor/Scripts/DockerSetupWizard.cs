using UnityEditor;
using UnityEngine;

namespace UnityMCPSharp.Editor
{
    /// <summary>
    /// Setup wizard to guide users through Docker installation
    /// </summary>
    public class DockerSetupWizard : EditorWindow
    {
        private bool _dockerInstalled;
        private bool _checking = true;
        private string _statusMessage = "Checking Docker installation...";

        [MenuItem("Tools/Unity MCP Server/Setup Wizard", priority = 1)]
        public static void ShowWizard()
        {
            var window = GetWindow<DockerSetupWizard>();
            window.titleContent = new GUIContent("MCP Setup Wizard");
            window.minSize = new Vector2(500, 300);
            window.maxSize = new Vector2(500, 300);
        }

        private async void OnEnable()
        {
            await CheckDockerAsync();
        }

        private async System.Threading.Tasks.Task CheckDockerAsync()
        {
            _checking = true;
            _statusMessage = "Checking Docker installation...";
            Repaint();

            var config = MCPConfiguration.Instance;
            var manager = MCPServerManager.GetInstance(config);

            _dockerInstalled = await manager.IsDockerInstalledAsync();
            _checking = false;

            if (_dockerInstalled)
            {
                _statusMessage = "Docker is installed! ✓";
            }
            else
            {
                _statusMessage = "Docker is not installed";
            }

            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);

            // Header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("Unity MCP Server - Setup Wizard", headerStyle);

            GUILayout.Space(20);

            if (_checking)
            {
                GUILayout.Label(_statusMessage, EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Please wait while we check your system...", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            else if (_dockerInstalled)
            {
                // Docker is installed
                GUILayout.Label(_statusMessage, EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);

                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Docker is installed and ready!\n\n" +
                    "You can now use the Unity MCP Server. Click 'Open Dashboard' to get started.",
                    MessageType.Info);
                EditorGUI.indentLevel--;

                GUILayout.Space(20);

                if (GUILayout.Button("Open Dashboard", GUILayout.Height(30)))
                {
                    MCPDashboard.ShowWindow();
                    Close();
                }

                GUILayout.Space(10);

                if (GUILayout.Button("Re-check Docker", GUILayout.Height(25)))
                {
                    _ = CheckDockerAsync();
                }
            }
            else
            {
                // Docker is not installed
                GUILayout.Label(_statusMessage, EditorStyles.centeredGreyMiniLabel);
                GUILayout.Space(10);

                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "Docker Desktop is required to run the Unity MCP Server.\n\n" +
                    "Please download and install Docker Desktop for your platform:",
                    MessageType.Warning);
                EditorGUI.indentLevel--;

                GUILayout.Space(10);

                // Download buttons for different platforms
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Download for macOS", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    Application.OpenURL("https://www.docker.com/products/docker-desktop/");
                }

                if (GUILayout.Button("Download for Windows", GUILayout.Width(150), GUILayout.Height(30)))
                {
                    Application.OpenURL("https://www.docker.com/products/docker-desktop/");
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.Space(15);

                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "After installing Docker Desktop:\n" +
                    "1. Start Docker Desktop\n" +
                    "2. Wait for Docker to finish starting (check system tray)\n" +
                    "3. Click 'Re-check Docker' below",
                    MessageType.Info);
                EditorGUI.indentLevel--;

                GUILayout.Space(20);

                if (GUILayout.Button("Re-check Docker", GUILayout.Height(30)))
                {
                    _ = CheckDockerAsync();
                }
            }

            GUILayout.FlexibleSpace();

            // Footer
            GUILayout.Space(10);
            var footerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 10
            };
            GUILayout.Label("For more information, visit the documentation", footerStyle);

            if (GUILayout.Button("View Documentation", GUILayout.Height(20)))
            {
                Application.OpenURL("https://github.com/Abbabon/unity-mcp-sharp/blob/main/Documentation~/Installation.md");
            }

            GUILayout.Space(10);
        }
    }
}
