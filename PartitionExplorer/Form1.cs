using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Windows.Forms.DataVisualization.Charting;

namespace PartitionExplorer
{
    public partial class Form1 : Form
    {
        private string path_;
        // Only touched by the scan worker while it runs, and by the UI thread otherwise
        Dictionary<String, FolderSize> sizeDictionary = new Dictionary<string, FolderSize>();
        private ListViewItemComparer lvSorter = new ListViewItemComparer();
        private BackgroundWorker scanWorker = new BackgroundWorker();
        private FolderScan currentScan;
        private DirectoryInfo pendingScan;
        private bool pendingClearCache;
        private int lastProgressTick;

        public Form1(string path)
        {
            InitializeComponent();
            path_ = path;
            if (path_ == "") path_ =Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments;

            scanWorker.WorkerReportsProgress = true;
            scanWorker.WorkerSupportsCancellation = true;
            scanWorker.DoWork += scanWorker_DoWork;
            scanWorker.ProgressChanged += scanWorker_ProgressChanged;
            scanWorker.RunWorkerCompleted += scanWorker_RunWorkerCompleted;
        }

        #region Functions
        private void BindListView(FolderScan scan)
        {
            DirectoryInfo DI = scan.Folder;
            lvCurrentFolder.Tag = DI;
            lblPath.Text = DI.FullName;
            lvCurrentFolder.BeginUpdate();
            try
            {
                lvCurrentFolder.ListViewItemSorter = null;
                lvCurrentFolder.Items.Clear();
                ListViewItem lvi;

                if (DI.Parent != null)
                {
                    lvi = new ListViewItem("..", 0);
                    lvi.Tag = DI.Parent;
                    lvi.ToolTipText = DI.Name;
                    lvCurrentFolder.Items.Add(lvi);
                }

                foreach (ScanEntry entry in scan.Folders)
                {
                    lvi = new ListViewItem(entry.Info.Name, 0);
                    if (entry.IsLink)
                    {
                        lvi.SubItems.Add("link").Tag = 0L;
                        lvi.ToolTipText = "Junction or symbolic link. Its target is not counted here.";
                    }
                    else
                    {
                        lvi.SubItems.Add(SizeInAppropriateFormat(entry.Size) + (entry.Partial ? "*" : "")).Tag = entry.Size;
                        if (entry.Partial)
                            lvi.ToolTipText = "Some subfolders could not be read, so this size is a minimum.";
                    }
                    lvi.Tag = entry.Info;
                    lvCurrentFolder.Items.Add(lvi);
                }

                foreach (ScanEntry entry in scan.Files)
                {
                    lvi = new ListViewItem(entry.Info.Name, 1);
                    lvi.SubItems.Add(SizeInAppropriateFormat(entry.Size)).Tag = entry.Size;
                    lvi.Tag = entry.Info;
                    lvCurrentFolder.Items.Add(lvi);
                }

                if (lvSorter.SortColumn >= 0)
                    lvCurrentFolder.ListViewItemSorter = lvSorter;
            }
            finally
            {
                lvCurrentFolder.EndUpdate();
            }
        }

        private void UpdateSortIndicators()
        {
            string[] titles = { "Name", "Size" };
            for (int i = 0; i < lvCurrentFolder.Columns.Count && i < titles.Length; i++)
            {
                string title = titles[i];
                if (i == lvSorter.SortColumn)
                    title += lvSorter.Order == SortOrder.Ascending ? " \u25B2" : " \u25BC";
                lvCurrentFolder.Columns[i].Text = title;
            }
        }

        private void BindChart(FolderScan scan)
        {
            DirectoryInfo DI = scan.Folder;
            chart1.Series[0].Points.Clear();
            DataPoint dp;
            foreach (ScanEntry entry in scan.Folders)
            {
                if (entry.IsLink)
                    continue;
                dp = new DataPoint();
                dp.Label = NameInAppropriateFormat(entry.Info.Name) + " " + SizeInAppropriateFormat(entry.Size);
                dp.LabelToolTip = entry.Info.Name;
                dp.SetValueY(entry.Size);
                chart1.Series[0].Points.Add(dp);
            }
            long FileSize = 0;
            foreach (ScanEntry entry in scan.Files)
            {
                FileSize += entry.Size;
            }
            dp = new DataPoint();
            dp.Label = NameInAppropriateFormat("Files") + " " + SizeInAppropriateFormat(FileSize);
            dp.LabelToolTip = "Files";
            dp.SetValueY(FileSize);
            chart1.Series[0].Points.Add(dp);

            if (DI.FullName == DI.Root.FullName && chbShowFreeSpace.Checked)
            {
                DriveInfo drvI = new DriveInfo(DI.FullName.Substring(0, 1));
                dp = new DataPoint();
                dp.Label = NameInAppropriateFormat("Free ") + SizeInAppropriateFormat(drvI.AvailableFreeSpace);
                dp.SetValueY(drvI.AvailableFreeSpace);
                chart1.Series[0].Points.Add(dp);
            }
            chart1.Series[0].Sort(PointSortOrder.Ascending);
        }

