using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Kisbo.Utilities;
using Newtonsoft.Json.Linq;
using WinTaskbar;

using HtmlDocument = HtmlAgilityPack.HtmlDocument;

namespace Kisbo.Core
{
    internal partial class SearchWindow : Form
    {
        private const int MaxWorkers = 8;

        public SearchWindow()
        {
            InitializeComponent();
            this.ctlNotify.Text = this.Text = string.Format("Kisbo rev.{0}", KisboMain.RevNumber);
            this.ctlNotify.Icon = this.Icon = Properties.Resources.kisbo;

            if (!KisboMain.IsAdministratorMode)
                this.ctlInstallContextMenu.Image = this.ctlUninstallContextMenu.Image = UacImage.GetImage();

            this.m_taskbar = (ITaskbarList4)new CTaskbarList();
            this.m_taskbar.HrInit();
        }

        private readonly ITaskbarList4 m_taskbar;
        private readonly CancellationTokenSource m_cancel = new CancellationTokenSource();

        // 다운로드 큐
        private readonly LinkedList<KisboFile> m_queue = new LinkedList<KisboFile>();

        // 파일 path
        private readonly HashSet<string> m_filePathHash = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 쓰레드 작동 갯수
        private long m_threadCount = 0;

        // Set = 시작 / Reset = 일시정지
        private readonly ManualResetEventSlim m_workerNoPause = new ManualResetEventSlim(true);

        private Uri m_googleUri = new Uri("https://google.com/");

