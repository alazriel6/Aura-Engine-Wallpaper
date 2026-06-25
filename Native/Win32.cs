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

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    public static extern int GetWindowTextLength(IntPtr hWnd);

    /// <summary>
    /// Finds any stray "VLC (Direct3D11 output)" top-level windows from our process
    /// and hides them from the taskbar + screen. VLC's D3D11 vout creates these 
    /// behind our back, causing a giant popup flash.
    /// </summary>
    /// <summary>
    /// Returns a set of all current top-level VLC window handles in our process.
    /// Call BEFORE Play() to snapshot existing VLC windows.
    /// </summary>
    public static HashSet<IntPtr> GetVlcWindowHandles()
    {
        var result = new HashSet<IntPtr>();
        var ourPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        EnumWindows((hWnd, _) =>
        {
            GetWindowThreadProcessId(hWnd, out uint windowPid);
            if (windowPid != ourPid) return true;

            int len = GetWindowTextLength(hWnd);
            if (len <= 0) return true;

            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString().Contains("VLC", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(hWnd);
            }
            return true;
        }, IntPtr.Zero);

        return result;
    }

    /// <summary>
    /// Suppresses VLC windows that were NOT in the 'before' snapshot.
    /// Only hides from taskbar and moves offscreen — does NOT SW_HIDE (which would freeze VLC).
    /// </summary>
    public static void SuppressNewVlcWindows(HashSet<IntPtr> existingHandles)
    {
        var ourPid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id;

        EnumWindows((hWnd, _) =>
        {
            // Skip windows that existed before Play()
            if (existingHandles.Contains(hWnd)) return true;

            GetWindowThreadProcessId(hWnd, out uint windowPid);
            if (windowPid != ourPid) return true;

            int len = GetWindowTextLength(hWnd);
            if (len <= 0) return true;

            var sb = new StringBuilder(len + 1);
            GetWindowText(hWnd, sb, sb.Capacity);

            if (sb.ToString().Contains("VLC", StringComparison.OrdinalIgnoreCase))
            {
                // Remove from taskbar
                var exStyle = GetWindowLongPtr(hWnd, GwlExStyle).ToInt64();
                exStyle |= WsExToolWindow | WsExNoActivate;
                exStyle &= ~0x00040000L; // ~WS_EX_APPWINDOW
                SetWindowLongPtr(hWnd, GwlExStyle, new IntPtr(exStyle));

                // Move offscreen — VLC needs the window alive for D3D11 rendering
                MoveWindow(hWnd, -32000, -32000, 1, 1, false);
            }
            return true;
        }, IntPtr.Zero);
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DISPLAY_DEVICE
    {
        [MarshalAs(UnmanagedType.U4)]
        public int cb;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceString;
        [MarshalAs(UnmanagedType.U4)]
        public int StateFlags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string DeviceKey;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;

        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public const int ENUM_CURRENT_SETTINGS = -1;
    public const int ENUM_REGISTRY_SETTINGS = -2;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplayDevices(string? lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern bool EnumDisplaySettings(string? lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);
}
