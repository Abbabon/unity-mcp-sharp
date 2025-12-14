using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityMCPSharp;
using System.Threading.Tasks;

namespace UnityMCPSharp.Editor
{
    /// <summary>
    /// UIToolkit-based EditorWindow for Unity MCP Server dashboard
    /// </summary>
    public class MCPDashboard : EditorWindow
    {
        private MCPServerManager _serverManager;
        private MCPClient _client;
        private MCPConfiguration _config;

        // UI Elements
        private Label _statusLabel;
        private Label _connectionLabel;
        private Label _currentOperationLabel;
        private Button _startButton;
        private Button _stopButton;
        private Button _connectButton;
        private Button _disconnectButton;
        private Button _refreshButton;
        private TextField _serverUrlField;
        private TextField _dockerImageField;
        private ScrollView _logsScrollView;
        private ScrollView _operationsScrollView;
        private Toggle _autoConnectToggle;
        private Toggle _autoStartToggle;
        private Toggle _enableMcpLogsToggle;
        private Toggle _verboseLoggingToggle;
        private Toggle _showParametersToggle;

        // Tab buttons
        private Button _statusTab;
        private Button _configTab;
        private Button _logsTab;
        private Button _llmConfigTab;
        private string _activeTabName = "status-content";

        // Auto-refresh for logs
        private double _lastLogRefreshTime;
        private const double LOG_REFRESH_INTERVAL = 2.0; // seconds

        // Auto-refresh for connection status
        private double _lastConnectionCheckTime;
        private const double CONNECTION_CHECK_INTERVAL = 1.0; // seconds
        private bool _lastKnownConnectionState;

        private bool _isInitialized;
        private bool _showParameters = false; // Track whether to show parameters in operations log

