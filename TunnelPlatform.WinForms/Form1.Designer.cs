namespace TunnelPlatform.WinForms;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private TableLayoutPanel layoutRoot;
    private TableLayoutPanel layoutConfig;
    private FlowLayoutPanel pnlActions;
    private TableLayoutPanel layoutLogs;
    private Label lblApiUrl;
    private TextBox txtApiUrl;
    private Button btnRefreshProjects;
    private Label lblProjectList;
    private ComboBox cboProjects;
    private Button btnDeleteProject;
    private Label lblLedgerPath;
    private TextBox txtLedgerPath;
    private Button btnBrowseLedger;
    private Label lblDataFolder;
    private TextBox txtDataFolder;
    private Button btnBrowseDataFolder;
    private Button btnSyncLedger;
    private Button btnUploadSelected;
    private Button btnUploadAll;
    private Button btnRefreshTree;
    private Button btnDeleteEntity;
    private Label lblSummary;
    private CheckedListBox clbEntities;
    private TreeView tvServerFiles;
    private TextBox txtLogs;
    private ProgressBar progressImport;
    private SplitContainer splitContainerMain;
    private GroupBox grpConfig;
    private GroupBox grpEntities;
    private GroupBox grpFiles;
    private GroupBox grpLogs;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        layoutRoot = new TableLayoutPanel();
        grpConfig = new GroupBox();
        layoutConfig = new TableLayoutPanel();
        lblApiUrl = new Label();
        txtApiUrl = new TextBox();
        btnRefreshProjects = new Button();
        lblProjectList = new Label();
        cboProjects = new ComboBox();
        btnDeleteProject = new Button();
        lblLedgerPath = new Label();
        txtLedgerPath = new TextBox();
        btnBrowseLedger = new Button();
        lblDataFolder = new Label();
        txtDataFolder = new TextBox();
        btnBrowseDataFolder = new Button();
        pnlActions = new FlowLayoutPanel();
        btnSyncLedger = new Button();
        btnUploadSelected = new Button();
        btnUploadAll = new Button();
        btnRefreshTree = new Button();
        btnDeleteEntity = new Button();
        lblSummary = new Label();
        splitContainerMain = new SplitContainer();
        grpEntities = new GroupBox();
        clbEntities = new CheckedListBox();
        grpFiles = new GroupBox();
        tvServerFiles = new TreeView();
        grpLogs = new GroupBox();
        layoutLogs = new TableLayoutPanel();
        progressImport = new ProgressBar();
        txtLogs = new TextBox();
        layoutRoot.SuspendLayout();
        grpConfig.SuspendLayout();
        layoutConfig.SuspendLayout();
        pnlActions.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
        splitContainerMain.Panel1.SuspendLayout();
        splitContainerMain.Panel2.SuspendLayout();
        splitContainerMain.SuspendLayout();
        grpEntities.SuspendLayout();
        grpFiles.SuspendLayout();
        grpLogs.SuspendLayout();
        layoutLogs.SuspendLayout();
        SuspendLayout();
        // 
        // layoutRoot
        // 
        layoutRoot.ColumnCount = 1;
        layoutRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutRoot.Controls.Add(grpConfig, 0, 0);
        layoutRoot.Controls.Add(splitContainerMain, 0, 1);
        layoutRoot.Controls.Add(grpLogs, 0, 2);
        layoutRoot.Dock = DockStyle.Fill;
        layoutRoot.Location = new Point(0, 0);
        layoutRoot.Name = "layoutRoot";
        layoutRoot.Padding = new Padding(13, 11, 13, 11);
        layoutRoot.RowCount = 3;
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 229F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutRoot.RowStyles.Add(new RowStyle(SizeType.Absolute, 172F));
        layoutRoot.Size = new Size(1483, 800);
        layoutRoot.TabIndex = 0;
        // 
        // grpConfig
        // 
        grpConfig.Controls.Add(layoutConfig);
        grpConfig.Dock = DockStyle.Fill;
        grpConfig.Location = new Point(16, 14);
        grpConfig.Name = "grpConfig";
        grpConfig.Padding = new Padding(13, 10, 13, 11);
        grpConfig.Size = new Size(1451, 223);
        grpConfig.TabIndex = 0;
        grpConfig.TabStop = false;
        grpConfig.Text = "基础配置";
        // 
        // layoutConfig
        // 
        layoutConfig.ColumnCount = 6;
        layoutConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 101F));
        layoutConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43F));
        layoutConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 123F));
        layoutConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 101F));
        layoutConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57F));
        layoutConfig.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 135F));
        layoutConfig.Controls.Add(lblApiUrl, 0, 0);
        layoutConfig.Controls.Add(txtApiUrl, 1, 0);
        layoutConfig.Controls.Add(btnRefreshProjects, 2, 0);
        layoutConfig.Controls.Add(lblProjectList, 3, 0);
        layoutConfig.Controls.Add(cboProjects, 4, 0);
        layoutConfig.Controls.Add(btnDeleteProject, 5, 0);
        layoutConfig.Controls.Add(lblLedgerPath, 0, 1);
        layoutConfig.Controls.Add(txtLedgerPath, 1, 1);
        layoutConfig.Controls.Add(btnBrowseLedger, 5, 1);
        layoutConfig.Controls.Add(lblDataFolder, 0, 2);
        layoutConfig.Controls.Add(txtDataFolder, 1, 2);
        layoutConfig.Controls.Add(btnBrowseDataFolder, 5, 2);
        layoutConfig.Controls.Add(pnlActions, 0, 3);
        layoutConfig.Controls.Add(lblSummary, 0, 4);
        layoutConfig.Dock = DockStyle.Fill;
        layoutConfig.Location = new Point(13, 33);
        layoutConfig.Name = "layoutConfig";
        layoutConfig.RowCount = 5;
        layoutConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
        layoutConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
        layoutConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 37F));
        layoutConfig.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        layoutConfig.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutConfig.Size = new Size(1425, 179);
        layoutConfig.TabIndex = 0;
        // 
        // lblApiUrl
        // 
        lblApiUrl.Dock = DockStyle.Fill;
        lblApiUrl.Location = new Point(3, 0);
        lblApiUrl.Name = "lblApiUrl";
        lblApiUrl.Size = new Size(95, 37);
        lblApiUrl.TabIndex = 0;
        lblApiUrl.Text = "API 地址:";
        lblApiUrl.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtApiUrl
        // 
        txtApiUrl.Dock = DockStyle.Fill;
        txtApiUrl.Location = new Point(104, 3);
        txtApiUrl.Name = "txtApiUrl";
        txtApiUrl.Size = new Size(408, 30);
        txtApiUrl.TabIndex = 1;
        txtApiUrl.Text = "http://localhost:5140";
        // 
        // btnRefreshProjects
        // 
        btnRefreshProjects.Dock = DockStyle.Fill;
        btnRefreshProjects.Location = new Point(518, 1);
        btnRefreshProjects.Margin = new Padding(3, 1, 8, 3);
        btnRefreshProjects.Name = "btnRefreshProjects";
        btnRefreshProjects.Size = new Size(112, 33);
        btnRefreshProjects.TabIndex = 2;
        btnRefreshProjects.Text = "刷新实例";
        btnRefreshProjects.UseVisualStyleBackColor = true;
        btnRefreshProjects.Click += btnRefreshProjects_Click;
        // 
        // lblProjectList
        // 
        lblProjectList.Dock = DockStyle.Fill;
        lblProjectList.Location = new Point(641, 0);
        lblProjectList.Name = "lblProjectList";
        lblProjectList.Size = new Size(95, 37);
        lblProjectList.TabIndex = 3;
        lblProjectList.Text = "工程实例:";
        lblProjectList.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // cboProjects
        // 
        cboProjects.Dock = DockStyle.Fill;
        cboProjects.DropDownStyle = ComboBoxStyle.DropDownList;
        cboProjects.FormattingEnabled = true;
        cboProjects.Location = new Point(742, 1);
        cboProjects.Margin = new Padding(3, 1, 3, 3);
        cboProjects.Name = "cboProjects";
        cboProjects.Size = new Size(544, 32);
        cboProjects.TabIndex = 4;
        cboProjects.SelectedIndexChanged += cboProjects_SelectedIndexChanged;
        // 
        // btnDeleteProject
        // 
        btnDeleteProject.Dock = DockStyle.Fill;
        btnDeleteProject.Location = new Point(1294, 1);
        btnDeleteProject.Margin = new Padding(5, 1, 3, 3);
        btnDeleteProject.Name = "btnDeleteProject";
        btnDeleteProject.Size = new Size(128, 33);
        btnDeleteProject.TabIndex = 5;
        btnDeleteProject.Text = "删除实例";
        btnDeleteProject.UseVisualStyleBackColor = true;
        btnDeleteProject.Click += btnDeleteProject_Click;
        // 
        // lblLedgerPath
        // 
        lblLedgerPath.Dock = DockStyle.Fill;
        lblLedgerPath.Location = new Point(3, 37);
        lblLedgerPath.Name = "lblLedgerPath";
        lblLedgerPath.Size = new Size(95, 37);
        lblLedgerPath.TabIndex = 6;
        lblLedgerPath.Text = "台账文件:";
        lblLedgerPath.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtLedgerPath
        // 
        layoutConfig.SetColumnSpan(txtLedgerPath, 4);
        txtLedgerPath.Dock = DockStyle.Fill;
        txtLedgerPath.Location = new Point(104, 40);
        txtLedgerPath.Name = "txtLedgerPath";
        txtLedgerPath.Size = new Size(1182, 30);
        txtLedgerPath.TabIndex = 7;
        // 
        // btnBrowseLedger
        // 
        btnBrowseLedger.Dock = DockStyle.Fill;
        btnBrowseLedger.Location = new Point(1294, 38);
        btnBrowseLedger.Margin = new Padding(5, 1, 3, 3);
        btnBrowseLedger.Name = "btnBrowseLedger";
        btnBrowseLedger.Size = new Size(128, 33);
        btnBrowseLedger.TabIndex = 8;
        btnBrowseLedger.Text = "选择台账";
        btnBrowseLedger.UseVisualStyleBackColor = true;
        btnBrowseLedger.Click += btnBrowseLedger_Click;
        // 
        // lblDataFolder
        // 
        lblDataFolder.Dock = DockStyle.Fill;
        lblDataFolder.Location = new Point(3, 74);
        lblDataFolder.Name = "lblDataFolder";
        lblDataFolder.Size = new Size(95, 37);
        lblDataFolder.TabIndex = 9;
        lblDataFolder.Text = "数据目录:";
        lblDataFolder.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // txtDataFolder
        // 
        layoutConfig.SetColumnSpan(txtDataFolder, 4);
        txtDataFolder.Dock = DockStyle.Fill;
        txtDataFolder.Location = new Point(104, 77);
        txtDataFolder.Name = "txtDataFolder";
        txtDataFolder.Size = new Size(1182, 30);
        txtDataFolder.TabIndex = 10;
        // 
        // btnBrowseDataFolder
        // 
        btnBrowseDataFolder.Dock = DockStyle.Fill;
        btnBrowseDataFolder.Location = new Point(1294, 75);
        btnBrowseDataFolder.Margin = new Padding(5, 1, 3, 3);
        btnBrowseDataFolder.Name = "btnBrowseDataFolder";
        btnBrowseDataFolder.Size = new Size(128, 33);
        btnBrowseDataFolder.TabIndex = 11;
        btnBrowseDataFolder.Text = "选择目录";
        btnBrowseDataFolder.UseVisualStyleBackColor = true;
        btnBrowseDataFolder.Click += btnBrowseDataFolder_Click;
        // 
        // pnlActions
        // 
        layoutConfig.SetColumnSpan(pnlActions, 6);
        pnlActions.Controls.Add(btnSyncLedger);
        pnlActions.Controls.Add(btnUploadSelected);
        pnlActions.Controls.Add(btnUploadAll);
        pnlActions.Controls.Add(btnRefreshTree);
        pnlActions.Controls.Add(btnDeleteEntity);
        pnlActions.Dock = DockStyle.Fill;
        pnlActions.Location = new Point(0, 111);
        pnlActions.Margin = new Padding(0);
        pnlActions.Name = "pnlActions";
        pnlActions.Padding = new Padding(0, 4, 0, 0);
        pnlActions.Size = new Size(1425, 42);
        pnlActions.TabIndex = 12;
        pnlActions.WrapContents = false;
        // 
        // btnSyncLedger
        // 
        btnSyncLedger.Location = new Point(3, 7);
        btnSyncLedger.Name = "btnSyncLedger";
        btnSyncLedger.Size = new Size(123, 31);
        btnSyncLedger.TabIndex = 0;
        btnSyncLedger.Text = "同步台账";
        btnSyncLedger.UseVisualStyleBackColor = true;
        btnSyncLedger.Click += btnSyncLedger_Click;
        // 
        // btnUploadSelected
        // 
        btnUploadSelected.Location = new Point(138, 7);
        btnUploadSelected.Margin = new Padding(9, 3, 3, 3);
        btnUploadSelected.Name = "btnUploadSelected";
        btnUploadSelected.Size = new Size(146, 31);
        btnUploadSelected.TabIndex = 1;
        btnUploadSelected.Text = "上传勾选实体";
        btnUploadSelected.UseVisualStyleBackColor = true;
        btnUploadSelected.Click += btnUploadSelected_Click;
        // 
        // btnUploadAll
        // 
        btnUploadAll.Location = new Point(296, 7);
        btnUploadAll.Margin = new Padding(9, 3, 3, 3);
        btnUploadAll.Name = "btnUploadAll";
        btnUploadAll.Size = new Size(159, 31);
        btnUploadAll.TabIndex = 2;
        btnUploadAll.Text = "上传全部可用";
        btnUploadAll.UseVisualStyleBackColor = true;
        btnUploadAll.Click += btnUploadAll_Click;
        // 
        // btnRefreshTree
        // 
        btnRefreshTree.Location = new Point(467, 7);
        btnRefreshTree.Margin = new Padding(9, 3, 3, 3);
        btnRefreshTree.Name = "btnRefreshTree";
        btnRefreshTree.Size = new Size(124, 31);
        btnRefreshTree.TabIndex = 3;
        btnRefreshTree.Text = "刷新文件树";
        btnRefreshTree.UseVisualStyleBackColor = true;
        btnRefreshTree.Click += btnRefreshTree_Click;
        // 
        // btnDeleteEntity
        // 
        btnDeleteEntity.Location = new Point(603, 7);
        btnDeleteEntity.Margin = new Padding(9, 3, 3, 3);
        btnDeleteEntity.Name = "btnDeleteEntity";
        btnDeleteEntity.Size = new Size(146, 31);
        btnDeleteEntity.TabIndex = 4;
        btnDeleteEntity.Text = "删除站点/区间";
        btnDeleteEntity.UseVisualStyleBackColor = true;
        btnDeleteEntity.Click += btnDeleteEntity_Click;
        // 
        // lblSummary
        // 
        layoutConfig.SetColumnSpan(lblSummary, 6);
        lblSummary.Dock = DockStyle.Fill;
        lblSummary.Location = new Point(3, 153);
        lblSummary.Name = "lblSummary";
        lblSummary.Size = new Size(1419, 26);
        lblSummary.TabIndex = 13;
        lblSummary.Text = "请选择 API 地址、台账文件、数据目录，然后同步台账。";
        lblSummary.TextAlign = ContentAlignment.MiddleLeft;
        // 
        // splitContainerMain
        // 
        splitContainerMain.Dock = DockStyle.Fill;
        splitContainerMain.Location = new Point(16, 243);
        splitContainerMain.Name = "splitContainerMain";
        // 
        // splitContainerMain.Panel1
        // 
        splitContainerMain.Panel1.Controls.Add(grpEntities);
        // 
        // splitContainerMain.Panel2
        // 
        splitContainerMain.Panel2.Controls.Add(grpFiles);
        splitContainerMain.Size = new Size(1451, 371);
        splitContainerMain.SplitterDistance = 515;
        splitContainerMain.SplitterWidth = 5;
        splitContainerMain.TabIndex = 1;
        // 
        // grpEntities
        // 
        grpEntities.Controls.Add(clbEntities);
        grpEntities.Dock = DockStyle.Fill;
        grpEntities.Location = new Point(0, 0);
        grpEntities.Name = "grpEntities";
        grpEntities.Padding = new Padding(8);
        grpEntities.Size = new Size(515, 371);
        grpEntities.TabIndex = 0;
        grpEntities.TabStop = false;
        grpEntities.Text = "站点 / 区间列表";
        // 
        // clbEntities
        // 
        clbEntities.CheckOnClick = true;
        clbEntities.Dock = DockStyle.Fill;
        clbEntities.FormattingEnabled = true;
        clbEntities.HorizontalScrollbar = true;
        clbEntities.Location = new Point(8, 31);
        clbEntities.Name = "clbEntities";
        clbEntities.Size = new Size(499, 332);
        clbEntities.TabIndex = 0;
        clbEntities.SelectedIndexChanged += clbEntities_SelectedIndexChanged;
        // 
        // grpFiles
        // 
        grpFiles.Controls.Add(tvServerFiles);
        grpFiles.Dock = DockStyle.Fill;
        grpFiles.Location = new Point(0, 0);
        grpFiles.Name = "grpFiles";
        grpFiles.Padding = new Padding(8);
        grpFiles.Size = new Size(931, 371);
        grpFiles.TabIndex = 0;
        grpFiles.TabStop = false;
        grpFiles.Text = "服务器文件树";
        // 
        // tvServerFiles
        // 
        tvServerFiles.Dock = DockStyle.Fill;
        tvServerFiles.HideSelection = false;
        tvServerFiles.Location = new Point(8, 31);
        tvServerFiles.Name = "tvServerFiles";
        tvServerFiles.Size = new Size(915, 332);
        tvServerFiles.TabIndex = 0;
        // 
        // grpLogs
        // 
        grpLogs.Controls.Add(layoutLogs);
        grpLogs.Dock = DockStyle.Fill;
        grpLogs.Location = new Point(16, 620);
        grpLogs.Name = "grpLogs";
        grpLogs.Padding = new Padding(13, 8, 13, 11);
        grpLogs.Size = new Size(1451, 166);
        grpLogs.TabIndex = 2;
        grpLogs.TabStop = false;
        grpLogs.Text = "执行信息";
        // 
        // layoutLogs
        // 
        layoutLogs.ColumnCount = 1;
        layoutLogs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layoutLogs.Controls.Add(progressImport, 0, 0);
        layoutLogs.Controls.Add(txtLogs, 0, 1);
        layoutLogs.Dock = DockStyle.Fill;
        layoutLogs.Location = new Point(13, 31);
        layoutLogs.Name = "layoutLogs";
        layoutLogs.RowCount = 2;
        layoutLogs.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layoutLogs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layoutLogs.Size = new Size(1425, 124);
        layoutLogs.TabIndex = 0;
        // 
        // progressImport
        // 
        progressImport.Dock = DockStyle.Fill;
        progressImport.Location = new Point(3, 3);
        progressImport.Name = "progressImport";
        progressImport.Size = new Size(1419, 24);
        progressImport.TabIndex = 0;
        // 
        // txtLogs
        // 
        txtLogs.Dock = DockStyle.Fill;
        txtLogs.Location = new Point(3, 33);
        txtLogs.Multiline = true;
        txtLogs.Name = "txtLogs";
        txtLogs.ReadOnly = true;
        txtLogs.ScrollBars = ScrollBars.Vertical;
        txtLogs.Size = new Size(1419, 88);
        txtLogs.TabIndex = 1;
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(11F, 24F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1483, 800);
        Controls.Add(layoutRoot);
        MinimumSize = new Size(1177, 753);
        Name = "Form1";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "隧道平台数据上传工具";
        layoutRoot.ResumeLayout(false);
        grpConfig.ResumeLayout(false);
        layoutConfig.ResumeLayout(false);
        layoutConfig.PerformLayout();
        pnlActions.ResumeLayout(false);
        splitContainerMain.Panel1.ResumeLayout(false);
        splitContainerMain.Panel2.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
        splitContainerMain.ResumeLayout(false);
        grpEntities.ResumeLayout(false);
        grpFiles.ResumeLayout(false);
        grpLogs.ResumeLayout(false);
        layoutLogs.ResumeLayout(false);
        layoutLogs.PerformLayout();
        ResumeLayout(false);
    }

    #endregion
}
