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
            var response = await UnityBridgeClient.Instance.SendRequestAsync("get_menu_items", new { });
            
            var success = response.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
            var message = response.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
            if (!success)
                throw new Exception(message ?? "Failed to fetch menu items from Unity");
            var menuItems = response.TryGetProperty("menuItems", out var itemsProp) ? itemsProp : default;
            return menuItems.ValueKind == JsonValueKind.Undefined ? "[]" : menuItems.ToString();
        }
    }
} 