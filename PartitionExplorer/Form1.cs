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
                lvi.SubItems.Add(SizeInAppropriateFormat(DirectorySize(item)));
                lvi.Tag = item;
                lvCurrentFolder.Items.Add(lvi);
            }

            foreach (FileInfo item in DI.GetFiles())
            {
                lvi = new ListViewItem(item.Name, 1);
                lvi.SubItems.Add(SizeInAppropriateFormat(item.Length));
                lvi.Tag = item;
                lvCurrentFolder.Items.Add(lvi);
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

    }


}
