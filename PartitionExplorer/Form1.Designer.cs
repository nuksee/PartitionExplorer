namespace PartitionExplorer
{
    partial class Form1
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.Series series1 = new System.Windows.Forms.DataVisualization.Charting.Series();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label1 = new System.Windows.Forms.Label();
            this.cmbPartition = new System.Windows.Forms.ComboBox();
            this.lvCurrentFolder = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.imageList2 = new System.Windows.Forms.ImageList(this.components);
            this.chbShowFreeSpace = new System.Windows.Forms.CheckBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.cmbViews = new System.Windows.Forms.ComboBox();
            this.lblPath = new System.Windows.Forms.TextBox();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressScan = new System.Windows.Forms.ToolStripProgressBar();
            this.lnkCancel = new System.Windows.Forms.ToolStripStatusLabel();
            this.columnHeader3 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader4 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader5 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.ctxItem = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuShowInExplorer = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuCopyPath = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuDelete = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuProperties = new System.Windows.Forms.ToolStripMenuItem();
            this.btnBack = new System.Windows.Forms.Button();
            this.btnForward = new System.Windows.Forms.Button();
            this.chbExplorerMenu = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.ctxItem.SuspendLayout();
            this.SuspendLayout();
            // 
            // chart1
            // 
            this.chart1.BackColor = System.Drawing.Color.Transparent;
            this.chart1.BackHatchStyle = System.Windows.Forms.DataVisualization.Charting.ChartHatchStyle.BackwardDiagonal;
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            this.chart1.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.LegendItemOrder = System.Windows.Forms.DataVisualization.Charting.LegendItemOrder.SameAsSeriesOrder;
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(0, 0);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Doughnut;
            series1.CustomProperties = "PieLineColor=Black, PieDrawingStyle=Concave, PieLabelStyle=Outside, LabelsRadialLin" +
                "eSize=0";
            series1.IsValueShownAsLabel = true;
            series1.Legend = "Legend1";
            series1.MarkerStyle = System.Windows.Forms.DataVisualization.Charting.MarkerStyle.Square;
            series1.Name = "Series1";
            series1.SmartLabelStyle.AllowOutsidePlotArea = System.Windows.Forms.DataVisualization.Charting.LabelOutsidePlotAreaStyle.Yes;
            series1.ToolTip = "#VAL";
            series1.YValuesPerPoint = 2;
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(520, 507);
            this.chart1.TabIndex = 0;
            this.chart1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.chart1_MouseClick);
            this.chart1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.chart1_MouseMove);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(48, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Partition:";
            // 
            // cmbPartition
            // 
            this.cmbPartition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPartition.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbPartition.FormattingEnabled = true;
            this.cmbPartition.Location = new System.Drawing.Point(66, 6);
            this.cmbPartition.Name = "cmbPartition";
            this.cmbPartition.Size = new System.Drawing.Size(121, 21);
            this.cmbPartition.TabIndex = 2;
            this.cmbPartition.SelectedIndexChanged += new System.EventHandler(this.cmbPartition_SelectedIndexChanged);
            // 
            // lvCurrentFolder
            // 
            this.lvCurrentFolder.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.lvCurrentFolder.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5});
            this.lvCurrentFolder.ContextMenuStrip = this.ctxItem;
            this.lvCurrentFolder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvCurrentFolder.LargeImageList = this.imageList1;
            this.lvCurrentFolder.Location = new System.Drawing.Point(0, 0);
            this.lvCurrentFolder.MultiSelect = false;
            this.lvCurrentFolder.Name = "lvCurrentFolder";
            this.lvCurrentFolder.Size = new System.Drawing.Size(262, 507);
            this.lvCurrentFolder.ShowItemToolTips = true;
            this.lvCurrentFolder.SmallImageList = this.imageList2;
            this.lvCurrentFolder.TabIndex = 1;
            this.lvCurrentFolder.UseCompatibleStateImageBehavior = false;
            this.lvCurrentFolder.DoubleClick += new System.EventHandler(this.lvCurrentFolder_DoubleClick);
            this.lvCurrentFolder.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lvCurrentFolder_KeyDown);
            this.lvCurrentFolder.SelectedIndexChanged += new System.EventHandler(this.lvCurrentFolder_SelectedIndexChanged);
            this.lvCurrentFolder.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.lvCurrentFolder_ColumnClick);
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Name";
            this.columnHeader1.Width = 190;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Size";
            this.columnHeader2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader2.Width = 75;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "%";
            this.columnHeader3.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader3.Width = 55;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Items";
            this.columnHeader4.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.columnHeader4.Width = 65;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Modified";
            this.columnHeader5.Width = 110;
            // 
            // ctxItem
            // 
            this.ctxItem.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuOpen,
            this.mnuShowInExplorer,
            this.mnuCopyPath,
            this.toolStripSeparator1,
            this.mnuDelete,
            this.toolStripSeparator2,
            this.mnuProperties});
            this.ctxItem.Name = "ctxItem";
            this.ctxItem.Size = new System.Drawing.Size(181, 126);
            this.ctxItem.Opening += new System.ComponentModel.CancelEventHandler(this.ctxItem_Opening);
            // 
            // mnuOpen
            // 
            this.mnuOpen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.mnuOpen.Name = "mnuOpen";
            this.mnuOpen.Size = new System.Drawing.Size(180, 22);
            this.mnuOpen.Text = "Open";
            this.mnuOpen.Click += new System.EventHandler(this.mnuOpen_Click);
            // 
            // mnuShowInExplorer
            // 
            this.mnuShowInExplorer.Name = "mnuShowInExplorer";
            this.mnuShowInExplorer.Size = new System.Drawing.Size(180, 22);
            this.mnuShowInExplorer.Text = "Show in Explorer";
            this.mnuShowInExplorer.Click += new System.EventHandler(this.mnuShowInExplorer_Click);
            // 
            // mnuCopyPath
            // 
            this.mnuCopyPath.Name = "mnuCopyPath";
            this.mnuCopyPath.Size = new System.Drawing.Size(180, 22);
            this.mnuCopyPath.Text = "Copy path";
            this.mnuCopyPath.Click += new System.EventHandler(this.mnuCopyPath_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
            // 
            // mnuDelete
            // 
            this.mnuDelete.Name = "mnuDelete";
            this.mnuDelete.ShortcutKeyDisplayString = "Del";
            this.mnuDelete.Size = new System.Drawing.Size(180, 22);
            this.mnuDelete.Text = "Delete to Recycle Bin";
            this.mnuDelete.Click += new System.EventHandler(this.mnuDelete_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(177, 6);
            // 
            // mnuProperties
            // 
            this.mnuProperties.Name = "mnuProperties";
            this.mnuProperties.Size = new System.Drawing.Size(180, 22);
            this.mnuProperties.Text = "Properties";
            this.mnuProperties.Click += new System.EventHandler(this.mnuProperties_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "folder.ico");
            this.imageList1.Images.SetKeyName(1, "DOCL.ICO");
            // 
            // imageList2
            // 
            this.imageList2.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList2.ImageStream")));
            this.imageList2.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList2.Images.SetKeyName(0, "folder.ico");
            this.imageList2.Images.SetKeyName(1, "DOCL.ICO");
            // 
            // chbShowFreeSpace
            // 
            this.chbShowFreeSpace.AutoSize = true;
            this.chbShowFreeSpace.Location = new System.Drawing.Point(193, 10);
            this.chbShowFreeSpace.Name = "chbShowFreeSpace";
            this.chbShowFreeSpace.Size = new System.Drawing.Size(152, 17);
            this.chbShowFreeSpace.TabIndex = 5;
            this.chbShowFreeSpace.Text = "Show Partition Free Space";
            this.chbShowFreeSpace.UseVisualStyleBackColor = true;
            this.chbShowFreeSpace.CheckedChanged += new System.EventHandler(this.chbShowFreeSpace_CheckedChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 61);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lvCurrentFolder);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.chart1);
            this.splitContainer1.Size = new System.Drawing.Size(786, 507);
            this.splitContainer1.SplitterDistance = 262;
            this.splitContainer1.TabIndex = 6;
            // 
            // cmbViews
            // 
            this.cmbViews.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbViews.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cmbViews.FormattingEnabled = true;
            this.cmbViews.Location = new System.Drawing.Point(12, 33);
            this.cmbViews.Name = "cmbViews";
            this.cmbViews.Size = new System.Drawing.Size(121, 21);
            this.cmbViews.TabIndex = 7;
            this.cmbViews.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // lblPath
            // 
            this.lblPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblPath.Location = new System.Drawing.Point(189, 34);
            this.lblPath.Name = "lblPath";
            this.lblPath.Size = new System.Drawing.Size(561, 20);
            this.lblPath.TabIndex = 9;
            this.lblPath.KeyDown += new System.Windows.Forms.KeyEventHandler(this.lblPath_KeyDown);
            // 
            // btnBack
            // 
            this.btnBack.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBack.Location = new System.Drawing.Point(139, 33);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(22, 22);
            this.btnBack.TabIndex = 12;
            this.btnBack.Text = "\u25C0";
            this.btnBack.UseVisualStyleBackColor = true;
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // btnForward
            // 
            this.btnForward.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnForward.Location = new System.Drawing.Point(163, 33);
            this.btnForward.Name = "btnForward";
            this.btnForward.Size = new System.Drawing.Size(22, 22);
            this.btnForward.TabIndex = 13;
            this.btnForward.Text = "\u25B6";
            this.btnForward.UseVisualStyleBackColor = true;
            this.btnForward.Click += new System.EventHandler(this.btnForward_Click);
            // 
            // chbExplorerMenu
            // 
            this.chbExplorerMenu.AutoSize = true;
            this.chbExplorerMenu.Location = new System.Drawing.Point(360, 10);
            this.chbExplorerMenu.Name = "chbExplorerMenu";
            this.chbExplorerMenu.Size = new System.Drawing.Size(212, 17);
            this.chbExplorerMenu.TabIndex = 14;
            this.chbExplorerMenu.Text = "Add to folder right-click menu";
            this.chbExplorerMenu.UseVisualStyleBackColor = true;
            this.chbExplorerMenu.CheckedChanged += new System.EventHandler(this.chbExplorerMenu_CheckedChanged);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.BackgroundImage = global::PartitionExplorer.Properties.Resources.open;
            this.btnOpenFolder.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenFolder.Location = new System.Drawing.Point(780, 33);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(22, 22);
            this.btnOpenFolder.TabIndex = 10;
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.BackgroundImage = global::PartitionExplorer.Properties.Resources.refresh_over;
            this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefresh.Location = new System.Drawing.Point(754, 33);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(22, 22);
            this.btnRefresh.TabIndex = 8;
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.progressScan,
            this.lnkCancel});
            this.statusStrip1.Location = new System.Drawing.Point(0, 580);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(810, 22);
            this.statusStrip1.TabIndex = 11;
            // 
            // lblStatus
            // 
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(795, 17);
            this.lblStatus.Spring = true;
            this.lblStatus.Text = "Ready";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // progressScan
            // 
            this.progressScan.MarqueeAnimationSpeed = 30;
            this.progressScan.Name = "progressScan";
            this.progressScan.Size = new System.Drawing.Size(100, 16);
            this.progressScan.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressScan.Visible = false;
            // 
            // lnkCancel
            // 
            this.lnkCancel.IsLink = true;
            this.lnkCancel.Name = "lnkCancel";
            this.lnkCancel.Size = new System.Drawing.Size(43, 17);
            this.lnkCancel.Text = "Cancel";
            this.lnkCancel.Visible = false;
            this.lnkCancel.Click += new System.EventHandler(this.lnkCancel_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(810, 602);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.chbExplorerMenu);
            this.Controls.Add(this.btnForward);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.lblPath);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.cmbViews);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.chbShowFreeSpace);
            this.Controls.Add(this.cmbPartition);
            this.Controls.Add(this.label1);
            this.KeyPreview = true;
            this.Name = "Form1";
            this.Text = "Partition Explorer 1.0";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ctxItem.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbPartition;
        private System.Windows.Forms.ListView lvCurrentFolder;
        private System.Windows.Forms.ImageList imageList1;
        private System.Windows.Forms.CheckBox chbShowFreeSpace;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ComboBox cmbViews;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ImageList imageList2;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.TextBox lblPath;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripProgressBar progressScan;
        private System.Windows.Forms.ToolStripStatusLabel lnkCancel;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ContextMenuStrip ctxItem;
        private System.Windows.Forms.ToolStripMenuItem mnuOpen;
        private System.Windows.Forms.ToolStripMenuItem mnuShowInExplorer;
        private System.Windows.Forms.ToolStripMenuItem mnuCopyPath;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem mnuDelete;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem mnuProperties;
        private System.Windows.Forms.Button btnBack;
        private System.Windows.Forms.Button btnForward;
        private System.Windows.Forms.CheckBox chbExplorerMenu;
    }
}

