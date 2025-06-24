using System.Collections.Concurrent;
using System.Text.Json;
using WebSocketSharp; 
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace UnityMCPSharp.Server
{
    public class UnityBridgeClient : IDisposable
    {
        private const string DEFAULT_CLIENT_NAME = "UnityMCPSharpBridgeClient";
        private static readonly Lazy<UnityBridgeClient> _instance = new Lazy<UnityBridgeClient>(() => new UnityBridgeClient());
        
        public static UnityBridgeClient Instance => _instance.Value;
        
        private readonly Uri _uri;
        private WebSocket _webSocket;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> _pendingRequests = new();
        private readonly TimeSpan _requestTimeout;
        private readonly object _lock = new();
        private bool _isDisposed;
        private string _clientName;
        private bool _isConnecting;
        private TaskCompletionSource<bool> _connectionTcs;

        /// <summary>
        /// Private constructor for the singleton pattern
        /// </summary>
        private UnityBridgeClient(string host = "host.docker.internal", int timeoutSeconds = 10)
        {
            _clientName = DEFAULT_CLIENT_NAME;
            
            // Get UnityBridge port from environment variable or use default
            string portEnv = Environment.GetEnvironmentVariable("UNITY_BRIDGE_PORT");
            int port = string.IsNullOrEmpty(portEnv) || !int.TryParse(portEnv, out int parsedPort) ? 8090 : parsedPort;
            
            _uri = new Uri($"ws://{host}:{port}/McpUnity");
            _requestTimeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            // Initialize the WebSocket but don't connect yet
            InitializeWebSocket();
            
            Console.WriteLine($"UnityBridgeClient initialized with URI: {_uri}");
        }

        private void InitializeWebSocket()
        {
            // Clean up any existing websocket first
            CleanupWebSocket();
            
            // Create the new WebSocket
            _webSocket = new WebSocket(_uri.ToString());
            
            // Set headers if we have a client name
            if (!string.IsNullOrEmpty(_clientName))
            {
                _webSocket.SetCookie(new WebSocketSharp.Net.Cookie("X-Client-Name", _clientName));
            }
            
            // Setup event handlers
            _webSocket.OnOpen += OnWebSocketOpen;
            _webSocket.OnMessage += OnWebSocketMessage;
            _webSocket.OnError += OnWebSocketError;
            _webSocket.OnClose += OnWebSocketClose;
        }

        private void OnWebSocketOpen(object sender, EventArgs e)
        {
            lock (_lock)
            {
                _isConnecting = false;
                _connectionTcs?.TrySetResult(true);
            }
        }
        
        public async Task<bool> ConnectAsync(string clientName = null, CancellationToken cancellationToken = default)
        {
            // Quick check for already connected state
            if (_webSocket.IsAlive)
                return true;

            TaskCompletionSource<bool> connectionTcs = null;
            
            lock (_lock)
            {
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(UnityBridgeClient));
                
                // Return existing connection task if one is in progress
                if (_isConnecting)
                    return _connectionTcs.Task.Result;
                
                // Update client name if provided
                if (clientName != null)
                    _clientName = clientName;
                
                // Initialize a new WebSocket with updated settings
                InitializeWebSocket();
                
                // Mark that we're connecting and create a new task source
                _isConnecting = true;
                _connectionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                connectionTcs = _connectionTcs;
                
                try
                {
                    // Attempt to connect
                    _webSocket.Connect();
                }
                catch (Exception ex)
                {
                    _isConnecting = false;
                    _connectionTcs.TrySetException(ex);
                    throw;
                }
            }
            
            using var timeoutCts = new CancellationTokenSource(_requestTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);
            
            try
            {
                // Wait for the connection to complete or timeout
                using var registration = linkedCts.Token.Register(() => 
                {
                    if (timeoutCts.IsCancellationRequested)
                        connectionTcs.TrySetException(new TimeoutException($"Connection attempt timed out after {_requestTimeout.TotalSeconds} seconds"));
                    else
                        connectionTcs.TrySetCanceled(cancellationToken);
                });
                
                return await connectionTcs.Task.ConfigureAwait(false);
            }
            catch
            {
                lock (_lock)
                {
                    _isConnecting = false;
                }
                throw;
            }
        }

        public Task<JsonElement> SendRequestAsync(string method, object @params)
        {
            var id = Guid.NewGuid().ToString();
            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingRequests[id] = tcs;

            var request = new
            {
                jsonrpc = "2.0",
                id,
                method,
                @params
            };
            var json = JsonSerializer.Serialize(request);
            EnsureConnected();
            _webSocket.Send(json);

            var cts = new CancellationTokenSource(_requestTimeout);
            cts.Token.Register(() => {
                if (_pendingRequests.TryRemove(id, out var timedOutTcs))
                {
                    timedOutTcs.TrySetException(new TimeoutException($"Request {id} timed out after {_requestTimeout.TotalSeconds} seconds"));
                }
            });
            return tcs.Task;
        }

        private async Task EnsureConnected()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UnityBridgeClient));
                
            if (_webSocket == null || !_webSocket.IsAlive)
            {
                await ConnectAsync(_clientName).ConfigureAwait(false);
            }
        }

        private void HandleMessage(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                if (root.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
                {
                    var id = idProp.GetString();
                    if (_pendingRequests.TryRemove(id, out var tcs))
                    {
                        if (root.TryGetProperty("error", out var errorProp))
                        {
                            tcs.SetException(new Exception(errorProp.ToString()));
                        }
                        else if (root.TryGetProperty("result", out var resultProp))
                        {
                            tcs.SetResult(resultProp);
                        }
                        else
                        {
                            tcs.SetException(new Exception("Malformed response: missing result/error"));
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Optionally log
            }
        }

        private void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            HandleMessage(e.Data);
        }

        private void OnWebSocketError(object sender, ErrorEventArgs e)
        {
            // Log the error or handle it appropriately
            Console.Error.WriteLine($"WebSocket error: {e.Message}");
            
            // Cancel any pending requests with the error
            foreach (var request in _pendingRequests)
            {
                request.Value.TrySetException(new Exception($"WebSocket error: {e.Message}"));
            }
            _pendingRequests.Clear();
        }

        private void OnWebSocketClose(object sender, CloseEventArgs e)
        {
            Console.WriteLine($"WebSocket closed: {e.Reason} (Code: {e.Code})");
            
            // Cancel any pending requests
            foreach (var request in _pendingRequests)
            {
                request.Value.TrySetException(new Exception($"WebSocket closed: {e.Reason}"));
            }
            _pendingRequests.Clear();
            
            // You could also implement auto-reconnect logic here if desired
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            
            if (disposing)
            {
                // Cancel any pending requests
                foreach (var request in _pendingRequests)
                {
                    request.Value.TrySetException(new ObjectDisposedException(nameof(UnityBridgeClient)));
                }
                _pendingRequests.Clear();
                
                // Clean up the WebSocket
                CleanupWebSocket();
            }
            
            _isDisposed = true;
        }
        
        private void CleanupWebSocket()
        {
            if (_webSocket != null)
            {
                try
                {
                    _webSocket.OnMessage -= OnWebSocketMessage;
                    _webSocket.OnError -= OnWebSocketError;
                    _webSocket.OnClose -= OnWebSocketClose;
                    
                    if (_webSocket.IsAlive)
                    {
                        _webSocket.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error during WebSocket cleanup: {ex.Message}");
                }
                finally
                {
                    _webSocket = null;
                }
            }
        }
        
        ~UnityBridgeClient()
        {
            Dispose(false);
        }
    }
}