        private string NameInAppropriateFormat(string p)
        {
            if (p.Length > 13)
            {
                p = p.Substring(0, 10) + "...";
            }
            return p;
        }

        private string SizeInAppropriateFormat(long size)
        {
            string[] units = { "KB", "MB", "GB", "TB", "PB" };
            if (size < 1024)
                return size.ToString() + " B";
            double value = size;
            int unit = -1;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value.ToString("0.00") + " " + units[unit];
        }

        private static bool IsLink(DirectoryInfo DI)
        {
            return (DI.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;
        }

        // Runs on the scan worker. Junctions and symbolic links are skipped so their
        // targets are not counted twice and cycles cannot happen.
        private FolderSize DirectorySize(DirectoryInfo DI, BackgroundWorker worker)
        {
            FolderSize cached;
            if (sizeDictionary.TryGetValue(DI.FullName, out cached))
                return cached;

            if (worker.CancellationPending)
                throw new OperationCanceledException();

            int now = Environment.TickCount;
            if (now - lastProgressTick > 100)
            {
                lastProgressTick = now;
                worker.ReportProgress(0, DI.FullName);
            }

            FolderSize result = new FolderSize();
            try
            {
                foreach (DirectoryInfo d in DI.EnumerateDirectories())
                {
                    if (IsLink(d))
                        continue;
                    FolderSize sub = DirectorySize(d, worker);
                    result.Size += sub.Size;
                    result.Partial |= sub.Partial;
                }
            }
            catch (Exception ex)
            {
                if (!IsAccessError(ex)) throw;
                result.Partial = true;
            }
            try
            {
                foreach (FileInfo f in DI.EnumerateFiles())
                {
                    result.Size += f.Length;
                }
            }
            catch (Exception ex)
            {
                if (!IsAccessError(ex)) throw;
                result.Partial = true;
            }

            sizeDictionary[DI.FullName] = result;
            return result;
        }

        private static bool IsAccessError(Exception ex)
        {
            return ex is UnauthorizedAccessException || ex is IOException || ex is System.Security.SecurityException;
        }

        // Runs on the scan worker. Errors reading the folder itself propagate, so the
        // previous view stays on screen and the user sees why.
        private FolderScan ScanFolder(DirectoryInfo DI, BackgroundWorker worker)
        {
            FolderScan scan = new FolderScan();
            scan.Folder = DI;
            foreach (DirectoryInfo d in DI.GetDirectories())
            {
                ScanEntry entry = new ScanEntry();
                entry.Info = d;
                entry.IsLink = IsLink(d);
                if (!entry.IsLink)
                {
                    FolderSize size = DirectorySize(d, worker);
                    entry.Size = size.Size;
                    entry.Partial = size.Partial;
                    scan.Partial |= size.Partial;
                }
                scan.Folders.Add(entry);
            }
            foreach (FileInfo f in DI.GetFiles())
            {
                ScanEntry entry = new ScanEntry();
                entry.Info = f;
                try
                {
                    entry.Size = f.Length;
                }
                catch (Exception ex)
                {
                    if (!IsAccessError(ex)) throw;
                    entry.Partial = true;
                }
                scan.Files.Add(entry);
            }
            return scan;
        }

        private void StartScan(DirectoryInfo DI, bool clearCache)
        {
            if (scanWorker.IsBusy)
            {
                // Restart with the new folder once the running scan has stopped
                pendingScan = DI;
                pendingClearCache |= clearCache;
                scanWorker.CancelAsync();
                return;
            }
            if (clearCache)
                sizeDictionary.Clear();
            lblStatus.Text = "Scanning " + DI.FullName;
            progressScan.Visible = true;
            lnkCancel.Visible = true;
            scanWorker.RunWorkerAsync(DI);
        }

        #endregion

        #region Control Events
        private void Form1_Load(object sender, EventArgs e)
        {
            Dictionary<String, View> views = new Dictionary<string, View>();
            views.Add(View.Details.ToString(), View.Details);
            views.Add(View.LargeIcon.ToString(), View.LargeIcon);
            views.Add(View.List.ToString(), View.List);
            views.Add(View.SmallIcon.ToString(), View.SmallIcon);
            views.Add(View.Tile.ToString(), View.Tile);

            cmbViews.DataSource = views.ToList();
            cmbViews.DisplayMember = "Key";
            cmbViews.SelectedIndex = 4;


            DriveInfo DrvInfo = new DriveInfo("D");
            string[] Ds = System.Environment.GetLogicalDrives();
            foreach (string Drive in Ds)
            {
                DriveInfo dvI = new DriveInfo(Drive);
                if (dvI.DriveType == DriveType.Fixed)
                {
                    cmbPartition.DisplayMember = "Name";
                    cmbPartition.Items.Add(new DriveInfo(Drive));
                }
            }

            StartScan(new DirectoryInfo(path_), false);
        }

        private void cmbPartition_SelectedIndexChanged(object sender, EventArgs e)
        {
            StartScan(((DriveInfo)cmbPartition.SelectedItem).RootDirectory, false);
        }

        private void chbShowFreeSpace_CheckedChanged(object sender, EventArgs e)
        {
            if (currentScan != null)
                BindChart(currentScan);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvCurrentFolder.View = ((System.Collections.Generic.KeyValuePair<string, System.Windows.Forms.View>)(cmbViews.SelectedValue)).Value;
        }

        private void lvCurrentFolder_DoubleClick(object sender, EventArgs e)
        {
            if (lvCurrentFolder.SelectedItems.Count == 0)
                return;
            DirectoryInfo DI = lvCurrentFolder.SelectedItems[0].Tag as DirectoryInfo;
            if (DI != null)
                StartScan(DI, false);
        }

        private void lvCurrentFolder_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == lvSorter.SortColumn)
            {
                lvSorter.Order = lvSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            }
            else
            {
                lvSorter.SortColumn = e.Column;
                // Size is most useful largest-first; Name starts A-Z
                lvSorter.Order = e.Column == 1 ? SortOrder.Descending : SortOrder.Ascending;
            }
            UpdateSortIndicators();
            if (lvCurrentFolder.ListViewItemSorter == null)
                lvCurrentFolder.ListViewItemSorter = lvSorter;
            else
                lvCurrentFolder.Sort();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            DirectoryInfo DI = pendingScan ?? lvCurrentFolder.Tag as DirectoryInfo ?? new DirectoryInfo(path_);
            StartScan(DI, true);
        }

