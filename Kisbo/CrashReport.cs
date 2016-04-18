using System;
using System.IO;
using System.Windows.Forms;

namespace Kisbo
{
    internal static class CrashReport
    {
        public static void Init()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowCrashReport((Exception)e.ExceptionObject);
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) => ShowCrashReport(e.Exception);
        }

        private static void ShowCrashReport(Exception exception)
        {
            var date = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
            var file = string.Format("Crash-{0}.txt", date);

            using (var writer = new StreamWriter(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), file)))
            {
                writer.WriteLine("Decchi Crash Report");
                writer.WriteLine("Date    : " + date);
                writer.WriteLine("Version : " + Application.ProductVersion);
                writer.WriteLine();
                writer.WriteLine("OS Ver  : " + GetOSInfomation());
                writer.WriteLine("SPack   : " + System.Environment.OSVersion.VersionString);
                writer.WriteLine();
                writer.WriteLine("Exception");
                writer.WriteLine(exception.ToString());
            }
        }

        private static string GetOSInfomation()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion"))
                    return key.GetValue("ProductName").ToString();
            }
            catch
            {
            }

            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\Windows NT\CurrentVersion"))
                    return key.GetValue("ProductName").ToString();
            }
            catch
            {
            }

            return "Operating System Information unavailable";
        }
    }
}
