using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LiveWallpaperApp.Native
{
    public class ClickThroughHook : NativeWindow
    {
        private const int WM_NCHITTEST = 0x0084;
        private const int HTTRANSPARENT = -1;

        public ClickThroughHook(IntPtr handle)
        {
            AssignHandle(handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCHITTEST)
            {
                m.Result = new IntPtr(HTTRANSPARENT);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
