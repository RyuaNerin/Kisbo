using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kisbo.Utilities;
using WinTaskbar;

namespace Kisbo.Core
{
    internal partial class SearchWindow : Form
    {
        public SearchWindow()
        {
            InitializeComponent();
            this.ctlNotify.Text = this.Text = string.Format("Kisbo rev.{0}", KisboMain.RevNumber);
            this.ctlNotify.Icon = this.Icon = Properties.Resources.kisbo;

            if (!KisboMain.IsAdministratorMode)
                this.ctlInstallContextMenu.Image = this.ctlUninstallContextMenu.Image = UacImage.GetImage();

            this.m_taskbar = new Taskbar(this);
        }

        private void ctlCopyRight_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/Kisbo\"" }).Dispose();
        }

        private readonly Taskbar m_taskbar;
        private readonly ManualResetEvent m_isWorking = new ManualResetEvent(true);
        private readonly CancellationTokenSource m_cancel = new CancellationTokenSource();

        public readonly List<KisboFile> m_files = new List<KisboFile>();
        public readonly List<KisboFile> m_working = new List<KisboFile>();
        private Task[] m_workers = new Task[4];
        private Uri m_googleUri = new Uri("https://google.com/");
        
        public void AddFile(byte[] data)
        {
            this.AddFile(Encoding.UTF8.GetString(data).Split('\n'));
        }
        public void AddFile(IEnumerable<string> data)
        {
            if (this.InvokeRequired)
                this.Invoke(new Action<IEnumerable<string>>(this.AddFile), data);
            else
            {
                this.ctlList.BeginUpdate();

                bool containsFile = false;

                int find;
                lock (m_files)
                {
                    var lst = new List<string>();
                    int i;

                    foreach (var path in data)
                    {
                        if (KisboMain.Check(path))
                        {
                            containsFile = true;

                            find = this.m_files.FindIndex(e => e.FilePath.Equals(path, StringComparison.OrdinalIgnoreCase));

                            if (find >= 0)
                            {
                                var item = this.m_files[find];
                                if (item.State == States.Complete ||
                                    item.State == States.Error)
                                    item.Remove();
                            }

                            lst.Add(path);
                        }
                    }

                    for (i = 0; i < lst.Count; ++i)
                        KisboFile.Create(this, lst[i]);
                }

                this.ctlList.EndUpdate();

                this.SetProgress();

                if (!containsFile)
                    this.Activate();
            }
        }

        private void SearchWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.Hide();
            this.m_cancel.Cancel(true);
            Task.WaitAll(this.m_workers);
            Application.Exit();
        }

        private void SearchWindow_Shown(object sender, EventArgs e)
        {
            this.SetProgress();
            Task.Factory.StartNew(this.Search);
            Task.Factory.StartNew(new Action(() => {
                var update = LastRelease.CheckNewVersion();
                if (update != null)
                    if (this.ShowMessageBox("새 업데이트가 있어요!", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                        Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = string.Format("\"{0}\"", update.HtmlUrl) }).Dispose();
            }));
        }
        
        private void ctlPause_Click(object sender, EventArgs e)
        {
            if (this.m_isWorking.WaitOne(0))
            {
                this.m_isWorking.Reset();
                this.ctlPause.Text = "재시작";
            }
            else
            {
                this.m_isWorking.Set();
                this.ctlPause.Text = "일시정지";
            }

            this.SetProgress();
        }

        #region 검색
        public void SetProgress()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(this.SetProgress));
            }
            else
            {
                int complete = 0;
                int count;

                lock (this.m_files)
                {
                    count = this.m_working.Count;

                    for (int i = 0; i < this.m_working.Count; ++i)
                        if (this.m_working[i].State == States.Complete ||
                            this.m_working[i].State == States.Error)
                            ++complete;
                }

                this.ctlProgress.Text = string.Format("{0} / {1}", complete, count);

                if (complete != count)
                {
                    this.m_taskbar.SetProgressValue(complete + 1, count + 1);

                    if (this.m_isWorking.WaitOne(0))
                        this.m_taskbar.SetProgressState(TaskbarProgressBarState.Normal);
                    else
                        this.m_taskbar.SetProgressState(TaskbarProgressBarState.Paused);
                }
                else
                    this.m_taskbar.SetProgressState(TaskbarProgressBarState.NoProgress);
            }
        }
        
        private DialogResult ShowMessageBox(string text, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (this.InvokeRequired)
                return (DialogResult)this.Invoke(new Func<string, MessageBoxButtons, MessageBoxIcon, DialogResult>(this.ShowMessageBox), text, buttons, icon);
            else
                return MessageBox.Show(this, text, this.Text, buttons, icon);
        }

        private void Search()
        {
            // Check local domain
            try
            {
                var req = HttpWebRequest.Create(this.m_googleUri) as HttpWebRequest;
                req.Method = "HEAD";
                req.UserAgent = WebClientx.UserAgent;
                
                using (var res = req.GetResponse())
                    this.m_googleUri = new Uri(res.ResponseUri, "/");
            }
            catch
            {
            }

            // 4 Workers
            (this.m_workers[0] = new Task(this.SearchWorker, this.m_cancel.Token, this.m_cancel.Token, TaskCreationOptions.LongRunning)).Start();
            (this.m_workers[1] = new Task(this.SearchWorker, this.m_cancel.Token, this.m_cancel.Token, TaskCreationOptions.LongRunning)).Start();
            (this.m_workers[2] = new Task(this.SearchWorker, this.m_cancel.Token, this.m_cancel.Token, TaskCreationOptions.LongRunning)).Start();
            (this.m_workers[3] = new Task(this.SearchWorker, this.m_cancel.Token, this.m_cancel.Token, TaskCreationOptions.LongRunning)).Start();
        }

        private void SearchWorker(object oToken)
        {
            var token = (CancellationToken)oToken;

            KisboFile file;
            int i;
            bool working;

            using (var wc = new WebClientx(token))
            using (var buff = new MemoryStream(3 * 1024 * 1024))
            {
                var writer = new StreamWriter(buff, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" };

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        this.m_isWorking.WaitOne();

                        lock (this.m_files)
                        {
                            file = null;
                            working = true;
                            for (i = 0; i < this.m_working.Count; ++i)
                            {
                                if (working && (this.m_working[i].State == States.Complete || this.m_working[i].State == States.Error))
                                    working = false;

                                if (this.m_working[i].State == States.Wait)
                                {
                                    file = this.m_working[i];
                                    this.Invoke(new Action<States>(file.SetState), States.Working);
                                    break;
                                }
                            }
                        }

                        if (file != null)
                        {
                            if (SearchImage(wc, file, writer, token))
                                this.Invoke(new Action<States>(file.SetState), States.Complete);
                            else
                                this.Invoke(new Action<States>(file.SetState), States.Error);

                            this.SetProgress();
                        }
                        else
                        {
                            if (working)
                            {
                                lock (this.m_files)
                                {
                                    this.m_working.Clear();
                                }
                            }

                            Thread.Sleep(50);
                        }
                    }
                }
                catch
                {
                }
            }
        }
        
        private static Regex regSimilar = new Regex("<a href=\"([^\"]+)\">", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private bool SearchImage(WebClientx wc, KisboFile file, StreamWriter writer, CancellationToken token)
        {
            string body;

            Size oldSize;

            try
            {
                using (var img = Image.FromFile(file.FilePath))
                    this.Invoke(new Action<Size>(file.SetBeforeResolution), oldSize = img.Size);
            }
            catch
            {
                return false;
            }

            try
            {
                {
                    var origBoundary = "-----" + Guid.NewGuid().ToString("N");
                    var boundary = "--" + origBoundary;

                    var info = new FileInfo(file.FilePath);

                    // DELETE BOM
                    writer.BaseStream.SetLength(0);
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Disposition: form-data; name=\"image_url\"");
                    writer.WriteLine();
                    writer.WriteLine();
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Disposition: form-data; name=\"encoded_image\"; filename=\"\"");
                    writer.WriteLine("Content-Type: application/octet-stream");
                    writer.WriteLine();
                    writer.WriteLine();
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Disposition: form-data; name=\"image_content\"");
                    writer.WriteLine();
                    using (var fileStream = info.OpenRead())
                        Base64Stream.WriteTo(fileStream, writer.BaseStream, token);
                    writer.WriteLine();
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Disposition: form-data; name=\"filename\"");
                    writer.WriteLine();
                    writer.WriteLine(info.Name);
                    writer.WriteLine(boundary + "--");

                    writer.BaseStream.Position = 0;

                    if (!wc.UploadData(new Uri(this.m_googleUri, "/searchbyimage/upload"), writer.BaseStream, "multipart/form-data; boundary=" + origBoundary))
                        return false;
                }

                var searchResultUri = new Uri(this.m_googleUri, wc.Location);
                file.GoogleUrl = searchResultUri.AbsoluteUri;

                if ((body = wc.DownloadString(searchResultUri)) == null)
                    return false;

                int startIndex = body.IndexOf("<div class=\"card-section\">", StringComparison.OrdinalIgnoreCase);
                int endIndex   = body.IndexOf("<hr class=\"rgsep _l4\">", startIndex, StringComparison.OrdinalIgnoreCase);

                var m = regSimilar.Match(body, startIndex, endIndex - startIndex);

                if (m.Success)
                {
                    if ((body = wc.DownloadString(new Uri(this.m_googleUri, m.Groups[1].Value.Replace("&amp;", "&")))) == null)
                        return false;

                    string part, newExtension = null, newPath, tempPath = null, dir;
                    Guid guid;
                    RGMeta rgMeta;

                    startIndex = 0;
                    while ((startIndex = body.IndexOf("<div class=\"rg_meta\">", startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
                    {
                        if (file.Removed) break;
                        if (token.IsCancellationRequested) return false;

                        startIndex = body.IndexOf("{", startIndex + 1, StringComparison.OrdinalIgnoreCase);
                        endIndex = body.IndexOf("</div>", startIndex, StringComparison.OrdinalIgnoreCase);
                        part = body.Substring(startIndex, endIndex - startIndex);

                        startIndex = endIndex + 1;

                        try
                        {
                            rgMeta = RGMeta.Parse(part);
                            if (rgMeta.Width  <= oldSize.Width ||
                                rgMeta.Height <= oldSize.Height)
                                continue;

                            tempPath = Path.GetTempFileName();
                            using (var fileStream = File.OpenWrite(tempPath))
                                if (!BypassHttp.GetResponse(rgMeta.ImageUrl, fileStream, token))
                                    continue;

                            if (file.Removed) break;
                            using (var img = Image.FromFile(tempPath))
                            {
                                guid = img.RawFormat.Guid;

                                     if (guid == ImageFormat.Bmp .Guid) newExtension = ".bmp";
                                else if (guid == ImageFormat.Gif .Guid) newExtension = ".gif";
                                else if (guid == ImageFormat.Jpeg.Guid) newExtension = ".jpg";
                                else if (guid == ImageFormat.Png .Guid) newExtension = ".png";

                                if (img.Width  <= oldSize.Width ||
                                    img.Height <= oldSize.Height)
                                    continue;

                                this.Invoke(new Action<Size>(file.SetAfterResolution), oldSize = img.Size);
                            }

                            if (file.Removed) break;

                            dir = Path.Combine(Path.GetDirectoryName(file.FilePath), "kisbo-original");
                            if (!Directory.Exists(dir))
                                Directory.CreateDirectory(dir);

                            File.Move(file.FilePath, Path.Combine(dir, Path.GetFileName(file.FilePath)));

                            newPath = Path.ChangeExtension(file.FilePath, newExtension);
                            File.Move(tempPath, GetSafeFileName(newPath, ""));
                            
                            return true;
                        }
                        catch
                        {
                        }
                        finally
                        {
                            try
                            {
                                File.Delete(tempPath);
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static string GetSafeFileName(string path, string part)
        {
            var dir  = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext  = Path.GetExtension(path);

            var newPath = Path.Combine(dir, string.Format("{0}{1}{2}", name, part, ext));
            if (!File.Exists(newPath) && !Directory.Exists(newPath)) return newPath;

            int i = 0;
            do 
            {
                newPath = Path.Combine(dir, string.Format("{0}{1} ({2}){3}", name, part, ++i, ext));
            } while (File.Exists(newPath) || Directory.Exists(newPath));

            return newPath;
        }
        #endregion

        #region 메뉴
        private void ctlRemoveSelected_Click(object sender, EventArgs e)
        {
            lock (this.m_files)
            {
                int index = this.ctlList.SelectedItems[0].Index - 1;

                while (this.ctlList.SelectedItems.Count > 0)
                    ((KisboFile)this.ctlList.SelectedItems[0].Tag).Remove();

                if (index < 0) index = 0;
                if (index >= this.m_files.Count) index = this.m_files.Count - 1;

                if (index >= 0)
                    this.ctlList.Items[index].Selected = true;
            }
        }

        private void ctlClearAll_Click(object sender, EventArgs e)
        {
            lock (this.m_files)
            {
                while (this.m_files.Count > 0)
                    this.m_files[0].Remove();

                this.SetProgress();
            }
        }

        private void ctlRemoveCompleted_Click(object sender, EventArgs e)
        {
            lock (this.m_files)
            {
                int i = 0;
                while ( i < this.m_files.Count)
                {
                    if (this.m_files[i].State == States.Complete)
                        this.m_files[i].Remove();
                    else
                        ++i;
                }

                this.SetProgress();
            }
        }
        
        private void ctlListMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ctlOpenGoogleResult.Enabled = false;
            for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
            {
                if (((KisboFile)this.ctlList.SelectedItems[i].Tag).GoogleUrl != null)
                {
                    this.ctlOpenGoogleResult.Enabled = true;
                    break;
                }
            }
        }
        private void openDirectoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ctlList.SelectedItems.Count == 1)
            {
                var file = (KisboFile)this.ctlList.SelectedItems[0].Tag;
                using (Process.Start(new ProcessStartInfo("explorer", string.Format("/select,\"{0}\"", file.FilePath))))
                { }
            }
            else if (this.ctlList.SelectedItems.Count > 1)
            {
                var dic = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                
                KisboFile file;
                string dir;
                for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
                {
                    file = (KisboFile)this.ctlList.SelectedItems[i].Tag;
                    dir = Path.GetDirectoryName(file.FilePath);

                    if (!dic.ContainsKey(dir))
                        dic.Add(dir, new List<string>());

                    dic[dir].Add(file.FilePath);
                }

                foreach (var st in dic)
                    ExplorerHelper.OpenFolderAndSelectFiles(st.Key, st.Value.ToArray());
            }
        }

        private void openGoogleResultToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.ctlList.SelectedItems.Count <= 10)
            {
                KisboFile file;
                for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
                {
                    file = (KisboFile)this.ctlList.SelectedItems[0].Tag;

                    if (file.GoogleUrl != null)
                        using (Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = string.Format("\"{0}\"", file.GoogleUrl) }))
                        { }
                }
            }
        }

        private void ctlList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var item = this.ctlList.GetItemAt(e.X, e.Y);
                var file = (KisboFile)item.Tag;
                using (Process.Start(new ProcessStartInfo("explorer", string.Format("/select,\"{0}\"", file.FilePath))))
                { }
            }
        }
        #endregion

        #region 파일 추가하는 부분
        private void ctlList_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    for (int i = 0; i < files.Length; ++i)
                    {
                        if (KisboMain.Check(files[i]))
                        {
                            e.Effect = DragDropEffects.All;
                            return;
                        }
                    }
                }
            }
        }
        private void ctlList_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                    this.AddFile(files);
            }
        }
        #endregion

        #region Add
        private void ctlAddFile_Click(object sender, EventArgs e)
        {
            if (this.ctlOpenFile.ShowDialog() == DialogResult.OK)
            {
                var lst = new List<string>();
                for (int i = 0; i < this.ctlOpenFile.FileNames.Length; ++i)
                    if (KisboMain.Check(this.ctlOpenFile.FileNames[i]))
                        lst.Add(this.ctlOpenFile.FileNames[i]);

                if (lst.Count > 100)
                    ShowMessageBox("파일 수가 너무 많습니다!\n한번에 100개 이하씩 추가해주세요", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else if (lst.Count > 0)
                    this.AddFile(lst.ToArray());
            }
        }

        private void ctlAddDir_Click(object sender, EventArgs e)
        {
            if (this.ctlOpenDir.ShowDialog() == DialogResult.OK)
            {
                var lst = new List<string>();

                if (!GetFiles(lst, this.ctlOpenDir.SelectedPath))
                    ShowMessageBox("파일 수가 너무 많습니다!\n한번에 100개 이하씩 추가해주세요", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private bool GetFiles(IList<string> lst, string path, bool? containsSubdir = null)
        {
            int i = 0;
            string[] paths;

            paths = Directory.GetFiles(path);
            for (i = 0; i < path.Length; ++i)
            {
                if (KisboMain.Check(paths[i]))
                {
                    lst.Add(paths[i]);
                    if (lst.Count > 100)
                        return false;
                }
            }

            if (!containsSubdir.HasValue || !containsSubdir.Value)
            {
                paths = Directory.GetDirectories(path);

                // contains subitem
                if (!containsSubdir.HasValue && Directory.GetDirectories(this.ctlOpenDir.SelectedPath).Length > 0)
                    containsSubdir = ShowMessageBox("하위 폴더들도 추가할까요?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

                for (i = 0; i < path.Length; ++i)
                    if (!GetFiles(lst, paths[i], containsSubdir))
                        return false;
            }

            return true;
        }
        #endregion

        #region ContextMenu
        private void ctlInstallContextMenu_Click(object sender, EventArgs e)
        {
            this.ctlInstallContextMenu.Enabled = this.ctlUninstallContextMenu.Enabled = false;
            Task.Factory.StartNew(new Action(this.ctlInstallContextMenu_Click));
        }

        private void ctlInstallContextMenu_Click()
        {
            switch (ShellExtension.Install())
            {
            case ShellExtension.Result.NO_ERROR:
                this.RestartExplorer("우클릭 메뉴를 추가했어요");
                break;

            case ShellExtension.Result.FAIL_REG:
                this.ShowMessageBox("DLL 을 등록하지 못했습니다", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            case ShellExtension.Result.DLL_CREATAION_FAIL:
                this.ShowMessageBox("DLL 파일을 만들지 못했습니다", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            case ShellExtension.Result.NOT_AUTHORIZED:
                this.ShowMessageBox("관리자 권한으로 실행해주세요!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            case ShellExtension.Result.UNKNOWN:
                this.ShowMessageBox("알 수 없는 문제가 발생했어요!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;
            }

            this.Invoke(new Action(() => this.ctlInstallContextMenu.Enabled = this.ctlUninstallContextMenu.Enabled = true));
        }

        private void ctlUninstallContextMenu_Click(object sender, EventArgs e)
        {
            this.ctlInstallContextMenu.Enabled = this.ctlUninstallContextMenu.Enabled = false;
            Task.Factory.StartNew(new Action(this.ctlUninstallContextMenu_Click));
        }

        private void ctlUninstallContextMenu_Click()
        {
            switch (ShellExtension.Uninstall())
            {
            case ShellExtension.Result.NO_ERROR:
                this.RestartExplorer("우클릭 메뉴를 제거했어요");
                break;

            case ShellExtension.Result.FAIL_REG:
                this.ShowMessageBox("DLL 을 등록 해제하지 못했어요", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            case ShellExtension.Result.DLL_NOT_EXITED:
                this.ShowMessageBox("DLL 파일이 없어요", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            case ShellExtension.Result.NOT_AUTHORIZED:
                this.ShowMessageBox("관리자 권한으로 실행해주세요!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;

            case ShellExtension.Result.UNKNOWN:
                this.ShowMessageBox("알 수 없는 문제가 발생했어요!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                break;
            }

            this.Invoke(new Action(() => this.ctlInstallContextMenu.Enabled = this.ctlUninstallContextMenu.Enabled = true));
        }

        private void RestartExplorer(string str)
        {
            if (this.ShowMessageBox(str + "\n탐색기를 재시작 할까요?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                //taskkill /IM explorer.exe /F & explorer.exe
                using (var proc = Process.Start(new ProcessStartInfo { Arguments = "/IM explorer.exe /F", FileName = "taskkill", WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true }))
                    proc.WaitForExit();

                Process.Start("explorer.exe").Dispose();
            }
        }
        #endregion

        #region TopMost
        private void ctlTopMost_CheckedChanged(object sender, EventArgs e)
        {
            this.TopMost = this.ctlTopMost.Checked;
        }
        #endregion

        #region Tray
        private void SearchWindow_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.Hide();
        }
        private void ctlNotify_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Normal;
            this.Show();
            this.Focus();
        }
        #endregion
    }
}
