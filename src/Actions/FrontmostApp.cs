namespace Loupedeck.ResolveSpeedDialPlugin
{
    using System;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    // Is DaVinci Resolve the frontmost application? The plugin is universal (so its actions live inside
    // Logitech's own DaVinci Resolve profile rather than creating a competing application), which means
    // the SDK's IsApplicationActive() no longer answers this, so we ask the OS directly.
    internal static class FrontmostApp
    {
        public const String ResolveBundleId = "com.blackmagic-design.DaVinciResolve";
        public const String ResolveProcessName = "Resolve";

        private static Boolean _warned;

        public static Boolean IsResolve()
        {
            try
            {
                if (OperatingSystem.IsMacOS())
                {
                    return String.Equals(MacFrontmostBundleId(), ResolveBundleId, StringComparison.Ordinal);
                }

                if (OperatingSystem.IsWindows())
                {
                    return String.Equals(WindowsForegroundProcessName(), ResolveProcessName, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (Exception ex)
            {
                if (!_warned)
                {
                    _warned = true;
                    PluginLog.Warning(ex, "Frontmost application check failed; keystrokes will be blocked");
                }
            }

            return false;
        }

        // --- macOS: [[[NSWorkspace sharedWorkspace] frontmostApplication] bundleIdentifier] ---

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr objc_getClass(String name);

        [DllImport("/usr/lib/libobjc.dylib")]
        private static extern IntPtr sel_registerName(String name);

        [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        private static IntPtr _nsWorkspace;
        private static IntPtr _selShared;
        private static IntPtr _selFrontmost;
        private static IntPtr _selBundleId;
        private static IntPtr _selUtf8;

        private static String MacFrontmostBundleId()
        {
            if (_nsWorkspace == IntPtr.Zero)
            {
                NativeLibrary.Load("/System/Library/Frameworks/AppKit.framework/AppKit");
                _nsWorkspace = objc_getClass("NSWorkspace");
                _selShared = sel_registerName("sharedWorkspace");
                _selFrontmost = sel_registerName("frontmostApplication");
                _selBundleId = sel_registerName("bundleIdentifier");
                _selUtf8 = sel_registerName("UTF8String");
            }

            var workspace = objc_msgSend(_nsWorkspace, _selShared);
            var app = workspace == IntPtr.Zero ? IntPtr.Zero : objc_msgSend(workspace, _selFrontmost);
            var bundleId = app == IntPtr.Zero ? IntPtr.Zero : objc_msgSend(app, _selBundleId);
            var utf8 = bundleId == IntPtr.Zero ? IntPtr.Zero : objc_msgSend(bundleId, _selUtf8);
            return utf8 == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(utf8);
        }

        // --- Windows: process name of the foreground window ---

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern UInt32 GetWindowThreadProcessId(IntPtr hWnd, out UInt32 processId);

        private static String WindowsForegroundProcessName()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero)
            {
                return null;
            }

            GetWindowThreadProcessId(hwnd, out var pid);
            using var process = Process.GetProcessById((Int32)pid);
            return process.ProcessName;
        }
    }
}