        [MenuItem("Tools/Unity MCP Server/Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<MCPDashboard>();
            window.titleContent = new GUIContent("MCP Dashboard");
            window.minSize = new Vector2(600, 400);
        }

        public void CreateGUI()
        {
            // Load configuration
            _config = MCPConfiguration.Instance;

            // Get singleton instance (shared with MCPEditorIntegration)
            // This ensures both Dashboard and EditorIntegration use the same client
            _serverManager = MCPServerManager.GetInstance(_config);
            _client = _serverManager.GetClient();

            // Important: Don't dispose the client in OnDestroy if it's being used by MCPEditorIntegration
            // The client will persist across Unity sessions for auto-reconnect

            // Subscribe to events
            _serverManager.OnStatusChanged += OnServerStatusChanged;
            _client.OnConnected += OnClientConnected;
            _client.OnDisconnected += OnClientDisconnected;
            _client.OnError += OnClientError;

            // Create UI
            CreateUI();

            // Initial status check
            _ = RefreshStatusAsync();

            // Subscribe to update for auto-refresh
            EditorApplication.update += OnEditorUpdate;

            _isInitialized = true;
        }

        private void OnDestroy()
        {
            // Unsubscribe from update
            EditorApplication.update -= OnEditorUpdate;

            if (_serverManager != null)
            {
                _serverManager.OnStatusChanged -= OnServerStatusChanged;
            }

            if (_client != null)
            {
                _client.OnConnected -= OnClientConnected;
                _client.OnDisconnected -= OnClientDisconnected;
                _client.OnError -= OnClientError;
                // Don't dispose the client - it's managed by MCPEditorIntegration
                // and needs to persist for auto-reconnect functionality
            }
        }

        private void OnEditorUpdate()
        {
            if (!_isInitialized)
                return;

            var currentTime = EditorApplication.timeSinceStartup;

            // Auto-refresh connection status
            if (currentTime - _lastConnectionCheckTime >= CONNECTION_CHECK_INTERVAL)
            {
                UpdateConnectionUI();
                UpdateOperationUI();
                _lastConnectionCheckTime = currentTime;
            }

            // Auto-refresh logs when logs tab is active
            if (_activeTabName == "logs-content")
            {
                if (currentTime - _lastLogRefreshTime >= LOG_REFRESH_INTERVAL)
                {
                    _ = RefreshLogsAsync();
                    _lastLogRefreshTime = currentTime;
                }
            }
        }

        private void UpdateOperationUI()
        {
            if (!_config.showVisualFeedback)
                return;

            // Update current operation label
            if (_currentOperationLabel != null)
            {
                if (MCPOperationTracker.IsOperationInProgress)
                {
                    _currentOperationLabel.text = $"⚡ {MCPOperationTracker.CurrentOperation}";
                    _currentOperationLabel.style.color = new Color(0.3f, 0.7f, 1f);
                    _currentOperationLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _currentOperationLabel.style.display = DisplayStyle.None;
                }
            }

            // Update operations log (show all operations, not just recent)
            if (_operationsScrollView != null && _config.showOperationLog)
            {
                _operationsScrollView.Clear();

                foreach (var op in MCPOperationTracker.AllOperations)
                {
                    var opLabel = new Label();
                    var timeStr = op.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    var statusIcon = op.Status == "completed" ? "✓" : op.Status == "failed" ? "✗" : "⋯";
                    var statusColor = op.Status == "completed" ? new Color(0.3f, 0.8f, 0.3f) :
                                    op.Status == "failed" ? new Color(0.9f, 0.3f, 0.3f) :
                                    new Color(0.7f, 0.7f, 0.7f);

                    // Build operation text
                    var opText = $"{statusIcon} [{timeStr}] {op.Operation}";

                    // Add parameters if toggle is on and parameters exist
                    if (_showParameters && !string.IsNullOrEmpty(op.Parameters))
                    {
                        opText += $"\n    Parameters: {op.Parameters}";
                    }

                    opLabel.text = opText;
                    opLabel.style.color = statusColor;
                    opLabel.style.fontSize = 11;
                    opLabel.style.paddingLeft = 4;
                    opLabel.style.paddingTop = 2;
                    opLabel.style.paddingBottom = 2;
                    opLabel.style.whiteSpace = WhiteSpace.Normal; // Allow text wrapping for parameters

                    _operationsScrollView.Add(opLabel);
                }
            }

            // Update background color tint
            if (MCPOperationTracker.IsOperationInProgress && _config.showVisualFeedback)
            {
                rootVisualElement.style.backgroundColor = _config.feedbackColor;
            }
            else
            {
                rootVisualElement.style.backgroundColor = new StyleColor(StyleKeyword.None);
            }
        }

        private void UpdateConnectionUI()
        {
            if (_client == null || _connectionLabel == null)
                return;

            var isConnected = _client.IsConnected;

            // Only update UI if connection state changed
            if (isConnected != _lastKnownConnectionState)
            {
                _lastKnownConnectionState = isConnected;

                if (isConnected)
                {
                    _connectionLabel.text = "Connected ✓";
                    _connectionLabel.style.color = Color.green;
                }
                else
                {
                    _connectionLabel.text = "Disconnected";
                    _connectionLabel.style.color = Color.red;
                }
            }
        }

        private void CreateUI()
        {
            var root = rootVisualElement;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;

            // Header
            var header = new Label("Unity MCP Server Dashboard");
            header.style.fontSize = 20;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            root.Add(header);

            // Create tabs
            var tabView = new VisualElement();
            tabView.style.flexDirection = FlexDirection.Row;
            tabView.style.marginBottom = 10;

            _statusTab = CreateTab("Status", true);
            _configTab = CreateTab("Configuration", false);
            _logsTab = CreateTab("Logs", false);
            _llmConfigTab = CreateTab("LLM Config", false);

            tabView.Add(_statusTab);
            tabView.Add(_configTab);
            tabView.Add(_logsTab);
            tabView.Add(_llmConfigTab);
            root.Add(tabView);

            // Content container
            var contentContainer = new VisualElement();
            contentContainer.style.flexGrow = 1;

            // Status Tab Content
            var statusContent = CreateStatusTab();
            statusContent.name = "status-content";
            contentContainer.Add(statusContent);

            // Configuration Tab Content
            var configContent = CreateConfigurationTab();
            configContent.name = "config-content";
            configContent.style.display = DisplayStyle.None;
            contentContainer.Add(configContent);

            // Logs Tab Content
            var logsContent = CreateLogsTab();
            logsContent.name = "logs-content";
            logsContent.style.display = DisplayStyle.None;
            contentContainer.Add(logsContent);

            // LLM Config Tab Content
            var llmConfigContent = CreateLLMConfigTab();
            llmConfigContent.name = "llmconfig-content";
            llmConfigContent.style.display = DisplayStyle.None;
            contentContainer.Add(llmConfigContent);

            root.Add(contentContainer);

            // Setup tab switching
            _statusTab.clicked += () => SwitchTab(contentContainer, "status-content", _statusTab);
            _configTab.clicked += () => SwitchTab(contentContainer, "config-content", _configTab);
            _logsTab.clicked += () => SwitchTab(contentContainer, "logs-content", _logsTab);
            _llmConfigTab.clicked += () => SwitchTab(contentContainer, "llmconfig-content", _llmConfigTab);
        }

        private Button CreateTab(string label, bool active)
        {
            var button = new Button { text = label };
            button.style.flexGrow = 1;
            button.style.height = 30;
            button.style.backgroundColor = active ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.2f, 0.2f, 0.2f);
            return button;
        }