        private void ctlCopyRight_DoubleClick(object sender, EventArgs e)
        {
            Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = "\"https://github.com/RyuaNerin/Kisbo\"" }).Dispose();
        }

        private void SearchWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            KisboMain.Instance.Release();
        }

        private int m_formClosing = 0;
        private void SearchWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                this.Hide();
                this.ctlNotify.ShowBalloonTip(5000, this.Text, "트레이에서 여전히 실행중!!", ToolTipIcon.Info);
                e.Cancel = true;
            }
            else
            {
                if (Interlocked.CompareExchange(ref this.m_formClosing, 1, 0) == 0)
                {
                    this.Enabled = false;

                    this.ctlNotify.Visible = false;
                    this.Hide();

                    Application.Exit();
                }
            }
        }

        private async void SearchWindow_Shown(object sender, EventArgs e)
        {
            await Task.Factory.StartNew(this.GetLocalGoogleDomain);

            var version = await Task.Factory.StartNew(LastRelease.CheckNewVersion);
            if (version != null)
            {
                if (this.ShowMessageBox("새 업데이트가 있어요!", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.OK)
                    Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = string.Format("\"{0}\"", version.HtmlUrl) }).Dispose();
            }
        }

        #region 검색
        public void AddFile(byte[] data)
        {
            if (data.Length == 1 && data[0] == 0xFE)
                this.Invoke(new Action(this.Activate));
            else
                this.AddFile(Encoding.UTF8.GetString(data).Split('\n'));
        }
        public void AddFile(IEnumerable<string> data)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action<IEnumerable<string>>(this.AddFile), data);
                }
                else
                {
                    this.ctlList.BeginUpdate();

                    var lst = new List<string>();

                    lock (this.m_queue)
                    {
                        Debug.WriteLine("AddFile Lock");

                        foreach (var path in data)
                            if (KisboMain.Check(path) && !this.m_filePathHash.Contains(path))
                                lst.Add(path);

                        if (lst.Count == 0)
                            return;

                        for (int i = 0; i < lst.Count; ++i)
                        {
                            var kf = new KisboFile(this, lst[i]);

                            this.ctlList.Items.Add(kf.ListViewItem);
                            this.m_filePathHash.Add(kf.OriginalFilePath);

                            this.m_queue.AddLast(kf);
                        }

                        Debug.WriteLine("AddFile Unlock");
                    }

                    this.ctlList.EndUpdate();

                    this.StartWorker(lst.Count);
                    this.UpdateProgress();
                }
            }
            catch
            {
            }
        }

        private void StartWorker(int count)
        {
            while (count-- > 0 && Interlocked.Read(ref this.m_threadCount) < MaxWorkers)
            {
                Interlocked.Increment(ref this.m_threadCount);

                new Thread(this.SearchWorker)
                {
                    IsBackground = true,
                }.Start();
            }
        }

        public void UpdateProgress()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(this.UpdateProgress));
                }
                else
                {
                    var threads = (int)Interlocked.Read(ref this.m_threadCount);

                    int val, max;

                    lock (this.m_queue)
                    {
                        val = this.ctlList.Items.Count - this.m_queue.Count - threads;
                        max = this.ctlList.Items.Count;
                    }

                    if (val < 0)
                        val = 0;

                    this.ctlProgress.Text = string.Format("{0} / {1}", val, max);

                    if (val != max)
                    {
                        this.m_taskbar.SetProgressValue(this.Handle, (ulong)val + 1, (ulong)max + 1);

                        if (this.m_workerNoPause.IsSet)
                            this.m_taskbar.SetProgressState(this.Handle, TBPFLAG.TBPF_NORMAL);
                        else
                            this.m_taskbar.SetProgressState(this.Handle, TBPFLAG.TBPF_PAUSED);
                    }
                    else
                    {
                        this.m_taskbar.SetProgressState(this.Handle, TBPFLAG.TBPF_NOPROGRESS);

                        if (threads == 0)
                            this.Invoke(new Action<int, string, string, ToolTipIcon>(this.ctlNotify.ShowBalloonTip), 5000, this.Text, "작업을 끝냈어요!!", ToolTipIcon.Info);
                    }
                }
            }
            catch
            {
            }
        }
        
        private DialogResult ShowMessageBox(string text, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            try
            {
                if (this.InvokeRequired)
                    return (DialogResult)this.Invoke(new Func<string, MessageBoxButtons, MessageBoxIcon, DialogResult>(this.ShowMessageBox), text, buttons, icon);
                else
                    return MessageBox.Show(this, text, this.Text, buttons, icon);
            }
            catch
            {
                return DialogResult.None;
            }
        }

        private void GetLocalGoogleDomain()
        {
            // Check local domain
            try
            {
                var req = HttpWebRequest.Create(this.m_googleUri) as HttpWebRequest;
                req.Method = "HEAD";
                req.UserAgent = WebClientx.UserAgent;
                
                using (var res = req.GetResponse())
                    this.m_googleUri = new UriBuilder(new Uri(res.ResponseUri, "/")) { Scheme = "https" }.Uri;
            }
            catch
            {
            }
        }

#if DEBUG
        private readonly Random m_workerId = new Random(DateTime.Now.Millisecond);
