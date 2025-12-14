using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp
{
    /// <summary>
    /// WebSocket client for communicating with Unity MCP Server via JSON-RPC 2.0
    /// </summary>
    public class MCPClient : IDisposable
    {
        private ClientWebSocket _webSocket;
        private CancellationTokenSource _cancellationTokenSource;
        private string _serverUrl;
        private bool _isConnected;
        private bool _isConnecting;
        private bool _autoReconnect;
        private int _reconnectAttempts;
        private int _reconnectDelay;

        // Queue for marshalling callbacks to main thread
        private readonly ConcurrentQueue<Action> _mainThreadQueue = new ConcurrentQueue<Action>();

        public event Action OnConnected;
        public event Action<string> OnDisconnected;
        public event Action<string, object> OnNotificationReceived;
        public event Action<string, string, object> OnRequestReceived; // requestId, method, parameters
        public event Action<string> OnError;

        public bool IsConnected => _isConnected && _webSocket?.State == WebSocketState.Open;
        public bool AutoReconnect { get => _autoReconnect; set => _autoReconnect = value; }

        public MCPClient(string serverUrl = "ws://localhost:3727/ws", bool autoReconnect = true, int reconnectAttempts = 3, int reconnectDelay = 5)
        {
            _serverUrl = serverUrl;
            _autoReconnect = autoReconnect;
            _reconnectAttempts = reconnectAttempts;
            _reconnectDelay = reconnectDelay;
        }

        public async Task<bool> ConnectAsync()
        {
            if (_isConnecting || IsConnected)
            {
                MCPLogger.LogWarning("[MCPClient] Already connected or connecting");
                return IsConnected;
            }

            _isConnecting = true;

            try
            {
                _webSocket?.Dispose();
                _webSocket = new ClientWebSocket();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                MCPLogger.Log($"[MCPClient] Connecting to {_serverUrl}...");

                await _webSocket.ConnectAsync(new Uri(_serverUrl), _cancellationTokenSource.Token);

                _isConnected = true;
                _isConnecting = false;

                MCPLogger.Log("[MCPClient] Connected successfully");
                OnConnected?.Invoke();

                // Start receiving messages
                _ = Task.Run(ReceiveMessagesAsync);

                return true;
            }
            catch (Exception ex)
            {
                _isConnected = false;
                _isConnecting = false;
                MCPLogger.LogError($"[MCPClient] Connection failed: {ex.Message}");
                OnError?.Invoke($"Connection failed: {ex.Message}");
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            try
            {
                _cancellationTokenSource?.Cancel();

                if (_webSocket?.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Client disconnecting", CancellationToken.None);
                }

                _isConnected = false;
                MCPLogger.Log("[MCPClient] Disconnected");
                OnDisconnected?.Invoke("Client initiated disconnect");
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[MCPClient] Error during disconnect: {ex.Message}");
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[1024 * 4];
            var messageBuilder = new StringBuilder();

            try
            {
                while (IsConnected && !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _isConnected = false;
                        var reason = result.CloseStatusDescription ?? "Server closed connection";
                        MCPLogger.LogWarning($"[MCPClient] Connection closed: {reason}");
                        OnDisconnected?.Invoke(reason);

                        // Attempt to reconnect if enabled
                        if (_autoReconnect)
                        {
                            _ = AttemptReconnectAsync();
                        }
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        messageBuilder.Append(text);

                        if (result.EndOfMessage)
                        {
                            var message = messageBuilder.ToString();
                            messageBuilder.Clear();
                            HandleMessage(message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                MCPLogger.Log("[MCPClient] Receive operation cancelled");
            }
            catch (Exception ex)
            {
                _isConnected = false;
                MCPLogger.LogError($"[MCPClient] Error receiving messages: {ex.Message}");
                OnError?.Invoke($"Error receiving messages: {ex.Message}");

                // Attempt to reconnect if enabled
                if (_autoReconnect)
                {
                    _ = AttemptReconnectAsync();
                }
            }
        }

        private async Task AttemptReconnectAsync()
        {
            for (int attempt = 1; attempt <= _reconnectAttempts; attempt++)
            {
                MCPLogger.Log($"[MCPClient] Reconnection attempt {attempt}/{_reconnectAttempts}...");

                await Task.Delay(_reconnectDelay * 1000);

                var success = await ConnectAsync();
                if (success)
                {
                    MCPLogger.Log($"[MCPClient] Reconnected successfully on attempt {attempt}");
                    return;
                }
            }

            MCPLogger.LogWarning($"[MCPClient] Failed to reconnect after {_reconnectAttempts} attempts");
        }

        private void HandleMessage(string message)
        {
            try
            {
                var jsonRpc = JsonConvert.DeserializeObject<JsonRpcMessage>(message);

                if (!string.IsNullOrEmpty(jsonRpc.method))
                {
                    // Check if this is a request (has both method and id) or notification (method only)
                    if (!string.IsNullOrEmpty(jsonRpc.id))
                    {
                        // This is a request from the server - enqueue for main thread processing
                        var id = jsonRpc.id;
                        var method = jsonRpc.method;
                        var parameters = jsonRpc.@params;
                        _mainThreadQueue.Enqueue(() => OnRequestReceived?.Invoke(id, method, parameters));
                    }
                    else
                    {
                        // This is a notification from the server
                        var method = jsonRpc.method;
                        var parameters = jsonRpc.@params;
                        _mainThreadQueue.Enqueue(() => OnNotificationReceived?.Invoke(method, parameters));
                    }
                }
                // Responses to our requests are handled elsewhere (if needed)
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[MCPClient] Error parsing message: {ex.Message}");
            }
        }

        /// <summary>
        /// Process queued main thread callbacks. Call this from EditorApplication.update or similar.
        /// </summary>
        public void ProcessMainThreadQueue()
        {
            while (_mainThreadQueue.TryDequeue(out var action))
            {
                try
                {
                    action?.Invoke();
                }
                catch (Exception ex)
                {
                    MCPLogger.LogError($"[MCPClient] Error processing main thread callback: {ex.Message}");
                }
            }
        }

        public async Task SendNotificationAsync(string method, object parameters = null)
        {
            if (!IsConnected)
            {
                MCPLogger.LogError("[MCPClient] Cannot send notification: not connected");
                return;
            }

            var notification = new JsonRpcNotification
            {
                jsonrpc = "2.0",
                method = method,
                @params = parameters
            };

            var json = JsonConvert.SerializeObject(notification);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource.Token);

                // Notification sent successfully (no need to log unless debugging)
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[MCPClient] Error sending notification: {ex.Message}");
                OnError?.Invoke($"Error sending notification: {ex.Message}");
            }
        }

        public async Task SendResponseAsync(string requestId, object result)
        {
            if (!IsConnected)
            {
                MCPLogger.LogError("[MCPClient] Cannot send response: not connected");
                return;
            }

            var response = new JsonRpcResponse
            {
                jsonrpc = "2.0",
                id = requestId,
                result = result
            };

            var json = JsonConvert.SerializeObject(response);
            var bytes = Encoding.UTF8.GetBytes(json);

            try
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Text,
                    true,
                    _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[MCPClient] Error sending response: {ex.Message}");
                OnError?.Invoke($"Error sending response: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _webSocket?.Dispose();
        }

        [Serializable]
        private class JsonRpcMessage
        {
            [JsonProperty("jsonrpc")]
            public string jsonrpc;
            [JsonProperty("id")]
            public string id;
            [JsonProperty("method")]
            public string method;
            [JsonProperty("params")]
            public object @params;
            [JsonProperty("result")]
            public object result;
            [JsonProperty("error")]
            public JsonRpcError error;
        }

        [Serializable]
        private class JsonRpcNotification
        {
            [JsonProperty("jsonrpc")]
            public string jsonrpc;
            [JsonProperty("method")]
            public string method;
            [JsonProperty("params")]
            public object @params;
        }

        [Serializable]
        private class JsonRpcResponse
        {
            [JsonProperty("jsonrpc")]
            public string jsonrpc;
            [JsonProperty("id")]
            public string id;
            [JsonProperty("result")]
            public object result;
        }

        [Serializable]
        private class JsonRpcError
        {
            [JsonProperty("code")]
            public int code;
            [JsonProperty("message")]
            public string message;
            [JsonProperty("data")]
            public object data;
        }
    }
}