        private void SwitchTab(VisualElement container, string targetName, Button activeButton)
        {
            // Track active tab
            _activeTabName = targetName;

            // Update content visibility
            foreach (var child in container.Children())
            {
                child.style.display = child.name == targetName ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Update tab button styles
            var activeColor = new Color(0.3f, 0.3f, 0.3f);
            var inactiveColor = new Color(0.2f, 0.2f, 0.2f);

            _statusTab.style.backgroundColor = (_statusTab == activeButton) ? activeColor : inactiveColor;
            _configTab.style.backgroundColor = (_configTab == activeButton) ? activeColor : inactiveColor;
            _logsTab.style.backgroundColor = (_logsTab == activeButton) ? activeColor : inactiveColor;
            _llmConfigTab.style.backgroundColor = (_llmConfigTab == activeButton) ? activeColor : inactiveColor;

            // Immediately refresh logs when switching to logs tab
            if (targetName == "logs-content")
            {
                _ = RefreshLogsAsync();
                _lastLogRefreshTime = EditorApplication.timeSinceStartup;
            }
        }

        private VisualElement CreateStatusTab()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            // Server Status Section
            var statusSection = new VisualElement();
            statusSection.style.marginBottom = 15;

            var statusHeader = new Label("Server Status");
            statusHeader.style.fontSize = 16;
            statusHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusHeader.style.marginBottom = 5;
            statusSection.Add(statusHeader);

            _statusLabel = new Label("Checking...");
            _statusLabel.style.marginBottom = 10;
            statusSection.Add(_statusLabel);

            // Server Control Buttons
            var serverButtons = new VisualElement();
            serverButtons.style.flexDirection = FlexDirection.Row;
            serverButtons.style.marginBottom = 10;

            _startButton = new Button(() => _ = StartServerAsync()) { text = "Start Server" };
            _startButton.style.flexGrow = 1;
            _startButton.style.marginRight = 5;
            serverButtons.Add(_startButton);

            _stopButton = new Button(() => _ = StopServerAsync()) { text = "Stop Server" };
            _stopButton.style.flexGrow = 1;
            _stopButton.style.marginRight = 5;
            serverButtons.Add(_stopButton);

            _refreshButton = new Button(() => _ = RefreshStatusAsync()) { text = "Refresh" };
            _refreshButton.style.flexGrow = 1;
            serverButtons.Add(_refreshButton);

            statusSection.Add(serverButtons);
            container.Add(statusSection);

            // Connection Status Section
            var connectionSection = new VisualElement();
            connectionSection.style.marginBottom = 15;

            var connectionHeader = new Label("Connection Status");
            connectionHeader.style.fontSize = 16;
            connectionHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            connectionHeader.style.marginBottom = 5;
            connectionSection.Add(connectionHeader);

            _connectionLabel = new Label("Disconnected");
            _connectionLabel.style.marginBottom = 10;
            connectionSection.Add(_connectionLabel);

            // Connection Control Buttons
            var connectionButtons = new VisualElement();
            connectionButtons.style.flexDirection = FlexDirection.Row;
            connectionButtons.style.marginBottom = 10;

            _connectButton = new Button(() => _ = ConnectAsync()) { text = "Connect" };
            _connectButton.style.flexGrow = 1;
            _connectButton.style.marginRight = 5;
            connectionButtons.Add(_connectButton);

            _disconnectButton = new Button(() => _ = DisconnectAsync()) { text = "Disconnect" };
            _disconnectButton.style.flexGrow = 1;
            connectionButtons.Add(_disconnectButton);

            connectionSection.Add(connectionButtons);
            container.Add(connectionSection);

            // MCP Operations Section
            if (_config.showVisualFeedback)
            {
                var operationsSection = new VisualElement();
                operationsSection.style.marginBottom = 15;

                var operationsHeader = new Label("MCP Operations");
                operationsHeader.style.fontSize = 16;
                operationsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
                operationsHeader.style.marginBottom = 5;
                operationsSection.Add(operationsHeader);

                // Current operation indicator
                _currentOperationLabel = new Label();
                _currentOperationLabel.style.fontSize = 13;
                _currentOperationLabel.style.marginBottom = 10;
                _currentOperationLabel.style.display = DisplayStyle.None;
                operationsSection.Add(_currentOperationLabel);

                if (_config.showOperationLog)
                {
                    // Header row with label and clear button
                    var headerRow = new VisualElement();
                    headerRow.style.flexDirection = FlexDirection.Row;
                    headerRow.style.justifyContent = Justify.SpaceBetween;
                    headerRow.style.marginBottom = 5;

                    var recentOpsLabel = new Label("Operation History (Persistent):");
                    recentOpsLabel.style.fontSize = 12;
                    recentOpsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    headerRow.Add(recentOpsLabel);

                    var clearButton = new Button(() =>
                    {
                        MCPOperationTracker.ClearOperationsLog();
                        UpdateOperationUI();
                    });
                    clearButton.text = "Clear Log";
                    clearButton.style.fontSize = 10;
                    clearButton.style.paddingLeft = 8;
                    clearButton.style.paddingRight = 8;
                    clearButton.style.paddingTop = 2;
                    clearButton.style.paddingBottom = 2;
                    headerRow.Add(clearButton);

                    operationsSection.Add(headerRow);

                    // Show parameters toggle
                    _showParametersToggle = new Toggle("Show input parameters");
                    _showParametersToggle.value = _showParameters;
                    _showParametersToggle.style.marginBottom = 5;
                    _showParametersToggle.RegisterValueChangedCallback(evt =>
                    {
                        _showParameters = evt.newValue;
                        UpdateOperationUI(); // Refresh the operations display
                    });
                    operationsSection.Add(_showParametersToggle);

                    // Operations log scroll view
                    _operationsScrollView = new ScrollView(ScrollViewMode.Vertical);
                    _operationsScrollView.style.maxHeight = 300;
                    _operationsScrollView.style.borderTopWidth = 1;
                    _operationsScrollView.style.borderBottomWidth = 1;
                    _operationsScrollView.style.borderLeftWidth = 1;
                    _operationsScrollView.style.borderRightWidth = 1;
                    _operationsScrollView.style.borderTopColor = new Color(0.3f, 0.3f, 0.3f);
                    _operationsScrollView.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);
                    _operationsScrollView.style.borderLeftColor = new Color(0.3f, 0.3f, 0.3f);
                    _operationsScrollView.style.borderRightColor = new Color(0.3f, 0.3f, 0.3f);
                    _operationsScrollView.style.paddingTop = 4;
                    _operationsScrollView.style.paddingBottom = 4;
                    operationsSection.Add(_operationsScrollView);
                }

                container.Add(operationsSection);
            }

