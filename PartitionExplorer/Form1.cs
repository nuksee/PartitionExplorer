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
        Dictionary<String, float> sizeDictionary = new Dictionary<string, float>();
        private ListViewItemComparer lvSorter = new ListViewItemComparer();

        public Form1(string path)
        {
            InitializeComponent();
            path_ = path;
            if (path_ == "") path_ =Microsoft.VisualBasic.FileIO.SpecialDirectories.MyDocuments;

        }

        #region Functions
        private void BindListView(DirectoryInfo DI)
        {
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

                foreach (DirectoryInfo item in DI.EnumerateDirectories())
                {
                    lvi = new ListViewItem(item.Name, 0);
                    float dirSize = DirectorySize(item);
                    lvi.SubItems.Add(SizeInAppropriateFormat(dirSize)).Tag = dirSize;
                    lvi.Tag = item;
                    lvCurrentFolder.Items.Add(lvi);
                }

                foreach (FileInfo item in DI.GetFiles())
                {
                    lvi = new ListViewItem(item.Name, 1);
                    lvi.SubItems.Add(SizeInAppropriateFormat(item.Length)).Tag = (float)item.Length;
                    lvi.Tag = item;
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

        private void BindChart(DirectoryInfo DI)
        {
            chart1.Series[0].Points.Clear();
            DataPoint dp;
            foreach (DirectoryInfo d in DI.GetDirectories())
            {
                dp = new DataPoint();
                float size = DirectorySize(d);
                dp.Label = NameInAppropriateFormat(d.Name) + " " + SizeInAppropriateFormat(size);
                dp.LabelToolTip = d.Name;
                dp.SetValueY(size);
                chart1.Series[0].Points.Add(dp);
            }
            float FileSize = 0;
            foreach (FileInfo f in DI.GetFiles())
            {
                FileSize += f.Length;
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

        private string SizeInAppropriateFormat(float size)
        {
            string result = "";
            float K = 1024;
            if (size < 1024)
                result = size.ToString() + " B";
            else if ((size / K) < 1024)
                result = (size / K).ToString("0.00") + " KB";
            else if ((size / (K * K)) < 1024)
                result = (size / (K * K)).ToString("0.00") + " MB";
            else if ((size / (K * K * K)) < 1024)
                result = (size / (K * K * K)).ToString("0.00") + " GB";
            return result;
        }

        private float DirectorySize(DirectoryInfo DI)
        {
            if (sizeDictionary.ContainsKey(DI.FullName))
            {
                return sizeDictionary[DI.FullName];
            }
            else
            {
                float size = 0;
                try
                {

                    if (DI.GetDirectories().Count<DirectoryInfo>() > 0)
                        foreach (DirectoryInfo d in DI.GetDirectories())
                        {
                            size += DirectorySize(d);
                        }
                }
                catch (Exception)
                {

                }
                try
                {
                    foreach (FileInfo f in DI.GetFiles())
                    {
                        size += f.Length;
                    }
                }
                catch (Exception)
                {

                }

                sizeDictionary.Add(DI.FullName, size);
                return size;
            }
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

            try
            {
                BindListView(new DirectoryInfo(path_));
                BindChart(new DirectoryInfo(path_));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void cmbPartition_SelectedIndexChanged(object sender, EventArgs e)
        {
            DirectoryInfo DI = ((DriveInfo)cmbPartition.SelectedItem).RootDirectory;
            BindListView(DI);
            BindChart(DI);
        }

        private void chbShowFreeSpace_CheckedChanged(object sender, EventArgs e)
        {
            if (lvCurrentFolder.Tag != null)
            {
                DirectoryInfo DI = (DirectoryInfo)lvCurrentFolder.Tag;
                BindListView(DI);
                BindChart(DI);
            }

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            lvCurrentFolder.View = ((System.Collections.Generic.KeyValuePair<string, System.Windows.Forms.View>)(cmbViews.SelectedValue)).Value;
        }

        private void lvCurrentFolder_DoubleClick(object sender, EventArgs e)
        {
            try
            {
                DirectoryInfo DI = (DirectoryInfo)lvCurrentFolder.SelectedItems[0].Tag;
                BindListView(DI);
                BindChart(DI);
            }
            catch (Exception)
            {

            }
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
            sizeDictionary.Clear();            
            try
            {
                DirectoryInfo DI = (DirectoryInfo)lvCurrentFolder.Tag;
                BindListView(DI);
                BindChart(DI);
            }
            catch (Exception)
            {

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

            private static float SizeOf(ListViewItem item)
            {
                if (item.SubItems.Count > 1 && item.SubItems[1].Tag is float)
                    return (float)item.SubItems[1].Tag;
                return 0;
            }
        }
    }


}
