using System.Runtime.InteropServices;
using System.Text;

namespace LiveWallpaperApp.Native;

public static class Win32
{
    public const int GwlStyle = -16;
    public const int GwlExStyle = -20;
    public const int WsChild = 0x40000000;
    public const int WsPopup = unchecked((int)0x80000000);
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

    private static readonly string LogPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "LiveWallpaperApp", "debug.log");

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

    [DllImport("user32.dll")]
    public static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

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

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool IsWindowVisible(IntPtr hWnd);

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

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }

    /// <summary>
    /// Locates the hidden WorkerW surface used by Explorer to compose the desktop.
    /// Uses the classic single-pass algorithm: find the top-level window containing
    /// SHELLDLL_DefView, then get the next WorkerW sibling after it.
    /// </summary>
    public static (IntPtr DesktopHost, IntPtr ShellView) EnsureWorkerW()
    {
        Log("EnsureWorkerW: starting");

        var progman = FindWindow("Progman", null);
        Log($"EnsureWorkerW: Progman = 0x{progman:X}");

        if (progman == IntPtr.Zero)
        {
            throw new InvalidOperationException("Unable to locate the Progman desktop window.");
        }

        // Ask Explorer to spawn the WorkerW pair.
        // The standard 0,0 message directs Progman to spawn a WorkerW behind the desktop icons.
        SendMessageTimeout(progman, WorkerWSpawnMessage, IntPtr.Zero, IntPtr.Zero,
            SmtoNormal | SmtoAbortIfHung, 1000, out _);

        System.Threading.Thread.Sleep(200);

        IntPtr workerW = IntPtr.Zero;
        IntPtr shellView = IntPtr.Zero;

        // Find SHELLDLL_DefView and get the WorkerW sibling if the desktop split succeeded
        EnumWindows((topHandle, _) =>
        {
            var view = FindWindowEx(topHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (view != IntPtr.Zero)
            {
                shellView = view;
                // If 0x052C succeeded, there is a WorkerW sibling immediately after this topHandle.
                workerW = FindWindowEx(IntPtr.Zero, topHandle, "WorkerW", null);
                
                var sb = new StringBuilder(256);
                GetClassName(topHandle, sb, sb.Capacity);
                Log($"EnsureWorkerW: found SHELLDLL_DefView=0x{view:X} in parent=0x{topHandle:X} (class={sb}), workerW sibling=0x{workerW:X}");
                return false;
            }
            return true;
        }, IntPtr.Zero);

        // Fallback: if we can't get WorkerW, attach directly to Progman.
        var desktopHost = workerW != IntPtr.Zero ? workerW : progman;
        Log($"EnsureWorkerW: desktopHost = 0x{desktopHost:X} (fallback? {workerW == IntPtr.Zero})");
        return (desktopHost, shellView);
    }

    public static void ConfigureWallpaperChild(IntPtr childHandle, IntPtr desktopHost, IntPtr shellView, int x, int y, int width, int height)
    {
        Log($"ConfigureWallpaperChild: child=0x{childHandle:X}, host=0x{desktopHost:X}, shell=0x{shellView:X}, pos=({x},{y},{width},{height})");

        if (childHandle == IntPtr.Zero)
        {
            throw new ArgumentException("Wallpaper window handle is invalid.", nameof(childHandle));
        }

        if (desktopHost == IntPtr.Zero)
        {
            throw new ArgumentException("Desktop host handle is invalid.", nameof(desktopHost));
        }

        // Make it a child window
        var style = GetWindowLongPtr(childHandle, GwlStyle).ToInt64();
        style = (style | WsChild | WsVisible | WsClipChildren | WsClipSiblings) & ~WsPopup;
        SetWindowLongPtr(childHandle, GwlStyle, new IntPtr(style));

        // Make it click-through and non-activatable (but NOT transparent to rendering)
        var exStyle = GetWindowLongPtr(childHandle, GwlExStyle).ToInt64();
        exStyle |= WsExToolWindow | WsExNoActivate;
        SetWindowLongPtr(childHandle, GwlExStyle, new IntPtr(exStyle));

        // Reparent into WorkerW
        var prevParent = SetParent(childHandle, desktopHost);
        Log($"ConfigureWallpaperChild: SetParent returned 0x{prevParent:X}");

        // Map screen coordinates to WorkerW/Progman client coordinates for multi-monitor
        var rect = new Rect { Left = x, Top = y, Right = x + width, Bottom = y + height };
        MapWindowPoints(IntPtr.Zero, desktopHost, ref rect, 2);
        Log($"ConfigureWallpaperChild: mapped rect=({rect.Left},{rect.Top},{rect.Width},{rect.Height})");

        MoveWindow(childHandle, rect.Left, rect.Top, width, height, true);

        // Instead of pushing to the absolute bottom (which goes behind the DComp background),
        // we insert the video precisely BEHIND SHELLDLL_DefView (the icons) in the Z-order.
        if (desktopHost == FindWindow("Progman", null) || desktopHost == shellView)
        {
            IntPtr insertAfter = new IntPtr(1); // Default to HWND_BOTTOM
            
            if (desktopHost == shellView)
            {
                IntPtr sysListView = FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
                if (sysListView != IntPtr.Zero) insertAfter = sysListView;
            }
            else if (desktopHost == FindWindow("Progman", null) && shellView != IntPtr.Zero)
            {
                // If attached to Progman, insert immediately behind SHELLDLL_DefView
                insertAfter = shellView;
            }
            
            SetWindowPos(childHandle, insertAfter, rect.Left, rect.Top, width, height, SwpNoActivate | SwpFrameChanged);
            Log($"ConfigureWallpaperChild: inserted after 0x{insertAfter:X}");
        }
        
        Log("ConfigureWallpaperChild: done");
    }

    [DllImport("user32.dll")]
    public static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

    public static void MakeWindowAndChildrenTransparent(IntPtr hwnd)
    {
        void MakeTransparent(IntPtr handle)
        {
            var exStyle = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
            exStyle |= WsExTransparent | WsExNoActivate;
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(exStyle));
            EnableWindow(handle, false);
        }

        MakeTransparent(hwnd);
        EnumChildWindows(hwnd, (child, _) =>
        {
            MakeTransparent(child);
            return true;
        }, IntPtr.Zero);
    }

    public static TimeSpan GetIdleTime()
    {
        var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };
        return GetLastInputInfo(ref info)
            ? TimeSpan.FromMilliseconds((uint)Environment.TickCount - info.Time)
            : TimeSpan.Zero;
    }
}