            return container;
        }

        private VisualElement CreateConfigurationTab()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            var header = new Label("Configuration");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            container.Add(header);

            // Status Indicator
            var statusIndicator = new VisualElement();
            statusIndicator.style.flexDirection = FlexDirection.Row;
            statusIndicator.style.marginBottom = 10;
            statusIndicator.style.paddingBottom = 5;
            statusIndicator.style.paddingLeft = 5;
            statusIndicator.style.paddingRight = 5;
            statusIndicator.style.paddingTop = 5;
            statusIndicator.style.backgroundColor = _config.IsInMemory() ? new Color(0.8f, 0.6f, 0.0f, 0.3f) : new Color(0.0f, 0.6f, 0.0f, 0.3f);

            var statusDot = new Label("●");
            statusDot.style.fontSize = 14;
            statusDot.style.color = _config.IsInMemory() ? new Color(1f, 0.8f, 0f) : Color.green;
            statusDot.style.marginRight = 5;
            statusIndicator.Add(statusDot);

            var statusText = new Label(_config.IsInMemory()
                ? "⚠️ In-memory configuration (changes will not persist until saved)"
                : "✓ Configuration saved to Assets/Resources/MCPConfiguration.asset");
            statusText.style.fontSize = 11;
            statusIndicator.Add(statusText);

