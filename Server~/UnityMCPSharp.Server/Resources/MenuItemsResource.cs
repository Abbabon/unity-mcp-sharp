using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace UnityMCPSharp.Server.Resources
{
    [McpServerResourceType]
    public static class MenuItemsResource
    {
        [McpServerResource(Name = "GetMenuItems", MimeType = "application/json", UriTemplate = "unity://menu-items"), Description("List of available menu items in Unity to execute")]
        public static async Task<string> GetMenuItems()
        {
            // Get the response from Unity
            var response = await UnityBridgeClient.Instance.SendRequestAsync("get_menu_items", new { });
                
            // Immediately convert the entire JsonElement to a string to preserve the data
            string responseJson = response.GetRawText();
                
            // Re-parse the JSON from the string to ensure we're working with a fresh JsonDocument
            using (JsonDocument doc = JsonDocument.Parse(responseJson))
            {
                var root = doc.RootElement;
                    
                // Process the response with the fresh JsonDocument
                bool success = false;
                string? message = null;
                string menuItemsJson = "[]";
                    
                if (root.TryGetProperty("success", out var successProp))
                {
                    success = successProp.GetBoolean();
                }
                    
                if (root.TryGetProperty("message", out var msgProp))
                {
                    message = msgProp.GetString();
                }
                    
                if (root.TryGetProperty("menuItems", out var itemsProp) && 
                    itemsProp.ValueKind != JsonValueKind.Undefined)
                {
                    menuItemsJson = itemsProp.GetRawText();
                }
                    
                // Check the success status before returning
                if (!success)
                {
                    throw new Exception(message ?? "Failed to fetch menu items from Unity");
                }
                    
                // Return the JSON directly
                return menuItemsJson;
            }
        }
    }
}
