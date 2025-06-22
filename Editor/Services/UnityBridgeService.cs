using UnityEngine;
using UnityEditor;
using System;
using WebSocketSharp;

namespace UnityMcpSharp.Editor.Services
{
    /// <summary>
    /// Service responsible for communicating with the MCP server
    /// </summary>
    public class UnityBridgeService
    {
        private WebSocket _webSocket;
        private bool _isConnected = false;
        
        private static UnityBridgeService _instance;
        public static UnityBridgeService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new UnityBridgeService();
                }
                return _instance;
            }
        }
        
        public bool IsConnected => _isConnected;
        
        private UnityBridgeService()
        {
            // Initialize the service
            EditorApplication.update += Update;
        }
        
        ~UnityBridgeService()
        {
            EditorApplication.update -= Update;
        }
        
        public void Connect()
        {
            try
            {
                // Default MCP server WebSocket URL
                string serverUrl = "ws://localhost:3000";
                
                _webSocket = new WebSocket(serverUrl);
                
                _webSocket.OnOpen += (sender, e) =>
                {
                    _isConnected = true;
                    Debug.Log("Connected to MCP server");
                };
                
                _webSocket.OnClose += (sender, e) =>
                {
                    _isConnected = false;
                    Debug.Log("Disconnected from MCP server");
                };
                
                _webSocket.OnError += (sender, e) =>
                {
                    _isConnected = false;
                    Debug.LogError($"WebSocket error: {e.Message}");
                };
                
                _webSocket.Connect();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to MCP server: {ex.Message}");
                _isConnected = false;
            }
        }
        
        public void Disconnect()
        {
            if (_webSocket != null && _webSocket.IsAlive)
            {
                _webSocket.Close();
            }
            _isConnected = false;
        }
        
        private void Update()
        {
            // For now, always show as offline
            _isConnected = false;
        }
    }
}
