using System.Runtime.InteropServices;

namespace LiveWallpaperApp.Native;

public static class Win32
{
    public const int GwlStyle = -16;
    public const int GwlExStyle = -20;
    public const int WsChild = 0x40000000;
    public const int WsVisible = 0x10000000;
    public const int WsClipChildren = 0x02000000;
    public const int WsClipSiblings = 0x04000000;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExNoActivate = 0x08000000;
    public const int WsExTransparent = 0x00000020;
    public const int SwpNoZOrder = 0x0004;
    public const int SwpNoActivate = 0x0010;
    public const int SwpFrameChanged = 0x0020;
    public const uint SmtoNormal = 0x0000;
    public const uint SmtoAbortIfHung = 0x0002;
    public const uint WorkerWSpawnMessage = 0x052C;

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LastInputInfo
    {
        public uint Size;
        public uint Time;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SendMessageTimeout(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam,
        uint fuFlags,
        uint uTimeout,
        out IntPtr lpdwResult);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, ref Rect lpPoints, uint cPoints);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool MoveWindow(IntPtr hWnd, int x, int y, int nWidth, int nHeight, bool bRepaint);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int x,
        int y,
        int cx,
        int cy,
        uint uFlags);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetLastInputInfo(ref LastInputInfo plii);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
    private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
    private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
    {
        return IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : new IntPtr(GetWindowLong32(hWnd, nIndex));
    }

    public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        return IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, dwNewLong)
            : new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    /// <summary>
    /// Locates the hidden WorkerW surface used by Explorer to compose the desktop.
    ///
    /// Windows does not paint the desktop as one simple bitmap. Explorer owns a Progman
    /// window and a SHELLDLL_DefView child that hosts the desktop icon ListView. Sending
    /// message 0x052C to Progman asks Explorer to create an extra WorkerW window. That
    /// empty WorkerW sits behind SHELLDLL_DefView, so a child HWND placed there becomes
    /// a desktop background while icons, selection rectangles, and Explorer gestures
    /// remain on the icon layer.
    ///
    /// SetParent is used because VLC renders into an HWND. Re-parenting the WPF renderer
    /// window into WorkerW lets DWM compose the video in Explorer's desktop tree instead
    /// of treating it like a normal top-level application window.
    /// </summary>
    public static IntPtr EnsureWorkerW()
    {
        var progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to locate the Progman desktop window.");
        }

        SendMessageTimeout(
            progman,
            WorkerWSpawnMessage,
            IntPtr.Zero,
            IntPtr.Zero,
            SmtoNormal | SmtoAbortIfHung,
            1000,
            out _);

        var workerW = IntPtr.Zero;

        EnumWindows((topHandle, _) =>
        {
            var shellView = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (shellView == IntPtr.Zero)
            {
                return true;
            }

            workerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
            return workerW == IntPtr.Zero;
        }, IntPtr.Zero);

        return workerW != IntPtr.Zero ? workerW : progman;
    }

    public static void ConfigureWallpaperChild(IntPtr childHandle, IntPtr workerWHandle, int x, int y, int width, int height)
    {
        if (childHandle == IntPtr.Zero)
        {
            throw new ArgumentException("Wallpaper window handle is invalid.", nameof(childHandle));
        }

        if (workerWHandle == IntPtr.Zero)
        {
            throw new ArgumentException("WorkerW handle is invalid.", nameof(workerWHandle));
        }

        var style = GetWindowLongPtr(childHandle, GwlStyle).ToInt64();
        style |= WsChild | WsVisible | WsClipChildren | WsClipSiblings;
        SetWindowLongPtr(childHandle, GwlStyle, new IntPtr(style));

        var exStyle = GetWindowLongPtr(childHandle, GwlExStyle).ToInt64();
        exStyle |= WsExToolWindow | WsExNoActivate | WsExTransparent;
        SetWindowLongPtr(childHandle, GwlExStyle, new IntPtr(exStyle));

        var rect = new Rect { Left = x, Top = y, Right = x + width, Bottom = y + height };
        MapWindowPoints(IntPtr.Zero, workerWHandle, ref rect, 2);

        SetParent(childHandle, workerWHandle);
        MoveWindow(childHandle, rect.Left, rect.Top, width, height, true);
        SetWindowPos(childHandle, IntPtr.Zero, rect.Left, rect.Top, width, height, SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
    }

    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<LastInputInfo>()
        };

        if (!GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var idleTicks = unchecked(Environment.TickCount - (int)info.Time);
        return TimeSpan.FromMilliseconds(Math.Max(0, idleTicks));
    }
}