        private void lnkCancel_Click(object sender, EventArgs e)
        {
            pendingScan = null;
            scanWorker.CancelAsync();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            pendingScan = null;
            scanWorker.CancelAsync();
        }

        private void scanWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                e.Result = ScanFolder((DirectoryInfo)e.Argument, (BackgroundWorker)sender);
            }
            catch (OperationCanceledException)
            {
                e.Cancel = true;
            }
        }

        private void scanWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            lblStatus.Text = "Scanning " + (string)e.UserState;
        }

        private void scanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (IsDisposed)
                return;

            if (pendingScan != null)
            {
                DirectoryInfo next = pendingScan;
                bool clear = pendingClearCache;
                pendingScan = null;
                pendingClearCache = false;
                StartScan(next, clear);
                return;
            }

            progressScan.Visible = false;
            lnkCancel.Visible = false;

            if (e.Error != null)
            {
                lblStatus.Text = "Could not open folder";
                MessageBox.Show(e.Error.Message);
            }
            else if (e.Cancelled)
            {
                lblStatus.Text = "Scan cancelled";
            }
            else
            {
                currentScan = (FolderScan)e.Result;
                BindListView(currentScan);
                BindChart(currentScan);
                lblStatus.Text = currentScan.Partial
                    ? "Some folders could not be read. Sizes marked * are minimums."
                    : "Ready";
            }
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("Explorer.exe", ((DirectoryInfo)lvCurrentFolder.Tag).FullName);           
        }
        #endregion

        // Sorts by Name (column 0) or Size in bytes (column 1).
        // ".." always stays on top and folders always come before files.
        private class ListViewItemComparer : System.Collections.IComparer
        {
            public int SortColumn = -1;
            public SortOrder Order = SortOrder.Ascending;

            public int Compare(object x, object y)
            {
                ListViewItem a = (ListViewItem)x;
                ListViewItem b = (ListViewItem)y;

                int rankA = Rank(a), rankB = Rank(b);
                if (rankA != rankB)
                    return rankA.CompareTo(rankB);

                int result;
                if (SortColumn == 1)
                {
                    result = SizeOf(a).CompareTo(SizeOf(b));
                    if (result == 0)
                        result = String.Compare(a.Text, b.Text, StringComparison.CurrentCultureIgnoreCase);
                }
                else
                {
                    result = String.Compare(a.Text, b.Text, StringComparison.CurrentCultureIgnoreCase);
                }
                return Order == SortOrder.Descending ? -result : result;
            }

            private static int Rank(ListViewItem item)
            {
                if (item.Text == "..")
                    return 0;
                return item.Tag is FileInfo ? 2 : 1;
            }

            private static long SizeOf(ListViewItem item)
            {
                if (item.SubItems.Count > 1 && item.SubItems[1].Tag is long)
                    return (long)item.SubItems[1].Tag;
                return 0;
            }
        }

        private class FolderSize
        {
            public long Size;
            public bool Partial;
        }

        private class ScanEntry
        {
            public FileSystemInfo Info;
            public long Size;
            public bool Partial;
            public bool IsLink;
        }

        private class FolderScan
        {
            public DirectoryInfo Folder;
            public List<ScanEntry> Folders = new List<ScanEntry>();
            public List<ScanEntry> Files = new List<ScanEntry>();
            public bool Partial;
        }
    }


}
