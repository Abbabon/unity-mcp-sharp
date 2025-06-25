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
            Console.WriteLine("[MenuItemsResource] GetMenuItems method called");
            
            try
            {
                Console.WriteLine("[MenuItemsResource] Sending request to Unity Bridge...");
                // Get the response from Unity
                var response = await UnityBridgeClient.Instance.SendRequestAsync("get_menu_items", new { });
                Console.WriteLine("[MenuItemsResource] Received response from Unity Bridge");
                
                // IMMEDIATELY convert the entire JsonElement to a string to preserve the data
                string responseJson = response.GetRawText();
                Console.WriteLine($"[MenuItemsResource] Raw JSON response: {responseJson}");
                
                // Re-parse the JSON from the string to ensure we're working with a fresh JsonDocument
                Console.WriteLine("[MenuItemsResource] Creating fresh JsonDocument from response string");
                using (JsonDocument doc = JsonDocument.Parse(responseJson))
                {
                    var root = doc.RootElement;
                    Console.WriteLine("[MenuItemsResource] Successfully parsed JSON document");
                    
                    // Process the response with the fresh JsonDocument
                    bool success = false;
                    string? message = null;
                    string menuItemsJson = "[]";
                    
                    Console.WriteLine("[MenuItemsResource] Extracting 'success' property");
                    if (root.TryGetProperty("success", out var successProp))
                    {
                        success = successProp.GetBoolean();
                        Console.WriteLine($"[MenuItemsResource] Success property value: {success}");
                    }
                    else
                    {
                        Console.WriteLine("[MenuItemsResource] Warning: 'success' property not found in response");
                    }
                    
                    Console.WriteLine("[MenuItemsResource] Extracting 'message' property");
                    if (root.TryGetProperty("message", out var msgProp))
                    {
                        message = msgProp.GetString();
                        Console.WriteLine($"[MenuItemsResource] Message property value: {message}");
                    }
                    else
                    {
                        Console.WriteLine("[MenuItemsResource] Warning: 'message' property not found in response");
                    }
                    
                    Console.WriteLine("[MenuItemsResource] Extracting 'menuItems' property");
                    if (root.TryGetProperty("menuItems", out var itemsProp) && 
                        itemsProp.ValueKind != JsonValueKind.Undefined)
                    {
                        menuItemsJson = itemsProp.GetRawText();
                        Console.WriteLine($"[MenuItemsResource] Found menuItems (length: {menuItemsJson.Length})");
                        Console.WriteLine($"[MenuItemsResource] Sample of menuItems: {(menuItemsJson.Length > 100 ? menuItemsJson.Substring(0, 100) + "..." : menuItemsJson)}");
                    }
                    else
                    {
                        Console.WriteLine("[MenuItemsResource] Warning: 'menuItems' property not found or undefined");
                    }
                    
                    // Check the success status before returning
                    if (!success)
                    {
                        Console.WriteLine($"[MenuItemsResource] Error: Request was not successful. Message: {message ?? "No message provided"}");
                        throw new Exception(message ?? "Failed to fetch menu items from Unity");
                    }
                    
                    // Return the JSON directly
                    Console.WriteLine("[MenuItemsResource] Successfully processed response, returning menu items JSON");
                    return menuItemsJson;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine($"[MenuItemsResource] ERROR in GetMenuItems: {ex.Message}");
                Console.Error.WriteLine($"[MenuItemsResource] Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"[MenuItemsResource] Inner exception: {ex.InnerException.Message}");
                    Console.Error.WriteLine($"[MenuItemsResource] Inner stack trace: {ex.InnerException.StackTrace}");
                }
                Console.ResetColor();
                throw;
            }
        }
    }
}
