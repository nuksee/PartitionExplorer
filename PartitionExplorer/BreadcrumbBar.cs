using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace PartitionExplorer
{
    // The path as clickable folder segments, like File Explorer's address bar. Clicking empty
    // space (or BeginEdit, for Ctrl+L and Alt+D) swaps in a text box for typing a path; Enter
    // asks to go there, and Esc or clicking away swaps the segments back. When the path doesn't
    // fit, the leftmost segments fold into a « button that lists them.
    public class BreadcrumbBar : Control
    {
        private class Crumb
        {
            public string Text;
            public string Path;
            public Rectangle Bounds;
            public bool Visible;
        }

        private static readonly Color LightBorder = Color.FromArgb(204, 204, 204);
        private static readonly Color LightHover = Color.FromArgb(229, 241, 251);
        private static readonly Color LightPressed = Color.FromArgb(204, 228, 247);
        private static readonly Color LightChevron = Color.FromArgb(110, 110, 110);

        private const int OverflowIndex = -2;
        private const int NoIndex = -1;

        private readonly TextBox editBox = new TextBox();
        private readonly ContextMenuStrip overflowMenu = new ContextMenuStrip();
        private readonly ToolTip toolTip = new ToolTip();
        private readonly List<Crumb> crumbs = new List<Crumb>();
        private Rectangle overflowBounds;
        private bool overflowVisible;
        private string folderPath = "";
        private bool darkMode;
        private int hotIndex = NoIndex;
        private int pressedIndex = NoIndex;
        private string toolTipText;

        // Raised with the folder a segment points to, or with the text typed in edit mode
        public event EventHandler<string> NavigateRequested;

        public BreadcrumbBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            // Only the text box takes the focus, so clicking the bar doesn't steal it back
            SetStyle(ControlStyles.Selectable, false);
            TabStop = false;

            editBox.BorderStyle = BorderStyle.None;
            editBox.Visible = false;
            editBox.KeyDown += editBox_KeyDown;
            editBox.LostFocus += (s, e) => EndEdit();
            Controls.Add(editBox);

            overflowMenu.ShowImageMargin = false;
            overflowMenu.Closed += (s, e) => { pressedIndex = NoIndex; Invalidate(); };
        }

        public bool DarkMode
        {
            get { return darkMode; }
            set
            {
                darkMode = value;
                editBox.BackColor = BarBack;
                editBox.ForeColor = darkMode ? Theme.Text : SystemColors.WindowText;
                if (editBox.IsHandleCreated)
                    Theme.SetWindowTheme(editBox.Handle, darkMode ? "DarkMode_CFD" : null, null);
                if (darkMode)
                    overflowMenu.Renderer = new DarkToolStripRenderer();
                else
                    overflowMenu.RenderMode = ToolStripRenderMode.ManagerRenderMode;
                Invalidate();
            }
        }

        // The folder shown. Setting it leaves edit mode.
        public string FolderPath
        {
            get { return folderPath; }
            set
            {
                folderPath = value ?? "";
                BuildCrumbs();
                EndEdit();
                LayoutCrumbs();
                Invalidate();
            }
        }

        // Where the focus goes when typing ends with Esc or Enter
        public Control ReturnFocusTo { get; set; }

        public bool Editing
        {
            get { return editBox.Visible; }
        }

        public void BeginEdit()
        {
            if (Editing)
            {
                editBox.SelectAll();
                return;
            }
            hotIndex = NoIndex;
            toolTip.Hide(this);
            editBox.Text = folderPath;
            PlaceEditBox();
            editBox.Visible = true;
            editBox.Focus();
            editBox.SelectAll();
            Invalidate();
        }

        public void EndEdit()
        {
            if (!Editing)
                return;
            // Hide first, so the focus move below doesn't come back here through LostFocus
            bool hadFocus = editBox.Focused;
            editBox.Visible = false;
            if (hadFocus && ReturnFocusTo != null && ReturnFocusTo.CanFocus)
                ReturnFocusTo.Focus();
            Invalidate();
        }

        private Color BarBack
        {
            get { return darkMode ? Theme.InputBack : SystemColors.Window; }
        }

        // One crumb per folder from the drive (or network share) down
        private void BuildCrumbs()
        {
            crumbs.Clear();
            if (folderPath == "")
                return;
            try
            {
                for (DirectoryInfo dir = new DirectoryInfo(folderPath); dir != null; dir = dir.Parent)
                {
                    string text = dir.Parent == null ? dir.FullName.TrimEnd('\\') : dir.Name;
                    crumbs.Insert(0, new Crumb { Text = text, Path = dir.FullName });
                }
            }
            catch (Exception)
            {
                crumbs.Clear();
                crumbs.Add(new Crumb { Text = folderPath, Path = folderPath });
            }
        }

        private int Pad { get { return LogicalToDeviceUnits(6); } }
        private int ChevronWidth { get { return LogicalToDeviceUnits(14); } }
        private int Inset { get { return LogicalToDeviceUnits(2); } }

        // Rightmost segments first, so the current folder always shows; anything that doesn't
        // fit (keeping some empty space to click for typing) goes behind the « button
        private void LayoutCrumbs()
        {
            overflowVisible = false;
            if (crumbs.Count == 0)
                return;

            const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            int[] widths = new int[crumbs.Count];
            int total = 0;
            for (int i = 0; i < crumbs.Count; i++)
            {
                widths[i] = TextRenderer.MeasureText(crumbs[i].Text, Font, Size.Empty, flags).Width + 2 * Pad;
                total += widths[i] + (i > 0 ? ChevronWidth : 0);
            }

            int available = Width - 2 * Inset - LogicalToDeviceUnits(24);
            int first = 0;
            if (total > available)
            {
                int overflowWidth = TextRenderer.MeasureText("«", Font, Size.Empty, flags).Width + 2 * Pad + ChevronWidth;
                int used = overflowWidth + widths[crumbs.Count - 1];
                first = crumbs.Count - 1;
                while (first > 0 && used + widths[first - 1] + ChevronWidth <= available)
                {
                    first--;
                    used += widths[first] + ChevronWidth;
                }
                overflowVisible = first > 0;
            }

            int height = Height - 2 * Inset;
            int x = Inset;
            if (overflowVisible)
            {
                int w = TextRenderer.MeasureText("«", Font, Size.Empty, flags).Width + 2 * Pad;
                overflowBounds = new Rectangle(x, Inset, w, height);
                x += w + ChevronWidth;
            }
            for (int i = 0; i < crumbs.Count; i++)
            {
                crumbs[i].Visible = i >= first;
                if (!crumbs[i].Visible)
                    continue;
                if (i > first)
                    x += ChevronWidth;
                // The current folder alone may still be too long, so it gets cut with an ellipsis
                int w = Math.Min(widths[i], Math.Max(0, Width - Inset - x));
                crumbs[i].Bounds = new Rectangle(x, Inset, w, height);
                x += w;
            }
        }

        private void PlaceEditBox()
        {
            int x = LogicalToDeviceUnits(6);
            editBox.Width = Math.Max(0, Width - x - LogicalToDeviceUnits(4));
            editBox.Location = new Point(x, (Height - editBox.Height) / 2);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            editBox.Font = Font;
            LayoutCrumbs();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutCrumbs();
            PlaceEditBox();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            DarkMode = darkMode;
        }

        // The current folder isn't a link (it's where you already are), so clicking it types,
        // the same as clicking empty space
        private int HitTest(Point p)
        {
            if (overflowVisible && overflowBounds.Contains(p))
                return OverflowIndex;
            for (int i = 0; i < crumbs.Count - 1; i++)
                if (crumbs[i].Visible && crumbs[i].Bounds.Contains(p))
                    return i;
            return NoIndex;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int hit = HitTest(e.Location);
            if (hit != hotIndex)
            {
                hotIndex = hit;
                Invalidate();
            }
            Cursor = hit == NoIndex ? Cursors.IBeam : Cursors.Default;

            // Tooltips for cut-off names and the « button
            string tip = null;
            if (hit == OverflowIndex)
                tip = "Show the rest of the path";
            else if (hit == NoIndex && crumbs.Count > 0 && crumbs[crumbs.Count - 1].Bounds.Contains(e.Location))
                tip = crumbs[crumbs.Count - 1].Path;
            else if (hit >= 0)
                tip = crumbs[hit].Path;
            if (tip != toolTipText)
            {
                toolTipText = tip;
                toolTip.SetToolTip(this, tip ?? "");
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (hotIndex != NoIndex && pressedIndex != OverflowIndex)
            {
                hotIndex = NoIndex;
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;
            int hit = HitTest(e.Location);
            if (hit == NoIndex)
            {
                BeginEdit();
                return;
            }
            pressedIndex = hit;
            Invalidate();
            if (hit == OverflowIndex)
                ShowOverflowMenu();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (pressedIndex >= 0 && HitTest(e.Location) == pressedIndex)
            {
                string path = crumbs[pressedIndex].Path;
                pressedIndex = NoIndex;
                Invalidate();
                NavigateRequested?.Invoke(this, path);
                return;
            }
            if (pressedIndex != OverflowIndex)
            {
                pressedIndex = NoIndex;
                Invalidate();
            }
        }

        // Nearest folder first, the same order as File Explorer
        private void ShowOverflowMenu()
        {
            overflowMenu.Items.Clear();
            for (int i = crumbs.Count - 1; i >= 0; i--)
            {
                if (crumbs[i].Visible)
                    continue;
                string path = crumbs[i].Path;
                ToolStripMenuItem item = new ToolStripMenuItem(crumbs[i].Text);
                item.Click += (s, e) => NavigateRequested?.Invoke(this, path);
                overflowMenu.Items.Add(item);
            }
            overflowMenu.Show(this, new Point(overflowBounds.Left, Height));
        }

        private void editBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                NavigateRequested?.Invoke(this, editBox.Text);
            }
            else if (e.KeyCode == Keys.Escape)
            {
                e.SuppressKeyPress = true;
                EndEdit();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush back = new SolidBrush(BarBack))
                g.FillRectangle(back, ClientRectangle);
            Color border = darkMode ? (Editing ? Theme.DimText : Theme.Border) : (Editing ? Theme.Accent : LightBorder);
            using (Pen edge = new Pen(border))
                g.DrawRectangle(edge, 0, 0, Width - 1, Height - 1);
            if (Editing)
                return;

            Color text = darkMode ? Theme.Text : SystemColors.WindowText;
            Color chevron = darkMode ? Theme.DimText : LightChevron;
            const TextFormatFlags flags = TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine |
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter | TextFormatFlags.EndEllipsis;

            bool first = true;
            if (overflowVisible)
            {
                PaintHighlight(g, overflowBounds, OverflowIndex);
                TextRenderer.DrawText(g, "«", Font, overflowBounds, text, flags);
                first = false;
            }
            for (int i = 0; i < crumbs.Count; i++)
            {
                Crumb crumb = crumbs[i];
                if (!crumb.Visible)
                    continue;
                if (!first)
                    PaintChevron(g, crumb.Bounds.Left - ChevronWidth, chevron);
                first = false;
                PaintHighlight(g, crumb.Bounds, i);
                TextRenderer.DrawText(g, crumb.Text, Font, crumb.Bounds, text, flags);
            }
        }

        private void PaintHighlight(Graphics g, Rectangle bounds, int index)
        {
            Color? fill = null;
            if (index == pressedIndex)
                fill = darkMode ? Theme.Pressed : LightPressed;
            else if (index == hotIndex)
                fill = darkMode ? Theme.Hover : LightHover;
            if (fill == null)
                return;
            using (SolidBrush brush = new SolidBrush(fill.Value))
                g.FillRectangle(brush, bounds);
        }

        private void PaintChevron(Graphics g, int left, Color color)
        {
            int s = LogicalToDeviceUnits(3);
            int cx = left + ChevronWidth / 2;
            int cy = Height / 2;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen pen = new Pen(color, Math.Max(1f, LogicalToDeviceUnits(1) * 1.2f)))
                g.DrawLines(pen, new[] { new Point(cx - s / 2, cy - s), new Point(cx + s / 2 + 1, cy), new Point(cx - s / 2, cy + s) });
            g.SmoothingMode = SmoothingMode.None;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                overflowMenu.Dispose();
                toolTip.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
