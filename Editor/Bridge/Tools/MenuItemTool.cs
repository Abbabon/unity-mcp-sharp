using Editor.Bridge.Models;
using Editor.Bridge.Services;
using Editor.Utils;
using Newtonsoft.Json.Linq;
using UnityEditor;

namespace Editor.Bridge.Tools
{
    /// <summary>
    /// Tool for executing Unity Editor menu items
    /// </summary>
    public class MenuItemTool : McpToolBase
    {
        public MenuItemTool()
        {
            Name = "execute_menu_item";
            Description = "Executes functions tagged with the MenuItem attribute";
        }
        
        /// <summary>
        /// Execute the MenuItem tool with the provided parameters synchronously
        /// </summary>
        /// <param name="parameters">Tool parameters as a JObject</param>
        public override JObject Execute(JObject parameters)
        {
            // Extract parameters with defaults
            string menuPath = parameters["menuPath"]?.ToObject<string>();
            if (string.IsNullOrEmpty(menuPath))
            {
                return UnityBridgeSocketHandler.CreateErrorResponse(
                    "Required parameter 'menuPath' not provided", 
                    "validation_error"
                );
            }
                
            // Log the execution
            UnityMcpSharpLogger.LogInfo($"[MCP Unity] Executing menu item: {menuPath}");
                
            // Execute the menu item
            bool success = EditorApplication.ExecuteMenuItem(menuPath);
                
            // Create the response
            return new JObject
            {
                ["success"] = success,
                ["type"] = "text",
                ["message"] = success 
                    ? $"Successfully executed menu item: {menuPath}" 
                    : $"Failed to execute menu item: {menuPath}"
            };
        }
    }
}