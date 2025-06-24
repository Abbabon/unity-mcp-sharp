using Editor.Bridge.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor.Bridge.UI
{
    public class UnityMcpSharpWindow : EditorWindow
    {
        private Label _statusLabel;
        private const string _offlineText = "Status: Offline";
        private const string _onlineText = "Status: Online";
        
        [MenuItem("Window/Unity MCP Sharp")]
        public static void ShowWindow()
        {
            UnityMcpSharpWindow wnd = GetWindow<UnityMcpSharpWindow>();
            wnd.titleContent = new GUIContent("Unity MCP Sharp");
            wnd.minSize = new Vector2(300, 200);
        }

        public void CreateGUI()
        {
            // Set up the scrolling container
            ScrollView scrollView = new ScrollView(ScrollViewMode.Vertical);
            rootVisualElement.Add(scrollView);
            
            // Add the title
            Label titleLabel = new Label("Unity MCP Sharp")
            {
                style =
                {
                    fontSize = 20,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 10,
                    marginTop = 5,
                    alignSelf = Align.Center
                }
            };
            scrollView.Add(titleLabel);

            // Create a container for the status section
            VisualElement statusSection = new VisualElement
            {
                style =
                {
                    marginTop = 15,
                    marginBottom = 15,
                    borderBottomWidth = 1,
                    borderBottomColor = new Color(0.2f, 0.2f, 0.2f),
                    paddingBottom = 15
                }
            };
            scrollView.Add(statusSection);

            // Add a section title
            Label sectionLabel = new Label("Server Status")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = 5
                }
            };
            statusSection.Add(sectionLabel);

            // Add the status label
            _statusLabel = new Label(_offlineText);
            statusSection.Add(_statusLabel);

            // Add status indicator icon
            VisualElement statusIndicator = new VisualElement
            {
                style =
                {
                    width = 12,
                    height = 12,
                    borderTopLeftRadius = 6,
                    borderTopRightRadius = 6,
                    borderBottomLeftRadius = 6,
                    borderBottomRightRadius = 6,
                    backgroundColor = Color.red,
                    marginLeft = 5,
                    marginRight = 5
                }
            };

            // Wrap the status in a horizontal container for better layout
            VisualElement statusContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginTop = 5
                }
            };
            statusContainer.Add(statusIndicator);
            statusContainer.Add(_statusLabel);
            statusSection.Add(statusContainer);

            // Add control buttons
            VisualElement buttonContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center,
                    marginTop = 10
                }
            };

            Button connectButton = new Button(ConnectToServer)
            {
                text = "Connect",
                style =
                {
                    marginRight = 5
                }
            };
            buttonContainer.Add(connectButton);

            Button disconnectButton = new Button(DisconnectFromServer)
            {
                text = "Disconnect",
                style =
                {
                    marginLeft = 5
                }
            };
            buttonContainer.Add(disconnectButton);
            statusSection.Add(buttonContainer);

            // Register for EditorApplication.update to refresh the status
            EditorApplication.update += UpdateStatus;
        }

        private void ConnectToServer()
        {
            UnityBridgeServer.Instance.StartServer();
            // UpdateStatusDisplay();
        }

        private void DisconnectFromServer()
        {
            // TODO: implement disconnect
            UnityBridgeServer.Instance.StopServer();
            // UpdateStatusDisplay();
        }

        private void UpdateStatus()
        {
            UpdateStatusDisplay();
        }

        private void UpdateStatusDisplay()
        {
            if (_statusLabel == null) return;

            // TODO: implement
            bool isConnected = false;
            _statusLabel.text = isConnected ? _onlineText : _offlineText;

            // Update status indicator color
            VisualElement statusContainer = _statusLabel.parent;
            if (statusContainer != null && statusContainer.childCount > 0)
            {
                VisualElement statusIndicator = statusContainer[0];
                statusIndicator.style.backgroundColor = isConnected ? Color.green : Color.red;
            }
        }

        private void OnDestroy()
        {
            EditorApplication.update -= UpdateStatus;
            // TODO: implement disconnect
            //UnityBridgeServer.Instance.Disconnect();
        }
    }
}
