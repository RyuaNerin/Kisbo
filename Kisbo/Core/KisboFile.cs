using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Kisbo.Core
{
    internal enum States
    {
        Wait       = 0, // 대기중
        Working    = 1, // 작업중
        WaitSearch = 2, // 잠시후 다시 검색해주세요
        Complete   = 3, // 성공
        Pass       = 4, // 스킵
        NoResult   = 5, // 검색결과 없음
        Error      = 6, // 에러
    }

    internal class KisboFile
    {
        public KisboFile(SearchWindow form, string filePath)
        {
            this.m_form = form;
            this.m_fileName = filePath;

            this.ListViewItem = new ListViewItem(Path.GetFileName(filePath));
            this.ListViewItem.Tag = this;
            this.ListViewItem.StateImageIndex = 0;
            this.ListViewItem.SubItems.Add("");
            this.ListViewItem.SubItems.Add("");
        }

        public void Clear()
        {
            this.NewFilePath = null;
            this.Removed = false;
            this.GoogleUrl = null;
            this.State = States.Wait;
        }

        private readonly object m_lock = new object();

        private readonly SearchWindow m_form;
        public readonly ListViewItem ListViewItem;

        public readonly string m_fileName;
        public string OriginalFilePath => this.m_fileName;
        public string NewFilePath { get; set; }
        public string FilePath => this.NewFilePath ?? this.m_fileName;

        private bool m_removed;
        public bool Removed
        {
            get { lock (this.m_lock) return this.m_removed; }
            set { lock (this.m_lock) this.m_removed = value; }
        }

        public bool NotTodo { get; set; }

        private string m_googleUrl;
        public string GoogleUrl
        {
            get { lock (this.m_lock) return this.m_googleUrl; }
            set { lock (this.m_lock) this.m_googleUrl = value; }
        }

        private States m_state;
        public States State
        {
            get { lock (this.m_lock) return this.m_state; }
            set
            {
                lock (this.m_lock)
                {
                    this.m_state = value;

                    this.m_form.Invoke(new Action(() => this.ListViewItem.StateImageIndex = (int)value));
                }
            }
        }
        public bool Working { get { lock (this.m_lock) return this.m_state == States.Working  || this.m_state == States.WaitSearch; } }
        public bool Worked  { get { lock (this.m_lock) return this.m_state != States.Wait     && this.m_state != States.Working && this.m_state != States.WaitSearch; } }
        public bool Success { get { lock (this.m_lock) return this.m_state == States.Complete || this.m_state == States.Pass    || this.m_state == States.NoResult; } }

        public void UpdateBeforeResolution(Size value)
        {
            this.m_form.Invoke(new Action(() => this.ListViewItem.SubItems[1].Text = string.Format("{0}x{1}", value.Width, value.Height)));
        }
        public void UpdateAfterResolution(Size value)
        {
            this.m_form.Invoke(new Action(() => this.ListViewItem.SubItems[2].Text = string.Format("{0}x{1}", value.Width, value.Height)));
        }
    };
}