            container.Add(statusIndicator);

            // Server URL
            _serverUrlField = new TextField("Server URL");
            _serverUrlField.value = _config.serverUrl;
            _serverUrlField.style.marginBottom = 5;
            container.Add(_serverUrlField);

            // Docker Image
            _dockerImageField = new TextField("Docker Image");
            _dockerImageField.value = _config.dockerImage;
            _dockerImageField.style.marginBottom = 10;
            container.Add(_dockerImageField);

            // Auto-connect toggle
            _autoConnectToggle = new Toggle("Auto-connect on startup");
            _autoConnectToggle.value = _config.autoConnect;
            _autoConnectToggle.style.marginBottom = 5;
            container.Add(_autoConnectToggle);

            // Auto-start toggle
            _autoStartToggle = new Toggle("Auto-start container");
            _autoStartToggle.value = _config.autoStartContainer;
            _autoStartToggle.style.marginBottom = 5;
            container.Add(_autoStartToggle);

            // Enable MCP logs toggle
            _enableMcpLogsToggle = new Toggle("Enable MCP logs in Console");
            _enableMcpLogsToggle.value = _config.enableMcpLogs;
            _enableMcpLogsToggle.tooltip = "Show MCP connection, protocol, and operation logs in Unity Console";
            _enableMcpLogsToggle.style.marginBottom = 5;
            container.Add(_enableMcpLogsToggle);

            // Verbose logging toggle
            _verboseLoggingToggle = new Toggle("Verbose logging (debug)");
            _verboseLoggingToggle.value = _config.verboseLogging;
            _verboseLoggingToggle.tooltip = "Show additional debug information (only when MCP logs enabled)";
            _verboseLoggingToggle.style.marginBottom = 15;
            container.Add(_verboseLoggingToggle);

            // Buttons row
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 10;

            var saveButton = new Button(SaveConfiguration) { text = "Save Configuration" };
            saveButton.style.flexGrow = 1;
            saveButton.style.marginRight = 5;
            buttonRow.Add(saveButton);

            var resetButton = new Button(ResetConfiguration) { text = "Reset to Defaults" };
            resetButton.style.flexGrow = 1;
            buttonRow.Add(resetButton);

            container.Add(buttonRow);

