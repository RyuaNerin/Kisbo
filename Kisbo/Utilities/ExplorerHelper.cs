using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Kisbo.Utilities
{
    public static class ExplorerHelper
    {
        [SuppressUnmanagedCodeSecurity]
        static class SafeNativeMethods
        {
            [DllImport("shell32.dll", ExactSpelling = true)]
            public static extern int SHOpenFolderAndSelectItems(
                IntPtr pidlFolder,
                uint cidl,
                [In, MarshalAs(UnmanagedType.LPArray)] IntPtr[] apidl,
                uint dwFlags);

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            public static extern IntPtr ILCreateFromPath(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPath);
        }

        public static void OpenFolderAndSelectFiles(string folder, params string[] filesToSelect)
        {
            int i;

            var filesToSelectIntPtrs = new IntPtr[filesToSelect.Length];
            for (i = 0; i < filesToSelect.Length; i++)
                filesToSelectIntPtrs[i] = SafeNativeMethods.ILCreateFromPath(filesToSelect[i]);

            var dir = SafeNativeMethods.ILCreateFromPath(folder);
            SafeNativeMethods.SHOpenFolderAndSelectItems(dir, (uint)filesToSelect.Length, filesToSelectIntPtrs, 0);
            ReleaseComObject(dir);
            ReleaseComObject(filesToSelectIntPtrs);
        }

        private static void ReleaseComObject(params object[] comObjs)
        {
            foreach (object obj in comObjs)
            {
                if (obj != null && Marshal.IsComObject(obj))
                    Marshal.ReleaseComObject(obj);
            }
        }
    }
}
