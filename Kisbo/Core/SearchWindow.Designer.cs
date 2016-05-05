namespace Kisbo.Core
{
    partial class SearchWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SearchWindow));
            this.ctlList = new System.Windows.Forms.ListView();
            this.ctlList0 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ctlList1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ctlList2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ctlListMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ctlOpenDirectory = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlOpenGoogleResult = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.ctlRework = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlListState = new System.Windows.Forms.ImageList(this.components);
            this.ctlOpenFile = new System.Windows.Forms.OpenFileDialog();
            this.ctlMenu = new System.Windows.Forms.MenuStrip();
            this.ctlExit0 = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlPause = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlAdd = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlAddFile = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlAddDir = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlMenuList = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlRemoveSelected = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlRemoveCompleted = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlSep0 = new System.Windows.Forms.ToolStripSeparator();
            this.ctlClearAll = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlTopMost = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.ctlInstallContextMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlUninstallContextMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.ctlCopyRight = new System.Windows.Forms.Label();
            this.ctlOpenDir = new System.Windows.Forms.FolderBrowserDialog();
            this.ctlProgress = new System.Windows.Forms.Label();
            this.ctlNotify = new System.Windows.Forms.NotifyIcon(this.components);
            this.ctlNofityMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.ctlExit = new System.Windows.Forms.ToolStripMenuItem();
            this.ttp = new System.Windows.Forms.ToolTip(this.components);
            this.ctlListMenu.SuspendLayout();
            this.ctlMenu.SuspendLayout();
            this.ctlNofityMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // ctlList
            // 
            this.ctlList.AllowDrop = true;
            this.ctlList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ctlList0,
            this.ctlList1,
            this.ctlList2});
            this.ctlList.ContextMenuStrip = this.ctlListMenu;
            this.ctlList.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctlList.FullRowSelect = true;
            this.ctlList.HideSelection = false;
            this.ctlList.LabelWrap = false;
            this.ctlList.Location = new System.Drawing.Point(12, 27);
            this.ctlList.Name = "ctlList";
            this.ctlList.Size = new System.Drawing.Size(296, 200);
            this.ctlList.StateImageList = this.ctlListState;
            this.ctlList.TabIndex = 0;
            this.ctlList.UseCompatibleStateImageBehavior = false;
            this.ctlList.View = System.Windows.Forms.View.Details;
            this.ctlList.DragDrop += new System.Windows.Forms.DragEventHandler(this.ctlList_DragDrop);
            this.ctlList.DragEnter += new System.Windows.Forms.DragEventHandler(this.ctlList_DragEnter);
            this.ctlList.KeyUp += new System.Windows.Forms.KeyEventHandler(this.ctlList_KeyUp);
            this.ctlList.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.ctlList_MouseDoubleClick);
            // 
            // ctlList0
            // 
            this.ctlList0.Text = "파일 이름";
            this.ctlList0.Width = 100;
            // 
            // ctlList1
            // 
            this.ctlList1.Text = "이전";
            this.ctlList1.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ctlList1.Width = 85;
            // 
            // ctlList2
            // 
            this.ctlList2.Text = "이후";
            this.ctlList2.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            this.ctlList2.Width = 85;
            // 
            // ctlListMenu
            // 
            this.ctlListMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctlOpenDirectory,
            this.ctlOpenGoogleResult,
            this.toolStripSeparator1,
            this.ctlRework});
            this.ctlListMenu.Name = "ctlListMenu";
            this.ctlListMenu.Size = new System.Drawing.Size(236, 76);
            this.ctlListMenu.Closing += new System.Windows.Forms.ToolStripDropDownClosingEventHandler(this.ctlListMenu_Closing);
            this.ctlListMenu.Opening += new System.ComponentModel.CancelEventHandler(this.ctlListMenu_Opening);
            // 
            // ctlOpenDirectory
            // 
            this.ctlOpenDirectory.Name = "ctlOpenDirectory";
            this.ctlOpenDirectory.Size = new System.Drawing.Size(235, 22);
            this.ctlOpenDirectory.Text = "폴더 열기";
            this.ctlOpenDirectory.Click += new System.EventHandler(this.ctlOpenDirectory_Click);
            // 
            // ctlOpenGoogleResult
            // 
            this.ctlOpenGoogleResult.Name = "ctlOpenGoogleResult";
            this.ctlOpenGoogleResult.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.W)));
            this.ctlOpenGoogleResult.Size = new System.Drawing.Size(235, 22);
            this.ctlOpenGoogleResult.Text = "이미지 검색결과 열기";
            this.ctlOpenGoogleResult.Click += new System.EventHandler(this.ctlOpenGoogleResult_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(232, 6);
            // 
            // ctlRework
            // 
            this.ctlRework.Name = "ctlRework";
            this.ctlRework.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.R)));
            this.ctlRework.Size = new System.Drawing.Size(235, 22);
            this.ctlRework.Text = "재시도";
            this.ctlRework.Click += new System.EventHandler(this.ctlRework_Click);
            // 
            // ctlListState
            // 
            this.ctlListState.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ctlListState.ImageStream")));
            this.ctlListState.TransparentColor = System.Drawing.Color.Transparent;
            this.ctlListState.Images.SetKeyName(0, "");
            this.ctlListState.Images.SetKeyName(1, "");
            this.ctlListState.Images.SetKeyName(2, "");
            this.ctlListState.Images.SetKeyName(3, "");
            this.ctlListState.Images.SetKeyName(4, "");
            this.ctlListState.Images.SetKeyName(5, "");
            this.ctlListState.Images.SetKeyName(6, "");
            // 
            // ctlOpenFile
            // 
            this.ctlOpenFile.Filter = "이미지 파일|*.bmp;*.jpg;*.jpeg;*.png;*.gif";
            // 
            // ctlMenu
            // 
            this.ctlMenu.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctlMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctlExit0,
            this.ctlPause,
            this.ctlAdd,
            this.ctlMenuList,
            this.ctlSettings});
            this.ctlMenu.Location = new System.Drawing.Point(0, 0);
            this.ctlMenu.Name = "ctlMenu";
            this.ctlMenu.Size = new System.Drawing.Size(320, 24);
            this.ctlMenu.TabIndex = 1;
            this.ctlMenu.Text = "menuStrip1";
            // 
            // ctlExit0
            // 
            this.ctlExit0.Name = "ctlExit0";
            this.ctlExit0.Size = new System.Drawing.Size(43, 20);
            this.ctlExit0.Text = "종료";
            this.ctlExit0.Click += new System.EventHandler(this.ctlExit_Click);
            // 
            // ctlPause
            // 
            this.ctlPause.AutoSize = false;
            this.ctlPause.CheckOnClick = true;
            this.ctlPause.Name = "ctlPause";
            this.ctlPause.Size = new System.Drawing.Size(70, 20);
            this.ctlPause.Text = "일시정지";
            this.ctlPause.Click += new System.EventHandler(this.ctlPause_Click);
            // 
            // ctlAdd
            // 
            this.ctlAdd.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctlAddFile,
            this.ctlAddDir});
            this.ctlAdd.Name = "ctlAdd";
            this.ctlAdd.Size = new System.Drawing.Size(43, 20);
            this.ctlAdd.Text = "추가";
            // 
            // ctlAddFile
            // 
            this.ctlAddFile.Name = "ctlAddFile";
            this.ctlAddFile.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.A)));
            this.ctlAddFile.Size = new System.Drawing.Size(193, 22);
            this.ctlAddFile.Text = "파일";
            this.ctlAddFile.Click += new System.EventHandler(this.ctlAddFile_Click);
            // 
            // ctlAddDir
            // 
            this.ctlAddDir.Name = "ctlAddDir";
            this.ctlAddDir.ShortcutKeys = ((System.Windows.Forms.Keys)(((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.Shift) 
            | System.Windows.Forms.Keys.A)));
            this.ctlAddDir.Size = new System.Drawing.Size(193, 22);
            this.ctlAddDir.Text = "디렉토리";
            this.ctlAddDir.Click += new System.EventHandler(this.ctlAddDir_Click);
            // 
            // ctlMenuList
            // 
            this.ctlMenuList.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctlRemoveSelected,
            this.ctlRemoveCompleted,
            this.ctlSep0,
            this.ctlClearAll});
            this.ctlMenuList.Name = "ctlMenuList";
            this.ctlMenuList.Size = new System.Drawing.Size(43, 20);
            this.ctlMenuList.Text = "목록";
            // 
            // ctlRemoveSelected
            // 
            this.ctlRemoveSelected.Name = "ctlRemoveSelected";
            this.ctlRemoveSelected.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.ctlRemoveSelected.Size = new System.Drawing.Size(258, 22);
            this.ctlRemoveSelected.Text = "선택한 파일 삭제";
            this.ctlRemoveSelected.Click += new System.EventHandler(this.ctlRemoveSelected_Click);
            // 
            // ctlRemoveCompleted
            // 
            this.ctlRemoveCompleted.Name = "ctlRemoveCompleted";
            this.ctlRemoveCompleted.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.Delete)));
            this.ctlRemoveCompleted.Size = new System.Drawing.Size(258, 22);
            this.ctlRemoveCompleted.Text = "다운로드 끝난 파일 삭제";
            this.ctlRemoveCompleted.Click += new System.EventHandler(this.ctlRemoveCompleted_Click);
            // 
            // ctlSep0
            // 
            this.ctlSep0.Name = "ctlSep0";
            this.ctlSep0.Size = new System.Drawing.Size(255, 6);
            // 
            // ctlClearAll
            // 
            this.ctlClearAll.Name = "ctlClearAll";
            this.ctlClearAll.Size = new System.Drawing.Size(258, 22);
            this.ctlClearAll.Text = "목록 비우기";
            this.ctlClearAll.Click += new System.EventHandler(this.ctlClearAll_Click);
            // 
            // ctlSettings
            // 
            this.ctlSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctlTopMost,
            this.ctlSep1,
            this.ctlInstallContextMenu,
            this.ctlUninstallContextMenu});
            this.ctlSettings.Name = "ctlSettings";
            this.ctlSettings.Size = new System.Drawing.Size(43, 20);
            this.ctlSettings.Text = "설정";
            // 
            // ctlTopMost
            // 
            this.ctlTopMost.CheckOnClick = true;
            this.ctlTopMost.Name = "ctlTopMost";
            this.ctlTopMost.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.M)));
            this.ctlTopMost.Size = new System.Drawing.Size(190, 22);
            this.ctlTopMost.Text = "맨 위로";
            this.ctlTopMost.CheckedChanged += new System.EventHandler(this.ctlTopMost_CheckedChanged);
            // 
            // ctlSep1
            // 
            this.ctlSep1.Name = "ctlSep1";
            this.ctlSep1.Size = new System.Drawing.Size(187, 6);
            // 
            // ctlInstallContextMenu
            // 
            this.ctlInstallContextMenu.Name = "ctlInstallContextMenu";
            this.ctlInstallContextMenu.Size = new System.Drawing.Size(190, 22);
            this.ctlInstallContextMenu.Text = "우클릭 메뉴에 추가";
            this.ctlInstallContextMenu.Click += new System.EventHandler(this.ctlInstallContextMenu_Click);
            // 
            // ctlUninstallContextMenu
            // 
            this.ctlUninstallContextMenu.Name = "ctlUninstallContextMenu";
            this.ctlUninstallContextMenu.Size = new System.Drawing.Size(190, 22);
            this.ctlUninstallContextMenu.Text = "우클릭 메뉴에서 삭제";
            this.ctlUninstallContextMenu.Click += new System.EventHandler(this.ctlUninstallContextMenu_Click);
            // 
            // ctlCopyRight
            // 
            this.ctlCopyRight.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlCopyRight.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ctlCopyRight.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctlCopyRight.Location = new System.Drawing.Point(168, 230);
            this.ctlCopyRight.Name = "ctlCopyRight";
            this.ctlCopyRight.Size = new System.Drawing.Size(140, 14);
            this.ctlCopyRight.TabIndex = 2;
            this.ctlCopyRight.Text = "Made by RyuaNerin";
            this.ctlCopyRight.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ctlCopyRight.DoubleClick += new System.EventHandler(this.ctlCopyRight_DoubleClick);
            // 
            // ctlOpenDir
            // 
            this.ctlOpenDir.ShowNewFolderButton = false;
            // 
            // ctlProgress
            // 
            this.ctlProgress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.ctlProgress.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ctlProgress.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ctlProgress.Location = new System.Drawing.Point(12, 230);
            this.ctlProgress.Name = "ctlProgress";
            this.ctlProgress.Size = new System.Drawing.Size(119, 14);
            this.ctlProgress.TabIndex = 3;
            this.ctlProgress.Text = "0 / 0";
            this.ctlProgress.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // ctlNotify
            // 
            this.ctlNotify.ContextMenuStrip = this.ctlNofityMenu;
            this.ctlNotify.Visible = true;
            this.ctlNotify.MouseClick += new System.Windows.Forms.MouseEventHandler(this.ctlNotify_MouseClick);
            // 
            // ctlNofityMenu
            // 
            this.ctlNofityMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ctlExit});
            this.ctlNofityMenu.Name = "ctlNofityMenu";
            this.ctlNofityMenu.Size = new System.Drawing.Size(99, 26);
            // 
            // ctlExit
            // 
            this.ctlExit.Name = "ctlExit";
            this.ctlExit.Size = new System.Drawing.Size(98, 22);
            this.ctlExit.Text = "종료";
            this.ctlExit.Click += new System.EventHandler(this.ctlExit_Click);
            // 
            // SearchWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(320, 253);
            this.Controls.Add(this.ctlProgress);
            this.Controls.Add(this.ctlCopyRight);
            this.Controls.Add(this.ctlList);
            this.Controls.Add(this.ctlMenu);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.MainMenuStrip = this.ctlMenu;
            this.MinimumSize = new System.Drawing.Size(336, 291);
            this.Name = "SearchWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Kisbo";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SearchWindow_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SearchWindow_FormClosed);
            this.Shown += new System.EventHandler(this.SearchWindow_Shown);
            this.ctlListMenu.ResumeLayout(false);
            this.ctlMenu.ResumeLayout(false);
            this.ctlMenu.PerformLayout();
            this.ctlNofityMenu.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ColumnHeader ctlList0;
        private System.Windows.Forms.ColumnHeader ctlList1;
        private System.Windows.Forms.ColumnHeader ctlList2;
        private System.Windows.Forms.OpenFileDialog ctlOpenFile;
        private System.Windows.Forms.MenuStrip ctlMenu;
        private System.Windows.Forms.ToolStripMenuItem ctlAdd;
        private System.Windows.Forms.ToolStripMenuItem ctlAddFile;
        private System.Windows.Forms.ToolStripMenuItem ctlAddDir;
        private System.Windows.Forms.ToolStripMenuItem ctlMenuList;
        private System.Windows.Forms.ToolStripMenuItem ctlRemoveSelected;
        private System.Windows.Forms.ToolStripMenuItem ctlRemoveCompleted;
        private System.Windows.Forms.ToolStripSeparator ctlSep0;
        private System.Windows.Forms.ToolStripMenuItem ctlClearAll;
        public System.Windows.Forms.ListView ctlList;
        private System.Windows.Forms.ContextMenuStrip ctlListMenu;
        private System.Windows.Forms.ToolStripMenuItem ctlOpenDirectory;
        private System.Windows.Forms.ToolStripMenuItem ctlOpenGoogleResult;
        private System.Windows.Forms.Label ctlCopyRight;
        private System.Windows.Forms.ToolStripMenuItem ctlPause;
        private System.Windows.Forms.ImageList ctlListState;
        private System.Windows.Forms.ToolStripMenuItem ctlSettings;
        private System.Windows.Forms.ToolStripMenuItem ctlUninstallContextMenu;
        private System.Windows.Forms.FolderBrowserDialog ctlOpenDir;
        private System.Windows.Forms.ToolStripMenuItem ctlInstallContextMenu;
        private System.Windows.Forms.Label ctlProgress;
        private System.Windows.Forms.ToolStripMenuItem ctlTopMost;
        private System.Windows.Forms.ToolStripSeparator ctlSep1;
        private System.Windows.Forms.NotifyIcon ctlNotify;
        private System.Windows.Forms.ContextMenuStrip ctlNofityMenu;
        private System.Windows.Forms.ToolStripMenuItem ctlExit;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem ctlRework;
        private System.Windows.Forms.ToolTip ttp;
        private System.Windows.Forms.ToolStripMenuItem ctlExit0;

    }
}
