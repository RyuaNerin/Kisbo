using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace Kisbo.Core
{
    internal enum States
    {
        Wait = 0,
        Working = 1,
        Complete = 2,
        Pass = 3,
        NoResult = 4,
        Error = 5,
    }

    internal class KisboFile
    {
        private KisboFile(SearchWindow form, string filePath)
        {
            this.m_form = form;
            this.m_fileName = filePath;

            this.ListViewItem = new ListViewItem(Path.GetFileName(filePath));
            this.ListViewItem.Tag = this;
            this.ListViewItem.StateImageIndex = 0;
            this.ListViewItem.SubItems.Add("");
            this.ListViewItem.SubItems.Add("");
        }

        public static void Create(SearchWindow form, string filePath)
        {
            var kisboFile = new KisboFile(form, filePath);

            form.ctlList.Items.Add(kisboFile.ListViewItem);
            form.m_list.Add(kisboFile);
        }

        public void Remove()
        {
            this.Removed = true;

            this.m_form.ctlList.Items.RemoveAt(this.ListViewItem.Index);
            
            this.m_form.m_list.Remove(this);

            if (!this.NotTodo)
            {
                Interlocked.Decrement(ref this.m_form.m_taskbarMax);

                if (this.Worked)
                    Interlocked.Decrement(ref this.m_form.m_taskbarVal);
            }
        }

        private readonly object m_lock = new object();

        private readonly SearchWindow m_form;
        public readonly ListViewItem ListViewItem;

        public readonly string m_fileName;
        public string FilePath { get { return this.m_fileName; } }

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
            set { lock (this.m_lock) this.m_state = value; }
        }
        public bool Success { get { return this.State == States.Complete || this.State == States.Pass || this.State == States.NoResult; } }
        public bool Worked { get { return this.State != States.Wait && this.State != States.Working; } }

        public void SetStatus()
        {
            this.m_form.Invoke(new Action(() => this.ListViewItem.StateImageIndex = (int)this.State));
        }
        public void SetBeforeResolution(Size value)
        {
            this.m_form.Invoke(new Action(() => this.ListViewItem.SubItems[1].Text = string.Format("{0}x{1}", value.Width, value.Height)));
        }
        public void SetAfterResolution(Size value)
        {
            this.m_form.Invoke(new Action(() => this.ListViewItem.SubItems[2].Text = string.Format("{0}x{1}", value.Width, value.Height)));
        }
    };
}
