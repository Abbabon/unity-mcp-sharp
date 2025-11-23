using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using UnityMcpServer.Models;

namespace UnityMcpServer.Services;

public class UnityWebSocketService
{
    private readonly ILogger<UnityWebSocketService> _logger;
    private readonly EditorSessionManager _sessionManager;
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<object?>> _pendingRequests = new();
    private readonly JsonSerializerOptions _jsonOptions;

    // Event raised when Unity notifies about a resource update
    public event Action<string>? ResourceUpdated;

    public UnityWebSocketService(ILogger<UnityWebSocketService> logger, EditorSessionManager sessionManager)
    {
        _logger = logger;
        _sessionManager = sessionManager;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task HandleWebSocketAsync(HttpContext context)
    {
        var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        var connectionId = Guid.NewGuid().ToString();
        _connections.TryAdd(connectionId, webSocket);

        _logger.LogInformation("Unity Editor connected: {ConnectionId}", connectionId);

        try
        {
            await ReceiveMessagesAsync(webSocket, connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in WebSocket connection {ConnectionId}", connectionId);
        }
        finally
        {
            _connections.TryRemove(connectionId, out _);
            _sessionManager.UnregisterEditor(connectionId);
            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Connection closed", CancellationToken.None);
            }
            webSocket.Dispose();
            _logger.LogInformation("Unity Editor disconnected: {ConnectionId}", connectionId);
        }
    }

    private async Task ReceiveMessagesAsync(WebSocket webSocket, string connectionId)
    {
        var buffer = new byte[1024 * 4];
        var messageBuilder = new StringBuilder();

        while (webSocket.State == WebSocketState.Open)
        {
            var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
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

                    await HandleJsonRpcMessageAsync(webSocket, message, connectionId);
                }
            }
        }
    }

    private async Task HandleJsonRpcMessageAsync(WebSocket webSocket, string message, string connectionId)
    {
        try
        {
            // Try to parse as a generic JSON-RPC message first
            using var doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // Check if this is a response to one of our pending requests
            if (root.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
            {
                var id = idElement.GetString();
                if (id != null && _pendingRequests.TryRemove(id, out var tcs))
                {
                    // This is a response to our request
                    if (root.TryGetProperty("result", out var resultElement))
                    {
                        var result = JsonSerializer.Deserialize<object>(resultElement.GetRawText(), _jsonOptions);
                        tcs.SetResult(result);
                        _logger.LogInformation("Received response for request {RequestId}", id);
                    }
                    else if (root.TryGetProperty("error", out var errorElement))
                    {
                        tcs.SetException(new Exception($"Unity returned error: {errorElement.GetRawText()}"));
                    }
                    return;
                }
            }

            // If not a response, treat as a notification from Unity
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(message, _jsonOptions);
            if (request == null)
            {
                await SendErrorAsync(webSocket, null, -32700, "Parse error");
                return;
            }

            _logger.LogInformation("Received notification from Unity: {Method}", request.Method);

            // Handle editor registration
            if (request.Method == "unity.register" && request.Params != null)
            {
                var paramsJson = JsonSerializer.Serialize(request.Params, _jsonOptions);
                var metadata = JsonSerializer.Deserialize<EditorMetadata>(paramsJson, _jsonOptions);
                if (metadata != null)
                {
                    _sessionManager.RegisterEditor(connectionId, metadata);
                    _logger.LogInformation("Unity Editor registered: {DisplayName}", metadata.DisplayName);

                    // Send acknowledgment back with the assigned connection ID
                    var response = new JsonRpcResponse
                    {
                        Id = request.Id,
                        Result = new
                        {
                            connectionId,
                            status = "registered",
                            message = $"Registered as {metadata.DisplayName}"
                        }
                    };
                    await SendResponseAsync(webSocket, response);
                }
                return;
            }

            // Check if this is a resource update notification from Unity
            if (request.Method?.StartsWith("unity.resourceUpdated") == true && request.Params != null)
            {
                // Extract resource URI from parameters
                var paramsJson = JsonSerializer.Serialize(request.Params, _jsonOptions);
                using var paramsDoc = JsonDocument.Parse(paramsJson);
                if (paramsDoc.RootElement.TryGetProperty("uri", out var uriElement))
                {
                    var resourceUri = uriElement.GetString();
                    if (resourceUri != null)
                    {
                        _logger.LogInformation("Unity notified resource update: {ResourceUri}", resourceUri);
                        // Raise event for ResourceSubscriptionService to handle
                        ResourceUpdated?.Invoke(resourceUri);
                    }
                }
            }

            // For notifications from Unity, acknowledge if it has an ID
            if (request.Id != null)
            {
                var response = new JsonRpcResponse
                {
                    Id = request.Id,
                    Result = new { status = "acknowledged" }
                };
                await SendResponseAsync(webSocket, response);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON received from {ConnectionId}", connectionId);
            await SendErrorAsync(webSocket, null, -32700, "Parse error");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message from {ConnectionId}", connectionId);
            await SendErrorAsync(webSocket, null, -32603, "Internal error");
        }
    }

    private Task<JsonRpcResponse> ProcessRequestAsync(JsonRpcRequest request)
    {
        // This will be extended to dispatch to actual Unity command handlers
        // For now, return a simple acknowledgment
        var response = new JsonRpcResponse
        {
            Id = request.Id,
            Result = new
            {
                status = "received",
                method = request.Method,
                message = "Request queued for processing by Unity Editor"
            }
        };
        return Task.FromResult(response);
    }

    private async Task SendResponseAsync(WebSocket webSocket, JsonRpcResponse response)
    {
        var json = JsonSerializer.Serialize(response, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task SendErrorAsync(WebSocket webSocket, object? id, int code, string message)
    {
        var response = new JsonRpcResponse
        {
            Id = id,
            Error = new JsonRpcError
            {
                Code = code,
                Message = message
            }
        };

        await SendResponseAsync(webSocket, response);
    }

    /// <summary>
    /// Send a notification to a specific Unity Editor instance
    /// </summary>
    public async Task SendToEditorAsync(string editorConnectionId, string method, object? parameters)
    {
        if (!_connections.TryGetValue(editorConnectionId, out var connection))
        {
            throw new InvalidOperationException($"Unity Editor '{editorConnectionId}' is not connected");
        }

        if (connection.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Unity Editor '{editorConnectionId}' connection is not open (State: {connection.State})");
        }

        var notification = new JsonRpcNotification
        {
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await connection.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogInformation("Sent notification to editor {EditorId}: {Method}", editorConnectionId, method);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to editor {EditorId}", editorConnectionId);
            throw;
        }
    }

    /// <summary>
    /// Send a notification to the Unity Editor selected for the current MCP session.
    /// Uses smart auto-selection: auto-selects if only one editor, throws if multiple editors and none selected.
    /// </summary>
    public async Task SendToCurrentSessionEditorAsync(string method, object? parameters)
    {
        var sessionId = McpSessionContext.CurrentSessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            // No session context - fall back to first editor for backwards compatibility
            _logger.LogWarning("No MCP session context available, falling back to first connected editor");
            await BroadcastNotificationAsync(method, parameters);
            return;
        }

        var editorId = _sessionManager.GetOrAutoSelectEditor(sessionId);
        if (editorId == null)
        {
            var editorCount = _sessionManager.GetEditorCount();
            if (editorCount == 0)
            {
                throw new InvalidOperationException("No Unity Editor instances are connected. Please ensure a Unity Editor with the MCP package is running and connected.");
            }
            else
            {
                throw new InvalidOperationException($"Multiple Unity Editors are connected ({editorCount}), but no editor has been selected for this session. Use unity_list_editors to see available editors, then unity_select_editor to choose one.");
            }
        }

        await SendToEditorAsync(editorId, method, parameters);
    }

    /// <summary>
    /// Broadcast a notification to all connected Unity Editor instances
    /// </summary>
    public async Task BroadcastNotificationAsync(string method, object? parameters)
    {
        _logger.LogInformation("Broadcasting notification to Unity: {Method}, Connections: {Count}", method, _connections.Count);

        var notification = new JsonRpcNotification
        {
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(notification, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        foreach (var connection in _connections.Values)
        {
            if (connection.State == WebSocketState.Open)
            {
                try
                {
                    await connection.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                    _logger.LogInformation("Broadcast sent successfully to Unity");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error broadcasting notification");
                }
            }
            else
            {
                _logger.LogWarning("Skipping broadcast - WebSocket connection not open (State: {State})", connection.State);
            }
        }
    }

    /// <summary>
    /// Send a request to a specific Unity Editor and wait for the response
    /// </summary>
    public async Task<T?> SendRequestToEditorAsync<T>(string editorConnectionId, string method, object? parameters, int timeoutSeconds = 10)
    {
        if (!_connections.TryGetValue(editorConnectionId, out var connection))
        {
            throw new InvalidOperationException($"Unity Editor '{editorConnectionId}' is not connected");
        }

        if (connection.State != WebSocketState.Open)
        {
            throw new InvalidOperationException($"Unity Editor '{editorConnectionId}' connection is not open");
        }

        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>();
        _pendingRequests.TryAdd(requestId, tcs);

        var request = new JsonRpcRequest
        {
            Id = requestId,
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        try
        {
            await connection.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogInformation("Sent request {RequestId} to editor {EditorId}: {Method}", requestId, editorConnectionId, method);
        }
        catch (Exception ex)
        {
            _pendingRequests.TryRemove(requestId, out _);
            _logger.LogError(ex, "Error sending request to editor {EditorId}", editorConnectionId);
            throw;
        }

        // Wait for response with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == tcs.Task)
        {
            var result = await tcs.Task;
            if (result == null)
            {
                return default;
            }

            // Try to deserialize to target type
            var resultJson = JsonSerializer.Serialize(result, _jsonOptions);
            return JsonSerializer.Deserialize<T>(resultJson, _jsonOptions);
        }
        else
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new TimeoutException($"Request {requestId} to editor {editorConnectionId} timed out after {timeoutSeconds} seconds");
        }
    }

    /// <summary>
    /// Send a request to the Unity Editor selected for the current MCP session and wait for the response.
    /// Uses smart auto-selection.
    /// </summary>
    public async Task<T?> SendRequestToCurrentSessionEditorAsync<T>(string method, object? parameters, int timeoutSeconds = 10)
    {
        var sessionId = McpSessionContext.CurrentSessionId;
        if (string.IsNullOrEmpty(sessionId))
        {
            // No session context - fall back to first editor for backwards compatibility
            _logger.LogWarning("No MCP session context available, falling back to first connected editor");
            return await SendRequestAsync<T>(method, parameters, timeoutSeconds);
        }

        var editorId = _sessionManager.GetOrAutoSelectEditor(sessionId);
        if (editorId == null)
        {
            var editorCount = _sessionManager.GetEditorCount();
            if (editorCount == 0)
            {
                throw new InvalidOperationException("No Unity Editor instances are connected");
            }
            else
            {
                throw new InvalidOperationException($"Multiple Unity Editors are connected ({editorCount}), but no editor has been selected for this session. Use unity_list_editors and unity_select_editor.");
            }
        }

        return await SendRequestToEditorAsync<T>(editorId, method, parameters, timeoutSeconds);
    }

    /// <summary>
    /// Send a request to Unity and wait for the response (legacy method - sends to first editor)
    /// </summary>
    public async Task<T?> SendRequestAsync<T>(string method, object? parameters, int timeoutSeconds = 10)
    {
        if (_connections.IsEmpty)
        {
            throw new InvalidOperationException("No Unity Editor instances connected");
        }

        var requestId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<object?>();
        _pendingRequests.TryAdd(requestId, tcs);

        var request = new JsonRpcRequest
        {
            Id = requestId,
            Method = method,
            Params = parameters
        };

        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        // Send to first connected Unity instance
        var connection = _connections.Values.FirstOrDefault(ws => ws.State == WebSocketState.Open);
        if (connection == null)
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new InvalidOperationException("No active Unity Editor connection");
        }

        try
        {
            await connection.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            _logger.LogInformation("Sent request {RequestId} to Unity: {Method}", requestId, method);
        }
        catch (Exception ex)
        {
            _pendingRequests.TryRemove(requestId, out _);
            _logger.LogError(ex, "Error sending request to Unity");
            throw;
        }

        // Wait for response with timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        var timeoutTask = Task.Delay(Timeout.Infinite, cts.Token);
        var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

        if (completedTask == tcs.Task)
        {
            var result = await tcs.Task;
            if (result == null)
            {
                return default;
            }

            // Try to deserialize to target type
            var resultJson = JsonSerializer.Serialize(result, _jsonOptions);
            return JsonSerializer.Deserialize<T>(resultJson, _jsonOptions);
        }
        else
        {
            _pendingRequests.TryRemove(requestId, out _);
            throw new TimeoutException($"Request {requestId} to Unity timed out after {timeoutSeconds} seconds");
        }
    }
}
