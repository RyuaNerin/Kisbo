using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Kisbo.Utilities
{
    public static class UacImage
    {
        private static Image Image;
        public static Image GetImage()
        {
            if (Image != null)
                return Image;

            var sii = new NativeMethods.SHSTOCKICONINFO();
            sii.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.SHSTOCKICONINFO));

            Marshal.ThrowExceptionForHR(NativeMethods.SHGetStockIconInfo(NativeMethods.SIID_SHIELD, NativeMethods.SHGSI_ICON | NativeMethods.SHGSI_SMALLICON, ref sii));

            return Image = Icon.FromHandle(sii.hIcon).ToBitmap();
        }

        private static class NativeMethods
        {
            [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            public struct SHSTOCKICONINFO
            {
                public UInt32 cbSize;
                public IntPtr hIcon;
                public Int32 iSysIconIndex;
                public Int32 iIcon;
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
                public string szPath;
            }

            [DllImport("Shell32.dll", SetLastError = false)]
            public static extern int SHGetStockIconInfo(uint siid, uint uFlags, ref SHSTOCKICONINFO psii);

            public const uint SIID_SHIELD = 77;
            public const uint SHGSI_ICON = 0x100;
            public const uint SHGSI_SMALLICON = 0x1;
        }
    }
}
