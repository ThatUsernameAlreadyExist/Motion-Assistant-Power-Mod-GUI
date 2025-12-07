using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Controls;


namespace PmGui.Helpers
{
    public static class NativeMethods
    {
        #region User32.dll Imports

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern bool LockSetForegroundWindow(uint uLockCode);

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int dwProcessId);

        [DllImport("user32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        #endregion

        #region Kernel32.dll Imports

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        #endregion

        #region Constants

        // ShowWindow commands
        public const int SW_HIDE = 0;
        public const int SW_SHOWNORMAL = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_SHOW = 5;
        public const int SW_MINIMIZE = 6;
        public const int SW_RESTORE = 9;

        // SetWindowPos flags
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint SWP_SHOWWINDOW = 0x0040;
        public const uint SWP_ASYNCWINDOWPOS = 0x4000;

        // SetWindowPos hWndInsertAfter
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static readonly IntPtr HWND_TOP = new IntPtr(0);

        // Virtual key codes
        public const byte VK_MENU = 0x12;        // Alt key
        public const byte VK_LMENU = 0xA4;       // Left Alt
        public const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        public const uint KEYEVENTF_KEYUP = 0x0002;

        // LockSetForegroundWindow
        public const uint LSFW_LOCK = 1;
        public const uint LSFW_UNLOCK = 2;

        // AllowSetForegroundWindow
        public const int ASFW_ANY = -1;

        // GetWindowLong
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOPMOST = 0x00000008;

        // FlashWindowEx
        public const uint FLASHW_STOP = 0;
        public const uint FLASHW_CAPTION = 1;
        public const uint FLASHW_TRAY = 2;
        public const uint FLASHW_ALL = 3;
        public const uint FLASHW_TIMER = 4;
        public const uint FLASHW_TIMERNOFG = 12;

        #endregion

        #region Structs

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        #endregion
    }
    public static class WindowFocusHelper
    {
        /// <summary>
        /// Forces a window to the foreground using multiple Windows API techniques
        /// </summary>
        public static bool ForceForeground(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
                return false;

            bool isAttached = false;
            bool result = false;
            uint currentThreadId = 0;
            uint foregroundThreadId = 0;

            try
            {
                    // Check if already foreground
                IntPtr foregroundWindow = NativeMethods.GetForegroundWindow();
                if (foregroundWindow == hWnd)
                    return true;

                // Get thread information
                currentThreadId = NativeMethods.GetCurrentThreadId();
                foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
                uint targetThreadId = NativeMethods.GetWindowThreadProcessId(hWnd, out _);

                NativeMethods.LockSetForegroundWindow(NativeMethods.LSFW_UNLOCK);
                NativeMethods.AllowSetForegroundWindow(NativeMethods.ASFW_ANY);

                SimulateAltKeyPress();

                if (currentThreadId != foregroundThreadId && foregroundThreadId != 0)
                {
                    isAttached = NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
                }

                NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);

                result = NativeMethods.SetForegroundWindow(hWnd);
                NativeMethods.BringWindowToTop(hWnd);
                NativeMethods.ShowWindow(hWnd, NativeMethods.SW_SHOW);
                NativeMethods.SetFocus(hWnd);

                NativeMethods.SetWindowPos(hWnd, NativeMethods.HWND_NOTOPMOST,
                    0, 0, 0, 0,
                    NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_SHOWWINDOW);
            }
            finally
            {
                if (isAttached)
                {
                    NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
                }
            }

            return result;
        }

        /// <summary>
        /// Simulates Alt key press to bypass Windows foreground lock
        /// </summary>
        private static void SimulateAltKeyPress()
        {
            // Press Alt
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0,
                NativeMethods.KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);

            // Release Alt
            NativeMethods.keybd_event(NativeMethods.VK_MENU, 0,
                NativeMethods.KEYEVENTF_EXTENDEDKEY | NativeMethods.KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        /// <summary>
        /// Flashes the window in taskbar (fallback if foreground fails)
        /// </summary>
        public static void FlashWindow(IntPtr hWnd, uint count = 3)
        {
            var flashInfo = new NativeMethods.FLASHWINFO
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.FLASHWINFO>(),
                hwnd = hWnd,
                dwFlags = NativeMethods.FLASHW_ALL | NativeMethods.FLASHW_TIMERNOFG,
                uCount = count,
                dwTimeout = 0
            };
            NativeMethods.FlashWindowEx(ref flashInfo);
        }

        /// <summary>
        /// Stops window flashing
        /// </summary>
        public static void StopFlashWindow(IntPtr hWnd)
        {
            var flashInfo = new NativeMethods.FLASHWINFO
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.FLASHWINFO>(),
                hwnd = hWnd,
                dwFlags = NativeMethods.FLASHW_STOP,
                uCount = 0,
                dwTimeout = 0
            };
            NativeMethods.FlashWindowEx(ref flashInfo);
        }

        /// <summary>
        /// Gets the native window handle from an Avalonia Window
        /// </summary>
        public static IntPtr GetHandle(Window window)
        {
            if (window == null)
                return IntPtr.Zero;

            try
            {
                var platformHandle = window.TryGetPlatformHandle();
                if (platformHandle != null)
                {
                    return platformHandle.Handle;
                }
            }
            catch
            {
            }

            return IntPtr.Zero;
        }
    }
}