#endif
        private void SearchWorker()
        {
            var token = this.m_cancel.Token;

#if DEBUG
            var threadId = this.m_workerId.Next().ToString("X");
            Thread.CurrentThread.Name = $"SearchWorker {threadId}";
#endif

            using (var wc = new WebClientx(token))
            using (var buff = new MemoryStream(1024 * 1024))
            {
                var writer = new StreamWriter(buff, Encoding.UTF8) { AutoFlush = true, NewLine = "\r\n" };

                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        this.m_workerNoPause.Wait();
                        if (token.IsCancellationRequested) return;

                        if (buff.Capacity > 2 * 1024 * 1024)
                        {
                            buff.SetLength(0);
                            buff.Capacity = 1024 * 1024;
                        }

                        KisboFile kf;
                        lock (this.m_queue)
                        {
                            var node = this.m_queue.First;

                            if (node == null)
                            {
                                break;
                            }

                            kf = node.Value;

                            this.m_queue.Remove(node);
                        }

                        kf.State = States.Working;
                        if (token.IsCancellationRequested) return;

                        this.UpdateProgress();

                        this.SearchImage(wc, kf, writer, token);

                        this.UpdateProgress();
                    }
                }
                catch
                {
                }
            }

            Interlocked.Decrement(ref this.m_threadCount);
            this.UpdateProgress();
        }
        
        private void SearchImage(WebClientx wc, KisboFile file, StreamWriter writer, CancellationToken token)
        {
            string body;

            Size oldSize;

            try
            {
                using (var img = Image.FromFile(file.OriginalFilePath))
                    file.UpdateBeforeResolution(oldSize = img.Size);
            }
            catch
            {
                file.State = States.Error;
                return;
            }

            try
            {
                {
                    var origBoundary = "-----" + Guid.NewGuid().ToString("N");
                    var boundary = "--" + origBoundary;

                    var info = new FileInfo(file.OriginalFilePath);

                    // DELETE BOM
                    writer.BaseStream.SetLength(0);
                    writer.WriteLine(boundary);
                    writer.WriteLine("Content-Disposition: form-data; name=\"encoded_image\"; filename=\"{0}\"", info.Name);
                    writer.WriteLine("Content-Type: application/octet-stream");
                    writer.WriteLine();
                    using (var fileStream = info.OpenRead())
                        fileStream.CopyTo(writer.BaseStream, 4096);
                    writer.WriteLine();
                    writer.WriteLine(boundary + "--");

                    writer.BaseStream.Position = 0;

                    do
                    {
                        if (token.IsCancellationRequested)
                        {
                            file.State = States.Error;
                            return;
                        }

                        if (!this.m_workerNoPause.Wait(0))
                        {
                            file.State = States.WaitSearch;
                            this.m_workerNoPause.Wait();
                            if (token.IsCancellationRequested)
                            {
                                file.State = States.Error;
                                return;
                            }

                            file.State = States.Working;
                        }

                        body = wc.UploadData(new Uri(this.m_googleUri, "/searchbyimage/upload"), writer.BaseStream, "multipart/form-data; boundary=" + origBoundary);
                        if (body == null)
                        {
                            file.State = States.Error;
                            return;
                        }

                        // TODO
                        /*
                        if (body.IndexOf("topstuff", StringComparison.OrdinalIgnoreCase) == -1)
                        {
                            body = null;

                            if (this.m_search.WaitOne(0))
                            {
                                file.State = States.WaitSearch;
                                file.SetStatus();

                                this.m_search.Reset();
                                Thread.Sleep(60 * 1000);
                                this.m_search.Set();

                                if (token.IsCancellationRequested) return States.Error;

                                file.State = States.Working;
                                file.SetStatus();
                            }
                        }
                        */
                    } while (body == null);
                }

                var baseUri = wc.ResponseUri;
                file.GoogleUrl = baseUri.AbsoluteUri;
                
                var html = new HtmlDocument();
                html.LoadHtml(body);
                var docNode = html.DocumentNode;

                bool succ = false;
                foreach (var a_gl in docNode.SelectNodes(@"//span[@class='gl']//a"))
                {
                    var href = a_gl.GetAttributeValue("href", null)?.Replace("&amp;", "&");
                    if (href == null)
                        continue;

                    if (!href.Contains("tbs=") || href.Contains(",isz"))
                        continue;

                    var u = new Uri(this.m_googleUri, href);
                    if ((body = wc.DownloadString(u, baseUri.AbsoluteUri)) == null)
                    {
                        file.State = States.Error;
                        return;
                    }
                    baseUri = u;

                    html.LoadHtml(body);
                    docNode = html.DocumentNode;

                    succ = true;
                    break;
                }
                
                if (!succ)
                {
                    file.State = States.NoResult;
                    return;
                }

                var afDataList = new List<AfData>();

                foreach (var scriptNode in docNode.SelectNodes("//*[name()='script']"))
                {
                    var script = scriptNode.InnerText;

                    // find ds:2
                    if (!script.Contains("AF_initDataCallback"))
                        continue;

                    try
                    {
                        var sb = new StringBuilder();
                        var pos = 0;
                        var stack = 0;
                        var escape = false;
                        var isString = false;
                        while (pos < script.Length)
                        {
                            pos = script.IndexOf("[1,[0,", pos);
                            if (pos == -1)
                                break;

                            sb.Clear();
                            sb.Append("[1,[0,");
                            pos += sb.Length;
                            stack = 2;

                            while (pos < script.Length)
                            {
                                var c = script[pos];

                                sb.Append(c);

                                if (escape)
                                {
                                    escape = false;
                                }
                                else
                                {
                                    switch (c)
                                    {
                                        case '[':
                                            if (!isString)
                                                stack++;
                                            break;

                                        case ']':
                                            if (!isString)
                                                stack--;
                                            break;

                                        case '\\':
                                            escape = true;
                                            break;

                                        case '"':
                                            isString = !isString;
                                            break;
                                    }
                                }

                                pos++;

                                if (stack == 0)
                                {
                                    try
                                    {
                                        var ja = JArray.Parse(sb.ToString());

                                        afDataList.Add(new AfData
                                        {
                                            ImageUri = new Uri(ja[1][3][0].Value<string>()),
                                            Referer = ja[1][9]["2003"][2].Value<string>(),
                                            Width = ja[1][3][1].Value<int>(),
                                            Height = ja[1][3][2].Value<int>(),
                                        });
                                    }
                                    catch
                                    {
                                    }

                                    break;
                                }
                            }
                        }

                        if (afDataList.Count > 0)
                            break;
                    }
                    catch
                    {
                    }
                }

                if (afDataList.Count == 0)
                {
                    file.State = States.Error;
                    return;
                }

                afDataList.Sort((a, b) => (a.Width * (long)a.Height).CompareTo(b.Width * (long)b.Height) * -1);

                string newExtension = null, newPath, tempPath = null, dir;
                Guid guid;

                foreach (var afData in afDataList)
                {
                    if (file.Removed) break;

                    if (token.IsCancellationRequested)
                    {
                        file.State = States.Error;
                        return;
                    }

                    try
                    {
                        if (afData.Width  <= oldSize.Width ||
                            afData.Height <= oldSize.Height)
                            continue;

                        tempPath = Path.GetTempFileName();

                        using (var fileStream = File.OpenWrite(tempPath))
                        {
                            fileStream.SetLength(0);
                            if (!wc.DownloadData(afData.ImageUri, fileStream, afData.Referer))
                                continue;
                        }

                        if (file.Removed) break;
                        using (var img = Image.FromFile(tempPath))
                        {
                            guid = img.RawFormat.Guid;

                                 if (guid == ImageFormat.Bmp.Guid)  newExtension = ".bmp";
                            else if (guid == ImageFormat.Gif.Guid)  newExtension = ".gif";
                            else if (guid == ImageFormat.Jpeg.Guid) newExtension = ".jpg";
                            else if (guid == ImageFormat.Png.Guid)  newExtension = ".png";

                            if (img.Width  <= oldSize.Width ||
                                img.Height <= oldSize.Height)
                                continue;

                            file.UpdateAfterResolution(img.Size);
                        }

                        if (file.Removed) break;

                        dir = Path.Combine(Path.GetDirectoryName(file.OriginalFilePath), "kisbo-original");
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        File.Move(file.OriginalFilePath, GetSafeFileName(Path.Combine(dir, Path.GetFileName(file.OriginalFilePath))));

                        newPath = Path.ChangeExtension(file.OriginalFilePath, newExtension);
                        File.Move(tempPath, GetSafeFileName(newPath));

                        file.NewFilePath = newPath;

                        file.State = States.Complete;
                        return;
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

                file.State = States.Pass;
            }
            catch
            {
                file.State = States.Error;
            }
        }

        private static string GetSafeFileName(string path)
        {
            var dir  = Path.GetDirectoryName(path);
            var name = Path.GetFileNameWithoutExtension(path);
            var ext  = Path.GetExtension(path);

            var newPath = Path.Combine(dir, string.Format("{0}{1}", name, ext));
            if (!File.Exists(newPath) && !Directory.Exists(newPath)) return newPath;

            int i = 0;
            do 
            {
                newPath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, ++i, ext));
            } while (File.Exists(newPath) || Directory.Exists(newPath));

            return newPath;
        }
        #endregion

        #region 메뉴
        private void ctlList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                Debug.WriteLine("ctlList_KeyUp Lock");
                for (int i = 0; i < this.ctlList.Items.Count; ++i)
                    this.ctlList.Items[i].Selected = true;
                Debug.WriteLine("ctlList_KeyUp Unlock");
            }
        }

        private void ctlPause_Click(object sender, EventArgs e)
        {
            if (this.m_workerNoPause.IsSet)
            {
                this.m_workerNoPause.Reset();
                this.ctlPause.Text = "일시정지";
            }
            else
            {
                this.m_workerNoPause.Set();
                this.ctlPause.Text = "재시작";
            }

            this.UpdateProgress();
        }

        private void ctlRemoveSelected_Click(object sender, EventArgs e)
        {
            if (this.ctlList.SelectedItems.Count == 0) return;

            for (int i = 0; i <this.ctlList.SelectedItems.Count; ++i)
            {
                if (((KisboFile)this.ctlList.SelectedItems[i].Tag).Working)
                {
                    if (this.ShowMessageBox("진행중인 파일이 있는데 계속할까요?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                        return;
                    break;
                }
            }

            lock (this.m_queue)
            {
                Debug.WriteLine("ctlRemoveSelected_Click Lock");
                int index = this.ctlList.SelectedItems[0].Index - 1;

                this.ctlList.BeginUpdate();

                while (this.ctlList.SelectedItems.Count > 0)
                {
                    var kf = (KisboFile)this.ctlList.SelectedItems[0].Tag;

                    this.m_queue.Remove(kf);

                    this.m_filePathHash.Remove(kf.OriginalFilePath);
                    this.ctlList.Items.RemoveAt(0);
                }

                this.ctlList.EndUpdate();

                if (index < 0) index = 0;
                if (index >= this.ctlList.Items.Count) index = this.ctlList.Items.Count - 1;

                if (index >= 0)
                    this.ctlList.Items[index].Selected = true;
                Debug.WriteLine("ctlRemoveSelected_Click Unlock");
            }
        }

        private void ctlClearAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i <this.ctlList.SelectedItems.Count; ++i)
            {
                if (((KisboFile)this.ctlList.SelectedItems[i].Tag).Working)
                {
                    if (this.ShowMessageBox("진행중인 파일이 있는데 계속할까요?", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                        return;
                    break;
                }
            }

            lock (this.m_queue)
            {
                Debug.WriteLine("ctlClearAll_Click Lock");

                this.ctlList.BeginUpdate();

                this.ctlList.Clear();
                this.m_queue.Clear();
                this.m_filePathHash.Clear();

                this.ctlList.EndUpdate();

                Debug.WriteLine("ctlClearAll_Click Unlock");
            }

            this.UpdateProgress();
        }

        private void ctlRemoveCompleted_Click(object sender, EventArgs e)
        {
            lock (this.m_queue)
            {
                Debug.WriteLine("ctlRemoveCompleted_Click Lock");
                
                this.ctlList.BeginUpdate();

                int i = 0;
                while (i < this.ctlList.Items.Count)
                {
                    var kf = (KisboFile)this.ctlList.Items[i].Tag;

                    if (kf.Success)
                        this.ctlList.Items.RemoveAt(i);
                    else
                        ++i;
                }

                this.ctlList.EndUpdate();

                this.UpdateProgress();

                Debug.WriteLine("ctlRemoveCompleted_Click Unlock");
            }
        }
        
        private void ctlListMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.ctlOpenGoogleResult.Enabled = false;
            this.ctlRework.Enabled = false;

            KisboFile item;
            for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
            {
                item = ((KisboFile)this.ctlList.SelectedItems[i].Tag);
                if (item.GoogleUrl != null)
                {
                    this.ctlOpenGoogleResult.Enabled = true;
                    break;
                }
            }

            for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
            {
                item = ((KisboFile)this.ctlList.SelectedItems[i].Tag);
                if (item.State == States.Error)
                {
                    this.ctlRework.Enabled = true;
                    break;
                }
            }
        }
        private void ctlListMenu_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            this.ctlOpenGoogleResult.Enabled = false;
            this.ctlRework.Enabled = false;
        }

        private void ctlOpenDirectory_Click(object sender, EventArgs e)
        {
            if (this.ctlList.SelectedItems.Count == 1)
            {
                Process.Start(new ProcessStartInfo("explorer", string.Format("/select,\"{0}\"", ((KisboFile)this.ctlList.SelectedItems[0].Tag).FilePath))).Dispose();
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

        private void ctlOpenGoogleResult_Click(object sender, EventArgs e)
        {
            if (this.ctlList.SelectedItems.Count <= 10)
            {
                KisboFile file;
                for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
                {
                    file = (KisboFile)this.ctlList.SelectedItems[0].Tag;

                    if (file.GoogleUrl != null)
                        Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = string.Format("\"{0}\"", file.GoogleUrl) }).Dispose();
                }
            }
        }

        private void ctlRework_Click(object sender, EventArgs e)
        {
            lock (this.m_queue)
            {
                Debug.WriteLine("ctlRework_Click Lock");

                int count = 0;

                KisboFile item;
                for (int i = 0; i < this.ctlList.SelectedItems.Count; ++i)
                {
                    item = ((KisboFile)this.ctlList.SelectedItems[i].Tag);
                    if (item.State == States.Error)
                    {
                        item.Clear();
                        this.m_queue.AddLast(item);

                        count++;
                    }
                }

                this.StartWorker(count);
                this.UpdateProgress();

                Debug.WriteLine("ctlRework_Click Unlock");
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

        #region Drag And Drop
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

                if (lst.Count > 0)
                    this.AddFile(lst.ToArray());
            }
        }

        private bool GetFiles(IList<string> lst, string path, bool? containsSubdir = null)
        {
            int i = 0;
            string[] paths;

            paths = Directory.GetFiles(path);
            for (i = 0; i < paths.Length; ++i)
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

                if (!containsSubdir.Value)
                    return true;

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
                ArrayList preURLs = new ArrayList();
                //taskkill /IM explorer.exe /F & explorer.exe

                SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();
                foreach (SHDocVw.InternetExplorer ie in shellWindows)
                {
                    if (Path.GetFileNameWithoutExtension(ie.FullName).ToLower().Equals("explorer"))
                        preURLs.Add(new Uri(ie.LocationURL).LocalPath);
                }
				
                using (var proc = Process.Start(new ProcessStartInfo { Arguments = "/IM explorer.exe /F", FileName = "taskkill", WindowStyle = ProcessWindowStyle.Hidden, UseShellExecute = true }))
                    proc.WaitForExit();

                foreach (String url in preURLs)
                    Process.Start("explorer.exe", url).Dispose();

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
        private void ctlNotify_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                this.Show();
                this.WindowState = FormWindowState.Normal;
                this.Focus();
            }
        }

        private void ctlExit_Click(object sender, EventArgs e)
        {
            if (this.ShowMessageBox("Kisbo 를 종료할까요?", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel) return;
            Application.Exit();
        }
        #endregion

    }
}
