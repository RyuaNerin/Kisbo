using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Windows.Forms;
using Kisbo.Core;
using Kisbo.Utilities;

namespace Kisbo
{
    internal static class KisboMain
    {
        private const string KISBO_MUTEX_NAME = "5088D54F-99E9-4C3B-B780-837FD12FF3A6";
        
        public static readonly int RevNumber;
        public static readonly bool IsAdministratorMode;

        static KisboMain()
        {
            RevNumber = Assembly.GetExecutingAssembly().GetName().Version.Revision;

            LibraryResolver.Init(typeof(Properties.Resources));
            CrashReport.Init();

            try
            {
                using (var cur = WindowsIdentity.GetCurrent())
                    IsAdministratorMode = new WindowsPrincipal(cur).IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                IsAdministratorMode = false;
            }

#if !DEBUG
            HttpWebRequest.DefaultWebProxy = null;
#endif

            HttpWebRequest.DefaultCachePolicy = new RequestCachePolicy(RequestCacheLevel.NoCacheNoStore);

            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.MaxServicePoints = 64;
        }

        private static readonly string[] AllowExtension = { ".bmp", ".jpg", ".jpeg", ".png", ".gif" };
        public static bool Check(string path)
        {
            if (!File.Exists(path)) return false;

            var extension = Path.GetExtension(path);
            for (int i = 0; i < AllowExtension.Length; ++i)
                if (AllowExtension[i].Equals(extension, StringComparison.OrdinalIgnoreCase))
                    return true;

            return false;
        }

        public static InstanceHelperEx Instance;
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                if (args[0] == "--install")   return (int)ShellExtension.Install(true);
                if (args[0] == "--uninstall") return (int)ShellExtension.Uninstall(true);
            }
            
            int i;

            var files = new List<string>(args.Length);
            if (args.Length == 1 && args[0] == "--pipe")
            {
                var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);

                string path;
                while (true)
                {
                    path = reader.ReadLine();
                    if (string.IsNullOrEmpty(path)) break;
                    
                    if (Check(path))
                        files.Add(path);
                }
            }
            else
            {
                for (i = 0; i < args.Length; ++i)
                    if (Check(args[i]))
                        files.Add(args[i]);
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (Instance = new InstanceHelperEx(KISBO_MUTEX_NAME))
            {
                if (Instance.IsInstance)
                {                    
                    var frm = new SearchWindow();
                    Instance.DataReceived += frm.AddFile;
                    Instance.Ready();

                    frm.AddFile(files);

                    Application.Run(frm);
                }
                else
                {
                    if (files.Count > 0)
                    {
                        byte[] data;
                        var sb = new StringBuilder(4096);
                        for (i = 0; i < args.Length; ++i)
                        {
                            if (i > 0)
                                sb.Append('\n');
                            sb.Append(args[i]);
                        }

                        data = Encoding.UTF8.GetBytes(sb.ToString());

                        Instance.Send(data);
                    }
                    else
                        Instance.Send(new byte[1] { 0 });
                }
            }

            return 0;
        }
    }
}
