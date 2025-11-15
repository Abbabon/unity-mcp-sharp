using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;

namespace UnityMCPSharp.Editor
{
    /// <summary>
    /// Tracks MCP operations for visual feedback and logging.
    /// Maintains both recent operations (for UI display) and all operations (for persistent log).
    /// </summary>
    public static class MCPOperationTracker
    {
        [Serializable]
        public class OperationInfo
        {
            public string Operation { get; set; }
            public DateTime Timestamp { get; set; }
            public string Status { get; set; } // "in_progress", "completed", "failed"
            public string Parameters { get; set; } // JSON string of input parameters
        }

        [Serializable]
        private class OperationLogWrapper
        {
            public List<OperationInfo> Operations { get; set; }
        }

        private static List<OperationInfo> _recentOperations = new List<OperationInfo>();
        private static List<OperationInfo> _allOperations = new List<OperationInfo>();
        private static string _currentOperation = null;
        private static string _logFilePath = null;

        public static List<OperationInfo> RecentOperations => _recentOperations;
        public static List<OperationInfo> AllOperations => _allOperations;
        public static string CurrentOperation => _currentOperation;
        public static bool IsOperationInProgress => !string.IsNullOrEmpty(_currentOperation);

        /// <summary>
        /// Initialize the operation tracker with log file path.
        /// Call this once during MCPEditorIntegration initialization.
        /// </summary>
        public static void Initialize()
        {
            _logFilePath = Path.Combine(Application.dataPath, "..", "Temp", "MCPOperations.log");
            LoadOperationsLog();
        }

        /// <summary>
        /// Start tracking a new operation.
        /// </summary>
        public static void StartOperation(string operationName, int maxLogEntries, bool verboseLogging, object parameters = null)
        {
            _currentOperation = operationName;
            var op = new OperationInfo
            {
                Operation = operationName,
                Timestamp = DateTime.Now,
                Status = "in_progress",
                Parameters = parameters != null ? JsonConvert.SerializeObject(parameters, Formatting.None) : null
            };
            _recentOperations.Insert(0, op);
            _allOperations.Insert(0, op);

            // Trim recent operations to max entries
            if (_recentOperations.Count > maxLogEntries)
            {
                _recentOperations.RemoveAt(_recentOperations.Count - 1);
            }

            // Save to persistent log
            SaveOperationsLog();

            if (verboseLogging)
            {
                Debug.Log($"[MCP Operation] Started: {operationName}");
            }
        }

        /// <summary>
        /// Mark the current operation as completed or failed.
        /// </summary>
        public static void CompleteOperation(bool success, bool verboseLogging)
        {
            if (!string.IsNullOrEmpty(_currentOperation) && _recentOperations.Count > 0)
            {
                _recentOperations[0].Status = success ? "completed" : "failed";
                if (_allOperations.Count > 0)
                {
                    _allOperations[0].Status = success ? "completed" : "failed";
                }

                // Save to persistent log
                SaveOperationsLog();

                if (verboseLogging)
                {
                    Debug.Log($"[MCP Operation] {(success ? "Completed" : "Failed")}: {_currentOperation}");
                }
            }
            _currentOperation = null;
        }

        /// <summary>
        /// Load operations from persistent log file.
        /// </summary>
        private static void LoadOperationsLog()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    string json = File.ReadAllText(_logFilePath);
                    var wrapper = JsonConvert.DeserializeObject<OperationLogWrapper>(json);
                    if (wrapper != null && wrapper.Operations != null)
                    {
                        _allOperations = wrapper.Operations;
                        // Load recent operations (last N entries) - we'll use a default max of 50
                        _recentOperations = _allOperations.Take(50).ToList();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPOperationTracker] Error loading operations log: {ex.Message}");
            }
        }

        /// <summary>
        /// Save operations to persistent log file.
        /// </summary>
        private static void SaveOperationsLog()
        {
            try
            {
                var wrapper = new OperationLogWrapper { Operations = _allOperations };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);
                File.WriteAllText(_logFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MCPOperationTracker] Error saving operations log: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all operations from memory and persistent log.
        /// </summary>
        public static void ClearOperationsLog()
        {
            _allOperations.Clear();
            _recentOperations.Clear();
            SaveOperationsLog();
        }
    }
}
