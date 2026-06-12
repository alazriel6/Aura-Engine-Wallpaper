using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace LiveWallpaperApp.Native;

public static class ShellThumbnailProvider
{
    [ComImport]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItem { }

    [ComImport]
    [Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        void GetImage([In] SIZE size, [In] int flags, [Out] out IntPtr phbm);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SIZE { public int cx; public int cy; }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
        [In] IntPtr pbc,
        [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [Out][MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr hObject);

    public static bool TryExtractThumbnail(string videoPath, string outputPath, int width = 512, int height = 512)
    {
        IntPtr hbitmap = IntPtr.Zero;
        try
        {
            var riid = new Guid("bcc18b79-ba16-442f-80c4-8a59c30c463b");
            SHCreateItemFromParsingName(videoPath, IntPtr.Zero, riid, out IShellItemImageFactory factory);
            
            // SIIGBF_MEMORYONLY = 0x02, SIIGBF_RESIZETOFIT = 0x00
            factory.GetImage(new SIZE { cx = width, cy = height }, 0x00, out hbitmap);
            
            if (hbitmap != IntPtr.Zero)
            {
                #pragma warning disable CA1416
                using var image = Image.FromHbitmap(hbitmap);
                image.Save(outputPath, ImageFormat.Jpeg);
                #pragma warning restore CA1416
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (hbitmap != IntPtr.Zero)
            {
                DeleteObject(hbitmap);
            }
        }
    }
}
