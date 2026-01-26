using System;
using System.Runtime.InteropServices;

namespace UnityMCPSharp
{
    /// <summary>
    /// Platform-specific helper to bring Unity Editor window to the foreground.
    /// Uses P/Invoke calls that can run from any thread (including WebSocket background thread).
    /// This enables Unity to receive focus even when its main thread is throttled due to being unfocused.
    /// </summary>
    public static class WindowFocusHelper
    {
#if UNITY_EDITOR_WIN
        // Windows API imports
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private const int SW_RESTORE = 9;
        private const int SW_SHOW = 5;
#endif

#if UNITY_EDITOR_OSX
        // macOS Objective-C runtime imports
        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
        private static extern IntPtr objc_getClass(string className);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
        private static extern IntPtr sel_registerName(string selectorName);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool arg1);
#endif

        /// <summary>
        /// Brings Unity Editor window to the foreground using platform-specific APIs.
        /// This method is thread-safe and can be called from any thread, including the WebSocket background thread.
        /// </summary>
        /// <returns>True if the operation was attempted successfully, false if platform is not supported or an error occurred</returns>
        public static bool BringToForeground()
        {
            try
            {
#if UNITY_EDITOR_WIN
                return BringToForegroundWindows();
#elif UNITY_EDITOR_OSX
                return BringToForegroundMacOS();
#else
                MCPLogger.LogWarning("[WindowFocusHelper] Platform not supported for auto-focus");
                return false;
#endif
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[WindowFocusHelper] Error bringing Unity to foreground: {ex.Message}");
                return false;
            }
        }

#if UNITY_EDITOR_WIN
        private static bool BringToForegroundWindows()
        {
            try
            {
                // Get the main window handle of the current process (Unity Editor)
                var process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr unityWindow = process.MainWindowHandle;

                if (unityWindow == IntPtr.Zero)
                {
                    MCPLogger.LogWarning("[WindowFocusHelper] Could not get Unity window handle");
                    return false;
                }

                // If window is minimized, restore it first
                if (IsIconic(unityWindow))
                {
                    ShowWindow(unityWindow, SW_RESTORE);
                }

                // Get the foreground window's thread
                IntPtr foregroundWindow = GetForegroundWindow();
                uint foregroundThreadId = GetWindowThreadProcessId(foregroundWindow, out _);
                uint currentThreadId = GetCurrentThreadId();

                // Attach input threads to allow SetForegroundWindow to work
                // (Windows requires this when calling from a non-foreground application)
                if (foregroundThreadId != currentThreadId)
                {
                    AttachThreadInput(currentThreadId, foregroundThreadId, true);
                    SetForegroundWindow(unityWindow);
                    AttachThreadInput(currentThreadId, foregroundThreadId, false);
                }
                else
                {
                    SetForegroundWindow(unityWindow);
                }

                MCPLogger.LogVerbose("[WindowFocusHelper] Unity Editor brought to foreground (Windows)");
                return true;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[WindowFocusHelper] Windows focus error: {ex.Message}");
                return false;
            }
        }
#endif

#if UNITY_EDITOR_OSX
        private static bool BringToForegroundMacOS()
        {
            try
            {
                // Get NSApplication class
                IntPtr nsAppClass = objc_getClass("NSApplication");
                if (nsAppClass == IntPtr.Zero)
                {
                    MCPLogger.LogWarning("[WindowFocusHelper] Could not get NSApplication class");
                    return false;
                }

                // Get shared application instance
                IntPtr sharedAppSel = sel_registerName("sharedApplication");
                IntPtr sharedApp = objc_msgSend(nsAppClass, sharedAppSel);
                if (sharedApp == IntPtr.Zero)
                {
                    MCPLogger.LogWarning("[WindowFocusHelper] Could not get shared NSApplication");
                    return false;
                }

                // Call activateIgnoringOtherApps:YES to bring to foreground
                IntPtr activateSel = sel_registerName("activateIgnoringOtherApps:");
                objc_msgSend_bool(sharedApp, activateSel, true);

                MCPLogger.LogVerbose("[WindowFocusHelper] Unity Editor brought to foreground (macOS)");
                return true;
            }
            catch (Exception ex)
            {
                MCPLogger.LogError($"[WindowFocusHelper] macOS focus error: {ex.Message}");
                return false;
            }
        }
#endif
    }
}
