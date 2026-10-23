using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms.DataVisualization.Charting;
using Microsoft.Win32;

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
        private ScanRequest pendingScan;
        private bool pendingClearCache;
        private int lastProgressTick;
        private Stack<DirectoryInfo> backStack = new Stack<DirectoryInfo>();
        private Stack<DirectoryInfo> forwardStack = new Stack<DirectoryInfo>();
        private bool loadingSettings;

        // Slices smaller than this share of the chart are merged into "Other"
        private const double OtherSliceThreshold = 0.03;
        private const string ExplorerMenuKey = @"Software\Classes\Directory\shell\OpenInPartitionExplorer";
        private const uint SHOP_FILEPATH = 0x2;

        // Shared by the donut and the treemap so a folder has the same colour in both
        private static readonly Color[] Palette = {
            Color.FromArgb(78, 121, 167), Color.FromArgb(242, 142, 43), Color.FromArgb(89, 161, 79),
            Color.FromArgb(225, 87, 89), Color.FromArgb(118, 183, 178), Color.FromArgb(237, 201, 72),
            Color.FromArgb(176, 122, 161), Color.FromArgb(255, 157, 167), Color.FromArgb(156, 117, 95),
            Color.FromArgb(186, 176, 172) };
        private static readonly Color OtherColor = Color.FromArgb(156, 156, 156);
        private static readonly Color FreeColor = Color.FromArgb(222, 222, 222);
        private Dictionary<string, Color> colorMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, string pszObjectName, string pszPropertyPage);

        public Form1(string path)
        {
            InitializeComponent();
            path_ = path;
            if (path_ == "" && Directory.Exists(Properties.Settings.Default.LastFolder))
                path_ = Properties.Settings.Default.LastFolder;
            if (path_ == "") path_ =Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments;

            treemap1.TagEquals = SameTag;
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

                long total = scan.TotalSize;

                foreach (ScanEntry entry in scan.Folders)
                {
                    lvi = new ListViewItem(entry.Info.Name, 0);
                    if (entry.IsLink)
                    {
                        lvi.SubItems.Add("link").Tag = 0L;
                        lvi.SubItems.Add("").Tag = 0L;
                        lvi.SubItems.Add("").Tag = 0L;
                        lvi.ToolTipText = "Junction or symbolic link. Its target is not counted here.";
                    }
                    else
                    {
                        lvi.SubItems.Add(FormatSize(entry.Size) + (entry.Partial ? "*" : "")).Tag = entry.Size;
                        lvi.SubItems.Add(PercentText(entry.Size, total)).Tag = entry.Size;
                        lvi.SubItems.Add(entry.Count.ToString("N0") + (entry.Partial ? "*" : "")).Tag = entry.Count;
                        if (entry.Partial)
                            lvi.ToolTipText = "Some subfolders could not be read, so this size is a minimum.";
                    }
                    lvi.SubItems.Add(entry.Info.LastWriteTime.ToString("g")).Tag = entry.Info.LastWriteTime;
                    lvi.Tag = entry.Info;
                    lvCurrentFolder.Items.Add(lvi);
                }

                foreach (ScanEntry entry in scan.Files)
                {
                    lvi = new ListViewItem(entry.Info.Name, 1);
                    lvi.SubItems.Add(FormatSize(entry.Size)).Tag = entry.Size;
                    lvi.SubItems.Add(PercentText(entry.Size, total)).Tag = entry.Size;
                    lvi.SubItems.Add("").Tag = 0L;
                    lvi.SubItems.Add(entry.Info.LastWriteTime.ToString("g")).Tag = entry.Info.LastWriteTime;
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
            string[] titles = { "Name", "Size", "%", "Items", "Modified" };
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

            List<DataPoint> points = new List<DataPoint>();
            DataPoint dp;
            foreach (ScanEntry entry in scan.Folders)
            {
                if (entry.IsLink)
                    continue;
                points.Add(MakePoint(entry.Info.Name, entry.Size, entry.Info));
            }
            long FileSize = 0;
            foreach (ScanEntry entry in scan.Files)
            {
                FileSize += entry.Size;
            }
            points.Add(MakePoint("Files", FileSize, "Files"));

            if (DI.FullName == DI.Root.FullName && chbShowFreeSpace.Checked)
            {
                DriveInfo drvI = new DriveInfo(DI.FullName.Substring(0, 1));
                points.Add(MakePoint("Free", drvI.AvailableFreeSpace, null));
            }

            // Merge the small slices into one so their labels don't pile up
            double chartTotal = 0;
            foreach (DataPoint p in points)
                chartTotal += p.YValues[0];
            List<DataPoint> small = points.FindAll(p => chartTotal > 0 && p.YValues[0] / chartTotal < OtherSliceThreshold);
            if (small.Count > 1)
            {
                long otherSize = 0;
                List<string> names = new List<string>();
                foreach (DataPoint p in small)
                {
                    otherSize += (long)p.YValues[0];
                    names.Add(p.LabelToolTip);
                    points.Remove(p);
                }
                dp = MakePoint("Other", otherSize, null);
                dp.LabelToolTip = String.Join("\n", names.ToArray());
                dp.ToolTip = dp.LabelToolTip;
                points.Add(dp);
            }

            foreach (DataPoint p in points)
                chart1.Series[0].Points.Add(p);
            chart1.Series[0].Sort(PointSortOrder.Ascending);
            HighlightSelectedSlice();
        }

        private DataPoint MakePoint(string name, long size, object tag)
        {
            DataPoint dp = new DataPoint();
            dp.Label = NameInAppropriateFormat(name) + " " + FormatSize(size);
            dp.LabelToolTip = name;
            dp.SetValueY(size);
            dp.Tag = tag;
            dp.Color = name == "Free" ? FreeColor : ColorFor(tag);
            return dp;
        }

        private static string ColorKey(object tag)
        {
            DirectoryInfo DI = tag as DirectoryInfo;
            return DI != null ? DI.FullName : tag as string;
        }

        private Color ColorFor(object tag)
        {
            Color c;
            string key = ColorKey(tag);
            return key != null && colorMap.TryGetValue(key, out c) ? c : OtherColor;
        }

        // Largest first, so the biggest items get the most distinct colours
        private void AssignColors(FolderScan scan)
        {
            colorMap.Clear();
            List<KeyValuePair<string, long>> keyed = scan.Folders.Where(f => !f.IsLink)
                .Select(f => new KeyValuePair<string, long>(f.Info.FullName, f.Size)).ToList();
            keyed.Add(new KeyValuePair<string, long>("Files", scan.Files.Sum(f => f.Size)));
            int i = 0;
            foreach (KeyValuePair<string, long> k in keyed.OrderByDescending(k => k.Value))
                colorMap[k.Key] = Palette[i++ % Palette.Length];
        }

        private void BindTreemap(FolderScan scan)
        {
            List<TreemapControl.Item> items = new List<TreemapControl.Item>();
            foreach (ScanEntry entry in scan.Folders)
            {
                if (entry.IsLink)
                    continue;
                TreemapControl.Item item = new TreemapControl.Item { Name = entry.Info.Name, Size = entry.Size, Tag = entry.Info, Color = ColorFor(entry.Info) };
                foreach (ScanEntry child in entry.Children)
                    item.Children.Add(new TreemapControl.Item { Name = child.Info != null ? child.Info.Name : "(files)", Size = child.Size, Tag = child.Info });
                items.Add(item);
            }
            long fileSize = scan.Files.Sum(f => f.Size);
            items.Add(new TreemapControl.Item { Name = "Files", Size = fileSize, Tag = "Files", Color = ColorFor("Files") });
            treemap1.SetItems(items);
        }

        private static bool SameTag(object a, object b)
        {
            string ka = ColorKey(a), kb = ColorKey(b);
            return ka != null && String.Equals(ka, kb, StringComparison.OrdinalIgnoreCase);
        }

        private void SetChartMode(int mode)
        {
            splitCharts.Panel1Collapsed = mode == 1;
            splitCharts.Panel2Collapsed = mode == 0;
        }

        private int ChartMode
        {
            get { return rbDonut.Checked ? 0 : rbTreemap.Checked ? 1 : 2; }
            set
            {
                RadioButton rb = value == 0 ? rbDonut : value == 1 ? rbTreemap : rbBoth;
                rb.Checked = true;
                SetChartMode(value);
            }
        }

        // Pulls out the slice for the selected row (its folder, or "Files" for a file)
        // and outlines the matching treemap box
        private void HighlightSelectedSlice()
        {
            object selected = null;
            if (lvCurrentFolder.SelectedItems.Count > 0 && lvCurrentFolder.SelectedItems[0].Text != "..")
            {
                object tag = lvCurrentFolder.SelectedItems[0].Tag;
                selected = tag is FileInfo ? "Files" : tag;
            }
            foreach (DataPoint p in chart1.Series[0].Points)
            {
                bool match = selected != null && p.Tag != null &&
                    (p.Tag is string ? p.Tag.Equals(selected)
                        : selected is DirectoryInfo && ((DirectoryInfo)p.Tag).FullName == ((DirectoryInfo)selected).FullName);
                p["Exploded"] = match ? "true" : "false";
            }
            treemap1.SelectedTag = selected;
        }

        private static string PercentText(long size, long total)
        {
            if (total <= 0)
                return "";
            return (size * 100.0 / total).ToString("0.0") + " %";
        }

        private string NameInAppropriateFormat(string p)
        {
            if (p.Length > 13)
            {
                p = p.Substring(0, 10) + "...";
            }
            return p;
        }

        internal static string FormatSize(long size)
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
                    result.Count += sub.Count + 1;
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
                    result.FileSize += f.Length;
                    result.Count++;
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
                    entry.Count = size.Count;
                    entry.Partial = size.Partial;
                    scan.Partial |= size.Partial;
                    AddChildren(entry, d, size);
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

        // Runs on the scan worker after DirectorySize, so every subfolder size is already cached
        private void AddChildren(ScanEntry entry, DirectoryInfo DI, FolderSize size)
        {
            try
            {
                foreach (DirectoryInfo d in DI.EnumerateDirectories())
                {
                    FolderSize sub;
                    if (!IsLink(d) && sizeDictionary.TryGetValue(d.FullName, out sub))
                        entry.Children.Add(new ScanEntry { Info = d, Size = sub.Size });
                }
            }
            catch (Exception ex)
            {
                if (!IsAccessError(ex)) throw;
            }
            if (size.FileSize > 0)
                entry.Children.Add(new ScanEntry { Size = size.FileSize });
        }

        private void StartScan(DirectoryInfo DI, NavKind nav, bool clearCache)
        {
            ScanRequest request = new ScanRequest();
            request.Folder = DI;
            request.Nav = nav;
            if (scanWorker.IsBusy)
            {
                // Restart with the new folder once the running scan has stopped
                pendingScan = request;
                pendingClearCache |= clearCache;
                scanWorker.CancelAsync();
                return;
            }
            if (clearCache)
                sizeDictionary.Clear();
            lblStatus.Text = "Scanning " + DI.FullName;
            progressScan.Visible = true;
            lnkCancel.Visible = true;
            scanWorker.RunWorkerAsync(request);
        }

        private void NavigateTo(DirectoryInfo DI)
        {
            StartScan(DI, NavKind.Normal, false);
        }

        private void GoBack()
        {
            if (backStack.Count > 0)
                StartScan(backStack.Peek(), NavKind.Back, false);
        }

        private void GoForward()
        {
            if (forwardStack.Count > 0)
                StartScan(forwardStack.Peek(), NavKind.Forward, false);
        }

        private void GoUp()
        {
            if (currentScan != null && currentScan.Folder.Parent != null)
                NavigateTo(currentScan.Folder.Parent);
        }

        // Called once a scan has succeeded, before it becomes the current folder
        private void UpdateHistory(FolderScan scan)
        {
            if (currentScan == null || scan.Request.Nav == NavKind.Refresh)
                return;
            if (scan.Request.Nav == NavKind.Back)
            {
                backStack.Pop();
                forwardStack.Push(currentScan.Folder);
            }
            else if (scan.Request.Nav == NavKind.Forward)
            {
                forwardStack.Pop();
                backStack.Push(currentScan.Folder);
            }
            else if (!String.Equals(currentScan.Folder.FullName, scan.Folder.FullName, StringComparison.OrdinalIgnoreCase))
            {
                backStack.Push(currentScan.Folder);
                forwardStack.Clear();
            }
        }

        private void UpdateNavButtons()
        {
            btnBack.Enabled = backStack.Count > 0;
            btnForward.Enabled = forwardStack.Count > 0;
        }

        private FileSystemInfo SelectedInfo()
        {
            if (lvCurrentFolder.SelectedItems.Count == 0 || lvCurrentFolder.SelectedItems[0].Text == "..")
                return null;
            return lvCurrentFolder.SelectedItems[0].Tag as FileSystemInfo;
        }

        // Folders open in the app, files open with their default program
        private void OpenItem(ListViewItem item)
        {
            DirectoryInfo DI = item.Tag as DirectoryInfo;
            if (DI != null)
            {
                NavigateTo(DI);
                return;
            }
            FileInfo FI = item.Tag as FileInfo;
            if (FI == null)
                return;
            try
            {
                // .NET 8 no longer goes through the shell by default, which file associations need
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(FI.FullName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // Drops cached sizes for a deleted item, everything under it, and its parents
        private void InvalidateCache(string path)
        {
            string prefix = path.TrimEnd('\\') + "\\";
            List<string> stale = sizeDictionary.Keys.Where(k => k == path || k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (string key in stale)
                sizeDictionary.Remove(key);
            for (DirectoryInfo p = Directory.GetParent(path); p != null; p = p.Parent)
                sizeDictionary.Remove(p.FullName);
        }

        private static bool ExplorerMenuInstalled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(ExplorerMenuKey))
            {
                return key != null;
            }
        }

        // Per-user registration, so it needs no admin rights and follows the exe's real location
        private static void SetExplorerMenu(bool install)
        {
            if (!install)
            {
                Registry.CurrentUser.DeleteSubKeyTree(ExplorerMenuKey, false);
                return;
            }
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(ExplorerMenuKey))
            {
                key.SetValue("", "Open in PartitionExplorer");
                key.SetValue("Icon", Application.ExecutablePath);
                using (RegistryKey command = key.CreateSubKey("command"))
                {
                    command.SetValue("", "\"" + Application.ExecutablePath + "\" \"%1\"");
                }
            }
        }

        private void LoadSettings()
        {
            Properties.Settings settings = Properties.Settings.Default;
            loadingSettings = true;
            try
            {
                Rectangle bounds = settings.WindowBounds;
                if (bounds.Width > 0 && bounds.Height > 0 && Screen.AllScreens.Any(sc => sc.WorkingArea.IntersectsWith(bounds)))
                {
                    StartPosition = FormStartPosition.Manual;
                    Bounds = bounds;
                }
                if (settings.WindowMaximized)
                    WindowState = FormWindowState.Maximized;
                if (settings.SplitterDistance > 0 && settings.SplitterDistance < splitContainer1.Width - splitContainer1.Panel2MinSize)
                    splitContainer1.SplitterDistance = settings.SplitterDistance;
                if (settings.ViewIndex >= 0 && settings.ViewIndex < cmbViews.Items.Count)
                    cmbViews.SelectedIndex = settings.ViewIndex;
                chbShowFreeSpace.Checked = settings.ShowFreeSpace;
                ChartMode = settings.ChartMode;
                if (settings.ChartSplitterDistance > 0 && settings.ChartSplitterDistance < splitCharts.Width - splitCharts.Panel2MinSize)
                    splitCharts.SplitterDistance = settings.ChartSplitterDistance;
                chbExplorerMenu.Checked = ExplorerMenuInstalled();
            }
            finally
            {
                loadingSettings = false;
            }
        }

        private void SaveSettings()
        {
            Properties.Settings settings = Properties.Settings.Default;
            settings.WindowMaximized = WindowState == FormWindowState.Maximized;
            settings.WindowBounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            settings.SplitterDistance = splitContainer1.SplitterDistance;
            settings.ViewIndex = cmbViews.SelectedIndex;
            settings.ShowFreeSpace = chbShowFreeSpace.Checked;
            settings.ChartMode = ChartMode;
            if (!splitCharts.Panel1Collapsed && !splitCharts.Panel2Collapsed)
                settings.ChartSplitterDistance = splitCharts.SplitterDistance;
            if (currentScan != null)
                settings.LastFolder = currentScan.Folder.FullName;
            settings.Save();
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

            LoadSettings();
            UpdateNavButtons();
            NavigateTo(new DirectoryInfo(path_));
        }

        private void cmbPartition_SelectedIndexChanged(object sender, EventArgs e)
        {
            NavigateTo(((DriveInfo)cmbPartition.SelectedItem).RootDirectory);
        }

        private void chbShowFreeSpace_CheckedChanged(object sender, EventArgs e)
        {
            if (currentScan != null)
                BindChart(currentScan);
        }

        private void chbExplorerMenu_CheckedChanged(object sender, EventArgs e)
        {
            if (loadingSettings)
                return;
            try
            {
                SetExplorerMenu(chbExplorerMenu.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                loadingSettings = true;
                chbExplorerMenu.Checked = ExplorerMenuInstalled();
                loadingSettings = false;
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvCurrentFolder.View = ((System.Collections.Generic.KeyValuePair<string, System.Windows.Forms.View>)(cmbViews.SelectedValue)).Value;
        }

        private void lvCurrentFolder_DoubleClick(object sender, EventArgs e)
        {
            if (lvCurrentFolder.SelectedItems.Count > 0)
                OpenItem(lvCurrentFolder.SelectedItems[0]);
        }

        private void lvCurrentFolder_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                GoUp();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && lvCurrentFolder.SelectedItems.Count > 0)
            {
                OpenItem(lvCurrentFolder.SelectedItems[0]);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Delete && SelectedInfo() != null)
            {
                mnuDelete_Click(sender, EventArgs.Empty);
                e.Handled = true;
            }
        }

        private void lvCurrentFolder_SelectedIndexChanged(object sender, EventArgs e)
        {
            HighlightSelectedSlice();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Alt && e.KeyCode == Keys.Left) || e.KeyCode == Keys.BrowserBack)
            {
                GoBack();
                e.Handled = true;
            }
            else if ((e.Alt && e.KeyCode == Keys.Right) || e.KeyCode == Keys.BrowserForward)
            {
                GoForward();
                e.Handled = true;
            }
            else if (e.Alt && e.KeyCode == Keys.Up)
            {
                GoUp();
                e.Handled = true;
            }
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            GoBack();
        }

        private void btnForward_Click(object sender, EventArgs e)
        {
            GoForward();
        }

        private void lblPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string path = Environment.ExpandEnvironmentVariables(lblPath.Text.Trim().Trim('"'));
                if (Directory.Exists(path))
                    NavigateTo(new DirectoryInfo(path));
                else
                    lblStatus.Text = "Folder not found: " + path;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                if (currentScan != null)
                    lblPath.Text = currentScan.Folder.FullName;
            }
        }

        private void chart1_MouseClick(object sender, MouseEventArgs e)
        {
            DirectoryInfo DI = FolderAt(e.Location);
            if (DI != null)
                NavigateTo(DI);
        }

        private void treemap1_ItemClicked(object sender, object tag)
        {
            DirectoryInfo DI = tag as DirectoryInfo;
            if (DI != null)
                NavigateTo(DI);
        }

        private void rbChartMode_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
                SetChartMode(ChartMode);
        }

        private void chart1_MouseMove(object sender, MouseEventArgs e)
        {
            chart1.Cursor = FolderAt(e.Location) != null ? Cursors.Hand : Cursors.Default;
        }

        private DirectoryInfo FolderAt(Point location)
        {
            HitTestResult hit = chart1.HitTest(location.X, location.Y);
            if (hit.ChartElementType != ChartElementType.DataPoint && hit.ChartElementType != ChartElementType.DataPointLabel)
                return null;
            if (hit.PointIndex < 0 || hit.PointIndex >= chart1.Series[0].Points.Count)
                return null;
            return chart1.Series[0].Points[hit.PointIndex].Tag as DirectoryInfo;
        }

        private void ctxItem_Opening(object sender, CancelEventArgs e)
        {
            FileSystemInfo info = SelectedInfo();
            if (info == null)
            {
                e.Cancel = true;
                return;
            }
            // Deleting mid-scan would race the scan worker on the size cache
            mnuDelete.Enabled = !scanWorker.IsBusy;
        }

        private void mnuOpen_Click(object sender, EventArgs e)
        {
            if (lvCurrentFolder.SelectedItems.Count > 0)
                OpenItem(lvCurrentFolder.SelectedItems[0]);
        }

        private void mnuShowInExplorer_Click(object sender, EventArgs e)
        {
            FileSystemInfo info = SelectedInfo();
            if (info != null)
                System.Diagnostics.Process.Start("explorer.exe", "/select,\"" + info.FullName + "\"");
        }

        private void mnuCopyPath_Click(object sender, EventArgs e)
        {
            FileSystemInfo info = SelectedInfo();
            if (info != null)
                Clipboard.SetText(info.FullName);
        }

        private void mnuDelete_Click(object sender, EventArgs e)
        {
            FileSystemInfo info = SelectedInfo();
            if (info == null || scanWorker.IsBusy)
                return;
            try
            {
                // Windows shows its own confirmation and progress dialogs
                if (info is DirectoryInfo)
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(info.FullName,
                        Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                        Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
                else
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(info.FullName,
                        Microsoft.VisualBasic.FileIO.UIOption.AllDialogs,
                        Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                        Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            info.Refresh();
            if (info.Exists)
                return;
            InvalidateCache(info.FullName);
            StartScan(currentScan.Folder, NavKind.Refresh, false);
        }

        private void mnuProperties_Click(object sender, EventArgs e)
        {
            FileSystemInfo info = SelectedInfo();
            if (info != null)
                SHObjectProperties(Handle, SHOP_FILEPATH, info.FullName, null);
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
                // Numbers and dates are most useful largest/newest first; Name starts A-Z
                lvSorter.Order = e.Column == 0 ? SortOrder.Ascending : SortOrder.Descending;
            }
            UpdateSortIndicators();
            if (lvCurrentFolder.ListViewItemSorter == null)
                lvCurrentFolder.ListViewItemSorter = lvSorter;
            else
                lvCurrentFolder.Sort();
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            DirectoryInfo DI = pendingScan != null ? pendingScan.Folder
                : lvCurrentFolder.Tag as DirectoryInfo ?? new DirectoryInfo(path_);
            StartScan(DI, NavKind.Refresh, true);
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
            try
            {
                SaveSettings();
            }
            catch (System.Configuration.ConfigurationException)
            {
                // A broken user.config should not stop the app from closing
            }
        }

        private void scanWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                ScanRequest request = (ScanRequest)e.Argument;
                FolderScan scan = ScanFolder(request.Folder, (BackgroundWorker)sender);
                scan.Request = request;
                e.Result = scan;
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
                ScanRequest next = pendingScan;
                bool clear = pendingClearCache;
                pendingScan = null;
                pendingClearCache = false;
                StartScan(next.Folder, next.Nav, clear);
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
                FolderScan scan = (FolderScan)e.Result;
                UpdateHistory(scan);
                currentScan = scan;
                UpdateNavButtons();
                AssignColors(currentScan);
                BindListView(currentScan);
                BindChart(currentScan);
                BindTreemap(currentScan);
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

        // Sorts by Name (column 0) or by the raw value behind any other column.
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

                int result = 0;
                if (SortColumn > 0)
                {
                    IComparable keyA = KeyOf(a), keyB = KeyOf(b);
                    if (keyA != null && keyB != null)
                        result = keyA.CompareTo(keyB);
                }
                if (result == 0)
                    result = String.Compare(a.Text, b.Text, StringComparison.CurrentCultureIgnoreCase);
                return Order == SortOrder.Descending ? -result : result;
            }

            private static int Rank(ListViewItem item)
            {
                if (item.Text == "..")
                    return 0;
                return item.Tag is FileInfo ? 2 : 1;
            }

            // Each sortable column keeps its raw value (bytes, item count, date) in the sub-item's Tag
            private IComparable KeyOf(ListViewItem item)
            {
                if (item.SubItems.Count > SortColumn)
                    return item.SubItems[SortColumn].Tag as IComparable;
                return null;
            }
        }

        private class FolderSize
        {
            public long Size;
            public long FileSize;
            public long Count;
            public bool Partial;
        }

        private class ScanEntry
        {
            public FileSystemInfo Info;
            public long Size;
            public long Count;
            public bool Partial;
            public bool IsLink;
            // One level of subfolders (plus a null-Info entry for the folder's own files), for the treemap
            public List<ScanEntry> Children = new List<ScanEntry>();
        }

        private class FolderScan
        {
            public DirectoryInfo Folder;
            public ScanRequest Request;
            public List<ScanEntry> Folders = new List<ScanEntry>();
            public List<ScanEntry> Files = new List<ScanEntry>();
            public bool Partial;

            public long TotalSize
            {
                get { return Folders.Where(f => !f.IsLink).Sum(f => f.Size) + Files.Sum(f => f.Size); }
            }
        }

        private enum NavKind { Normal, Back, Forward, Refresh }

        private class ScanRequest
        {
            public DirectoryInfo Folder;
            public NavKind Nav;
        }
    }


}
