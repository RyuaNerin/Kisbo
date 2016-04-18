using System.Drawing;
using System.IO;
using System.Windows.Forms;

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

            this.FilePath = filePath;

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
            form.m_files.Add(kisboFile);
            form.m_working.Add(kisboFile);
        }

        public void Remove()
        {
            this.Removed = true;

            this.m_form.m_files.Remove(this);
            this.m_form.m_working.Remove(this);
            this.m_form.ctlList.Items.RemoveAt(this.ListViewItem.Index);
        }

        private readonly SearchWindow m_form;
        private readonly ListViewItem ListViewItem;
        public readonly string FilePath;

        public bool Removed;

        public string GoogleUrl;

        public States State;
        public bool Worked { get { return this.State != States.Wait && this.State != States.Working; } }

        public void SetState(States value)
        {
            this.State = value;
            this.ListViewItem.StateImageIndex = (int)value;
        }
        public void SetBeforeResolution(Size value)
        {
            this.ListViewItem.SubItems[1].Text = string.Format("{0}x{1}", value.Width, value.Height);
        }
        public void SetAfterResolution(Size value)
        {
            this.ListViewItem.SubItems[2].Text = string.Format("{0}x{1}", value.Width, value.Height);
        }
    };
}
