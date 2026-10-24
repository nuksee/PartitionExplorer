using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace PartitionExplorer
{
    // Controls whose dark look needs more than BackColor and ForeColor. Each one keeps its
    // standard Windows look while DarkMode is off.

    // Uses Windows' own dark list theme (selection, hover, scroll bars), and sets the header
    // text colour, which the dark header theme leaves black.
    public class ThemedListView : ListView
    {
        private const int LVM_GETHEADER = 0x101F;
        private const int LVM_GETTOOLTIPS = 0x104E;
        private const int WM_NOTIFY = 0x004E;
        private const int NM_CUSTOMDRAW = -12;
        private const int CDDS_PREPAINT = 0x1;
        private const int CDDS_ITEMPREPAINT = 0x10001;
        private const int CDRF_NOTIFYITEMDRAW = 0x20;
        private const int CDRF_DODEFAULT = 0x0;

        [StructLayout(LayoutKind.Sequential)]
        private struct NMHDR
        {
            public IntPtr hwndFrom;
            public IntPtr idFrom;
            public int code;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NMCUSTOMDRAW
        {
            public NMHDR hdr;
            public int dwDrawStage;
            public IntPtr hdc;
            public int left, top, right, bottom;
            public IntPtr dwItemSpec;
            public int uItemState;
            public IntPtr lItemlParam;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("gdi32.dll")]
        private static extern int SetTextColor(IntPtr hdc, int color);

        private bool darkMode;

        public bool DarkMode
        {
            get { return darkMode; }
            set
            {
                darkMode = value;
                UpdateTheme();
            }
        }

        // The header only exists once the list has been in Details view, so this runs
        // again whenever the view changes
        public void UpdateTheme()
        {
            if (!IsHandleCreated)
                return;
            // Same list styling as File Explorer: light hover and selection, flat headers
            Theme.SetWindowTheme(Handle, darkMode ? "DarkMode_Explorer" : "Explorer", null);
            IntPtr header = SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (header != IntPtr.Zero)
            {
                Theme.SetWindowTheme(header, darkMode ? "DarkMode_ItemsView" : null, null);
                InvalidateRect(header, IntPtr.Zero, true);
            }
            IntPtr tooltips = SendMessage(Handle, LVM_GETTOOLTIPS, IntPtr.Zero, IntPtr.Zero);
            if (tooltips != IntPtr.Zero)
                Theme.SetWindowTheme(tooltips, darkMode ? "DarkMode_Explorer" : null, null);
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            UpdateTheme();
        }

        protected override void WndProc(ref Message m)
        {
            if (darkMode && m.Msg == WM_NOTIFY)
            {
                NMHDR hdr = Marshal.PtrToStructure<NMHDR>(m.LParam);
                if (hdr.code == NM_CUSTOMDRAW && hdr.hwndFrom == SendMessage(Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero))
                {
                    NMCUSTOMDRAW draw = Marshal.PtrToStructure<NMCUSTOMDRAW>(m.LParam);
                    if (draw.dwDrawStage == CDDS_PREPAINT)
                    {
                        m.Result = (IntPtr)CDRF_NOTIFYITEMDRAW;
                        return;
                    }
                    if (draw.dwDrawStage == CDDS_ITEMPREPAINT)
                    {
                        SetTextColor(draw.hdc, ColorTranslator.ToWin32(Theme.Text));
                        m.Result = (IntPtr)CDRF_DODEFAULT;
                        return;
                    }
                }
            }
            base.WndProc(ref m);
        }
    }

    // The flat combo box paints a white outer border and a light grey drop-down button
    // whatever its BackColor, so in dark mode those are painted over once it has drawn.
    public class ThemedComboBox : ComboBox
    {
        private const int WM_PAINT = 0x000F;
        private bool darkMode;

        public bool DarkMode
        {
            get { return darkMode; }
            set
            {
                darkMode = value;
                Invalidate();
            }
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (darkMode && m.Msg == WM_PAINT && FlatStyle == FlatStyle.Flat && IsHandleCreated)
            {
                using (Graphics g = Graphics.FromHwnd(Handle))
                    PaintDarkFrame(g);
            }
        }

        // Same geometry as the framework's flat combo box adapter
        private void PaintDarkFrame(Graphics g)
        {
            Rectangle client = ClientRectangle;
            int buttonWidth = SystemInformation.GetHorizontalScrollBarArrowWidthForDpi(DeviceDpi);
            Rectangle button = new Rectangle(client.Width - 1 - buttonWidth, 1, buttonWidth, client.Height - 2);
            using (SolidBrush back = new SolidBrush(BackColor))
                g.FillRectangle(back, button);

            int s = LogicalToDeviceUnits(3);
            Point middle = new Point(button.Left + button.Width / 2, button.Top + button.Height / 2);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (Pen arrow = new Pen(Enabled ? ForeColor : Theme.DimText, Math.Max(1f, LogicalToDeviceUnits(1) * 1.2f)))
                g.DrawLines(arrow, new[] {
                    new Point(middle.X - s, middle.Y - s / 2), new Point(middle.X, middle.Y + s / 2 + 1), new Point(middle.X + s, middle.Y - s / 2) });
            g.SmoothingMode = SmoothingMode.None;

            using (Pen edge = new Pen(ContainsFocus ? Theme.DimText : Theme.Border))
                g.DrawRectangle(edge, client.X, client.Y, client.Width - 1, client.Height - 1);
        }
    }

    // The visual-styles check box glyph ignores BackColor, so in dark mode a flat one is
    // painted over it
    public class ThemedCheckBox : CheckBox
    {
        private bool darkMode;

        public bool DarkMode
        {
            get { return darkMode; }
            set
            {
                darkMode = value;
                Invalidate();
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!darkMode)
                return;

            Size glyph = CheckBoxRenderer.GetGlyphSize(e.Graphics, CheckBoxState.UncheckedNormal);
            Rectangle box = new Rectangle(Padding.Left, (Height - glyph.Height) / 2, glyph.Width, glyph.Height);
            using (SolidBrush back = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(back, Rectangle.Inflate(box, 1, 1));

            box.Width--;
            box.Height--;
            if (Checked)
            {
                using (SolidBrush fill = new SolidBrush(Theme.Accent))
                    e.Graphics.FillRectangle(fill, box);
                float u = (box.Width + 1) / 13f;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Pen tick = new Pen(Color.White, Math.Max(1.5f, 1.6f * u)))
                    e.Graphics.DrawLines(tick, new[] {
                        new PointF(box.X + 3f * u, box.Y + 6.5f * u),
                        new PointF(box.X + 5.5f * u, box.Y + 9f * u),
                        new PointF(box.X + 10f * u, box.Y + 4f * u) });
            }
            else
            {
                bool hot = ClientRectangle.Contains(PointToClient(MousePosition));
                using (SolidBrush fill = new SolidBrush(Theme.InputBack))
                    e.Graphics.FillRectangle(fill, box);
                using (Pen edge = new Pen(hot ? Theme.Text : Theme.DimText))
                    e.Graphics.DrawRectangle(edge, box);
            }
        }
    }

    // A disabled flat button draws its text in a darker shade of BackColor, which can't be
    // seen on a dark background, so dark mode draws it dimmed instead
    public class ThemedButton : Button
    {
        private bool darkMode;

        public bool DarkMode
        {
            get { return darkMode; }
            set
            {
                darkMode = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (!darkMode || Enabled || Image != null)
                return;
            using (SolidBrush back = new SolidBrush(BackColor))
                e.Graphics.FillRectangle(back, ClientRectangle);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle, Theme.DisabledText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
