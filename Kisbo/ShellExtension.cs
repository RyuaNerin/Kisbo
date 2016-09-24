using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Kisbo
{
    internal static class ShellExtension
    {
        public enum Result : int
        {
            UNKNOWN = -1,
            NO_ERROR = 0,
            NOT_AUTHORIZED = 1,
            DLL_NOT_EXITED = 2,
            DLL_CREATAION_FAIL = 3,
            FAIL_REG = 4,
            FILE_USED = 5

        }
        public static Result Install(bool runas = false)
        {
            var startup = new ProcessStartInfo();
            startup.WindowStyle = ProcessWindowStyle.Hidden;
            startup.UseShellExecute = true;
            startup.WorkingDirectory = Environment.CurrentDirectory;

            if (KisboMain.IsAdministratorMode)
            {
                var dllPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), string.Format("Kisbo{0}.dll", IntPtr.Size == 8 ? 64 : 32));
                try
                {
                    File.WriteAllBytes(dllPath, IntPtr.Size == 8 ? Properties.Resources.KisboExt64 : Properties.Resources.KisboExt32);
                }
                catch (IOException)
                {
                    return Result.FILE_USED;
                }
                catch
                {
                    return Result.DLL_CREATAION_FAIL;
                }
                
                try
                {                	
                    using (var key = Registry.LocalMachine.CreateSubKey(@"Software\RyuaNerin"))
                        key.SetValue("Kisbo", Application.ExecutablePath, RegistryValueKind.ExpandString);
                }
                catch
                {
                    return Result.NOT_AUTHORIZED;
                }

                startup.FileName = "regsvr32";
                startup.Arguments = string.Format("/s \"{0}\"", dllPath);
                
                using (var proc = Process.Start(startup))
                {
                    proc.WaitForExit();
                    return proc.ExitCode == 0 ? Result.NO_ERROR : Result.FAIL_REG;
                }
            }
            else if (runas)
            {
                // 관리자로 켰는데 관리자가 아닌 경우
                return Result.NOT_AUTHORIZED;
            }
            else
            {
                startup.FileName = Application.ExecutablePath;
                startup.Arguments = "--install";
                startup.Verb = "runas";

                try
                {
                    using (var proc = Process.Start(startup))
                    {
                        proc.WaitForExit();
                        return (Result)proc.ExitCode;
                    }
                }
                catch(SystemException)
                {
                    return Result.NOT_AUTHORIZED;
                }
            }
        }
        public static Result Uninstall(bool runas = false)
        {
            var startup = new ProcessStartInfo();
            startup.WindowStyle = ProcessWindowStyle.Hidden;
            startup.UseShellExecute = true;
            startup.WorkingDirectory = Environment.CurrentDirectory;
     

            if (KisboMain.IsAdministratorMode)
            {
                var dllPath = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), string.Format("Kisbo{0}.dll", IntPtr.Size == 8 ? 64 : 32));
                if (!File.Exists(dllPath))
                    return Result.DLL_NOT_EXITED;

                startup.FileName = "regsvr32";
                startup.Arguments = string.Format("/s /u \"{0}\"", dllPath);
                

                using (var proc = Process.Start(startup))
                {
                    proc.WaitForExit();
                    return proc.ExitCode == 0 ? Result.NO_ERROR : Result.FAIL_REG;
                }
      
            }
            else if (runas)
            {
                // 관리자로 켰는데 관리자가 아닌 경우
                return Result.NOT_AUTHORIZED;
            }
            else
            {
                startup.FileName = Application.ExecutablePath;
                startup.Arguments = "--uninstall";
                startup.Verb = "runas";

                try
                {
                    using (var proc = Process.Start(startup))
                    {
                        proc.WaitForExit();
                        return (Result)proc.ExitCode;
                    }
                }
                catch (SystemException)
                {
                    return Result.NOT_AUTHORIZED;
                }
            }
        }
    }
}
