using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using UnityMCPSharp.Editor.Models;

namespace UnityMCPSharp.Editor.Handlers.Assets
{
    /// <summary>
    /// Handles requests to create a new C# script file.
    /// </summary>
    public static class CreateScriptHandler
    {
        public static void Handle(object parameters, MCPConfiguration config)
        {
            try
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(parameters);
                var data = Newtonsoft.Json.JsonConvert.DeserializeObject<CreateScriptData>(json);

                MCPOperationTracker.StartOperation("Create Script", config.maxOperationLogEntries, config.verboseLogging, data);

                // Ensure folder path exists
                var fullFolderPath = Path.Combine(Application.dataPath, data.folderPath);
                if (!Directory.Exists(fullFolderPath))
                {
                    Directory.CreateDirectory(fullFolderPath);
                }

                // Create script file
                var scriptFileName = $"{data.scriptName}.cs";
                var scriptPath = Path.Combine(fullFolderPath, scriptFileName);

                File.WriteAllText(scriptPath, data.scriptContent);

                // Refresh asset database to trigger compilation
                AssetDatabase.Refresh();

                Debug.Log($"[CreateScriptHandler] Created script: {scriptPath}");
                MCPOperationTracker.CompleteOperation(true, config.verboseLogging);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CreateScriptHandler] Error creating script: {ex.Message}");
                MCPOperationTracker.CompleteOperation(false, config.verboseLogging);
            }
        }
    }
}
