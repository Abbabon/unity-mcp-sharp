namespace UnityMcpServer.Services;

/// <summary>
/// Provides tool profile definitions for filtering MCP tools based on user preferences.
/// Profiles allow users to reduce token usage by exposing only the tools they need.
/// </summary>
public static class ToolProfileService
{
    /// <summary>
    /// Minimal profile - 12 core tools for basic workflows (~3k tokens)
    /// </summary>
    private static readonly HashSet<string> MinimalTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "unity_get_project_info",
        "unity_list_scene_objects",
        "unity_find_game_object",
        "unity_create_game_object",
        "unity_delete_game_object",
        "unity_create_script",
        "unity_get_compilation_status",
        "unity_get_console_logs",
        "unity_enter_play_mode",
        "unity_exit_play_mode",
        "unity_open_scene",
        "unity_save_scene"
    };

    /// <summary>
    /// Standard profile - 20 commonly used tools (~5k tokens)
    /// Includes all minimal tools plus additional commonly used ones
    /// </summary>
    private static readonly HashSet<string> StandardTools = new(MinimalTools, StringComparer.OrdinalIgnoreCase)
    {
        "unity_add_component_to_object",
        "unity_set_component_field",
        "unity_get_active_scene",
        "unity_list_scenes",
        "unity_get_play_mode_state",
        "unity_refresh_assets",
        "unity_trigger_script_compilation",
        "unity_create_asset"
    };

    /// <summary>
    /// Full profile - All 28 tools including advanced/multi-editor features (~7k tokens)
    /// </summary>
    private static readonly HashSet<string> FullTools = new(StandardTools, StringComparer.OrdinalIgnoreCase)
    {
        "unity_batch_create_game_objects",
        "unity_create_game_object_in_scene",
        "unity_close_scene",
        "unity_set_active_scene",
        "unity_run_menu_item",
        "unity_bring_editor_to_foreground",
        "unity_list_editors",
        "unity_select_editor"
    };

    /// <summary>
    /// Get the set of enabled tools for a given profile name.
    /// </summary>
    /// <param name="profileName">Profile name: "minimal", "standard", or "full"</param>
    /// <returns>HashSet of enabled tool names, or null to allow all tools</returns>
    public static HashSet<string>? GetToolsForProfile(string? profileName)
    {
        return profileName?.ToLowerInvariant() switch
        {
            "minimal" => MinimalTools,
            "standard" => StandardTools,
            "full" => null, // null means no filtering - all tools enabled
            _ => StandardTools // Default to standard if unknown
        };
    }

    /// <summary>
    /// Check if a specific tool is enabled for a given profile.
    /// </summary>
    public static bool IsToolEnabled(string toolName, string? profileName)
    {
        var tools = GetToolsForProfile(profileName);
        return tools == null || tools.Contains(toolName);
    }

    /// <summary>
    /// Get the count of tools in each profile.
    /// </summary>
    public static (int minimal, int standard, int full) GetProfileCounts()
    {
        return (MinimalTools.Count, StandardTools.Count, FullTools.Count);
    }
}
