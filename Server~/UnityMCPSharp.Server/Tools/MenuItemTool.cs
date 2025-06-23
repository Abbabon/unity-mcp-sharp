using System.ComponentModel;
using ModelContextProtocol.Server;

namespace UnityMCPSharp.Server.Tools
{
    [McpServerToolType]
    public static class MenuItemTool
    {
        [McpServerTool(Name = "ExecuteMenuItem"), Description("Executes a Unity menu item by path.")]
        public static async Task<string> ExecuteMenuItem(string menuPath)
        {
            var response = await UnityBridgeClient.Instance.SendRequestAsync("execute_menu_item", new { menuPath });
            
            var success = response.TryGetProperty("success", out var successProp) && successProp.GetBoolean();
            var message = response.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : null;
            if (!success)
                throw new Exception(message ?? $"Failed to execute menu item: {menuPath}");
            return message ?? $"Successfully executed menu item: {menuPath}";
        }
    }
}