            return container;
        }

        private VisualElement CreateLogsTab()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            var header = new Label("Server Logs");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            container.Add(header);

            // Buttons
            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.marginBottom = 10;

            var refreshLogsButton = new Button(() => _ = RefreshLogsAsync()) { text = "Refresh Logs" };
            refreshLogsButton.style.flexGrow = 1;
            refreshLogsButton.style.marginRight = 5;
            buttonRow.Add(refreshLogsButton);

            var clearLogsButton = new Button(ClearLogs) { text = "Clear" };
            clearLogsButton.style.flexGrow = 1;
            buttonRow.Add(clearLogsButton);

            container.Add(buttonRow);

            // Logs scroll view
            _logsScrollView = new ScrollView();
            _logsScrollView.style.flexGrow = 1;
            _logsScrollView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            _logsScrollView.style.paddingBottom = 5;
            _logsScrollView.style.paddingLeft = 5;
            _logsScrollView.style.paddingRight = 5;
            _logsScrollView.style.paddingTop = 5;
            container.Add(_logsScrollView);

            return container;
        }

        private VisualElement CreateLLMConfigTab()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            var header = new Label("LLM Configuration");
            header.style.fontSize = 16;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = 10;
            container.Add(header);

            var instructionLabel = new Label("Select your LLM/IDE and copy the configuration below:");
            instructionLabel.style.marginBottom = 10;
            instructionLabel.style.whiteSpace = WhiteSpace.Normal;
            container.Add(instructionLabel);

            // Dropdown for LLM selection
            var llmChoices = new List<string> { "Claude Code", "Claude Desktop", "GitHub Copilot", "Cursor" };
            var dropdown = new UnityEngine.UIElements.DropdownField("LLM/IDE", llmChoices, 0);
            dropdown.style.marginBottom = 10;
            container.Add(dropdown);

            // Config text field (read-only)
            var configTextField = new TextField();
            configTextField.multiline = true;
            configTextField.isReadOnly = true;
            configTextField.style.flexGrow = 1;
            configTextField.style.minHeight = 200;
            configTextField.style.whiteSpace = WhiteSpace.Normal;
            configTextField.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            configTextField.style.paddingBottom = 5;
            configTextField.style.paddingLeft = 5;
            configTextField.style.paddingRight = 5;
            configTextField.style.paddingTop = 5;
            container.Add(configTextField);

            // Instructions text
            var instructionsTextField = new TextField("Instructions");
            instructionsTextField.multiline = true;
            instructionsTextField.isReadOnly = true;
            instructionsTextField.style.minHeight = 100;
            instructionsTextField.style.marginTop = 10;
            instructionsTextField.style.whiteSpace = WhiteSpace.Normal;
            instructionsTextField.style.backgroundColor = new Color(0.2f, 0.25f, 0.2f);
            container.Add(instructionsTextField);

            // Copy button
            var copyButton = new Button(() => CopyToClipboard(configTextField.value)) { text = "Copy Configuration" };
            copyButton.style.marginTop = 10;
            copyButton.style.height = 30;
            container.Add(copyButton);

            // Update config text when dropdown changes
            dropdown.RegisterValueChangedCallback(evt => UpdateLLMConfig(evt.newValue, configTextField, instructionsTextField));

            // Initialize with first option
            UpdateLLMConfig(llmChoices[0], configTextField, instructionsTextField);

            return container;
        }

        private void UpdateLLMConfig(string llmName, TextField configField, TextField instructionsField)
        {
            string config = "";
            string instructions = "";

            switch (llmName)
            {
                case "Claude Code":
                    config = $@"# Unity-Managed MCP Server (HTTP Transport)
#
# Unity starts and manages the server container.
# Claude Code connects to the running server via HTTP.
#
# Setup command:

claude mcp add --transport http unity-mcp http://localhost:8080/mcp

# This connects Claude Code to:
# - Unity-managed container on port 8080
# - HTTP endpoint at /mcp (Streamable HTTP protocol)
# - Tools are available when Unity is running";
                    instructions = $@"Setup Instructions:

1. Start the MCP server from Unity Dashboard (Status tab → Start Server)
2. Wait for 'Running' status
3. Open your terminal and run:

   claude mcp add --transport http unity-mcp http://localhost:8080/mcp

4. In Claude Code, type /mcp to verify 'unity-mcp' shows as connected
5. Use the tools - they work only when Unity is running!

Available MCP Tools:
• unity_get_console_logs - Get Unity console logs
• unity_get_project_info - Get project information
• unity_get_compilation_status - Check compilation status
• unity_list_scene_objects - Get scene hierarchy
• unity_create_game_object - Create new GameObject
• unity_trigger_script_compilation - Force recompile

Note: Unity manages the server lifecycle. Claude Code just connects to it.";
                    break;

                case "Claude Desktop":
                    config = @"# Unity-Managed MCP Server (HTTP Transport)
# Add this to claude_desktop_config.json:

{
  ""mcpServers"": {
    ""unity-mcp"": {
      ""url"": ""http://localhost:8080/mcp""
    }
  }
}";
                    instructions = @"Setup Instructions:

1. Start the MCP server from Unity Dashboard (Status tab → Start Server)
2. Wait for 'Running' status
3. Open Claude Desktop settings:
   - Mac: ~/Library/Application Support/Claude/claude_desktop_config.json
   - Windows: %APPDATA%\Claude\claude_desktop_config.json
   - Linux: ~/.config/Claude/claude_desktop_config.json
4. Add the configuration above to your config file
5. Restart Claude Desktop
6. The Unity MCP tools will be available (only when Unity is running!)

Available MCP Tools:
• unity_get_console_logs, unity_get_project_info, unity_get_compilation_status
• unity_list_scene_objects, unity_create_game_object, unity_trigger_script_compilation";
                    break;

                case "GitHub Copilot":
                    config = @"# Unity-Managed MCP Server (HTTP Transport)
# Add this to VS Code settings.json:

{
  ""mcp"": {
    ""servers"": {
      ""unity-mcp"": {
        ""url"": ""http://localhost:8080/mcp""
      }
    }
  }
}";
                    instructions = @"Setup Instructions:

1. Start the MCP server from Unity Dashboard (Status tab → Start Server)
2. Wait for 'Running' status
3. Open VS Code settings (Ctrl+, or Cmd+,)
4. Search for 'MCP' or 'Copilot MCP'
5. Click 'Edit in settings.json'
6. Add the configuration above
7. Restart VS Code
8. The Unity MCP tools will be available (only when Unity is running!)

Available MCP Tools:
• unity_get_console_logs, unity_get_project_info, unity_get_compilation_status
• unity_list_scene_objects, unity_create_game_object, unity_trigger_script_compilation

Note: GitHub Copilot must support MCP for this to work.";
                    break;

                case "Cursor":
                    config = @"# Unity-Managed MCP Server (HTTP Transport)
# Add this to Cursor settings:

{
  ""mcpServers"": {
    ""unity-mcp"": {
      ""url"": ""http://localhost:8080/mcp""
    }
  }
}";
                    instructions = @"Setup Instructions:

1. Start the MCP server from Unity Dashboard (Status tab → Start Server)
2. Wait for 'Running' status
3. Open Cursor settings:
   - Mac: ~/Library/Application Support/Cursor/User/globalSettings/settings.json
   - Windows: %APPDATA%\Cursor\User\globalSettings\settings.json
   - Linux: ~/.config/Cursor/User/globalSettings/settings.json
4. Add the configuration above to your config file
5. Restart Cursor
6. The Unity MCP tools will be available (only when Unity is running!)

Available MCP Tools:
• unity_get_console_logs, unity_get_project_info, unity_get_compilation_status
• unity_list_scene_objects, unity_create_game_object, unity_trigger_script_compilation

Note: Cursor must support MCP for this to work.";
                    break;
            }

            configField.value = config;
            instructionsField.value = instructions;
        }

        private void CopyToClipboard(string text)
        {
            EditorGUIUtility.systemCopyBuffer = text;
            MCPLogger.Log("[MCPDashboard] Configuration copied to clipboard");
        }

        // Event Handlers
        private void OnServerStatusChanged(MCPServerManager.ServerStatus status, string message)
        {
            EditorApplication.delayCall += () =>
            {
                if (_statusLabel != null)
                {
                    _statusLabel.text = $"Status: {status}\n{message}";
                }
            };
        }

        private void OnClientConnected()
        {
            EditorApplication.delayCall += () =>
            {
                if (_connectionLabel != null)
                {
                    _connectionLabel.text = "Connected ✓";
                    _connectionLabel.style.color = Color.green;
                }
            };
        }

        private void OnClientDisconnected(string reason)
        {
            EditorApplication.delayCall += () =>
            {
                if (_connectionLabel != null)
                {
                    _connectionLabel.text = $"Disconnected: {reason}";
                    _connectionLabel.style.color = Color.red;
                }
            };
        }

        private void OnClientError(string error)
        {
            EditorApplication.delayCall += () =>
            {
                MCPLogger.LogError($"[MCPDashboard] Client error: {error}");
            };
        }

        // Button Actions
        private async Task StartServerAsync()
        {
            await _serverManager.StartServerAsync();
            await RefreshStatusAsync();
        }

        private async Task StopServerAsync()
        {
            // Graceful shutdown: disconnect client before stopping server
            if (_client.IsConnected)
            {
                await _client.DisconnectAsync();
            }

            await _serverManager.StopServerAsync();
            await RefreshStatusAsync();
        }

        private async Task RefreshStatusAsync()
        {
            await _serverManager.UpdateStatusAsync();
        }

        private async Task ConnectAsync()
        {
            await _client.ConnectAsync();
        }

        private async Task DisconnectAsync()
        {
            await _client.DisconnectAsync();
        }

        private async Task RefreshLogsAsync()
        {
            var logs = await _serverManager.GetServerLogsAsync(100);
            EditorApplication.delayCall += () =>
            {
                if (_logsScrollView != null)
                {
                    _logsScrollView.Clear();
                    var logTextField = new TextField();
                    logTextField.isReadOnly = true;
                    logTextField.multiline = true;
                    logTextField.value = logs;
                    logTextField.style.whiteSpace = WhiteSpace.Normal;
                    logTextField.style.flexGrow = 1;
                    logTextField.style.unityFontStyleAndWeight = FontStyle.Normal;
                    _logsScrollView.Add(logTextField);
                }
            };
        }

        private void ClearLogs()
        {
            _logsScrollView?.Clear();
        }

        private void SaveConfiguration()
        {
            _config.serverUrl = _serverUrlField.value;
            _config.dockerImage = _dockerImageField.value;
            _config.autoConnect = _autoConnectToggle.value;
            _config.autoStartContainer = _autoStartToggle.value;
            _config.enableMcpLogs = _enableMcpLogsToggle.value;
            _config.verboseLogging = _verboseLoggingToggle.value;

            _config.SaveToResources();

            MCPLogger.Log("[MCPDashboard] Configuration saved");

            // Refresh the dashboard to update status indicator
            rootVisualElement.Clear();
            CreateGUI();
        }

        private void ResetConfiguration()
        {
            if (EditorUtility.DisplayDialog(
                "Reset Configuration",
                "Are you sure you want to reset all settings to default values?",
                "Reset",
                "Cancel"))
            {
                _config.ResetToDefaults();

                // Refresh UI fields
                _serverUrlField.value = _config.serverUrl;
                _dockerImageField.value = _config.dockerImage;
                _autoConnectToggle.value = _config.autoConnect;
                _autoStartToggle.value = _config.autoStartContainer;
                _enableMcpLogsToggle.value = _config.enableMcpLogs;
                _verboseLoggingToggle.value = _config.verboseLogging;

                MCPLogger.Log("[MCPDashboard] Configuration reset to defaults");

                // Refresh the dashboard to update status indicator
                rootVisualElement.Clear();
                CreateGUI();
            }
        }
    }
}
