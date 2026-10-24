using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PartitionExplorer
{
    // Order matches the Theme drop-down and the Theme setting
    internal enum ThemeMode { System, Light, Dark }

    // Colours for the dark theme. The light theme keeps the standard Windows colours,
    // so controls go back to their defaults (Color.Empty) instead of using a palette.
    internal static class Theme
    {
        public static readonly Color Back = Color.FromArgb(32, 32, 32);
        public static readonly Color ListBack = Color.FromArgb(25, 25, 25);
        public static readonly Color InputBack = Color.FromArgb(45, 45, 45);
        public static readonly Color Text = Color.FromArgb(240, 240, 240);
        public static readonly Color DimText = Color.FromArgb(160, 160, 160);
        public static readonly Color DisabledText = Color.FromArgb(100, 100, 100);
        public static readonly Color Border = Color.FromArgb(70, 70, 70);
        public static readonly Color Hover = Color.FromArgb(58, 58, 58);
        public static readonly Color Pressed = Color.FromArgb(72, 72, 72);
        public static readonly Color Accent = Color.FromArgb(0, 120, 212);
        public static readonly Color Link = Color.FromArgb(110, 180, 255);

        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        // "Choose your default app mode" in Settings > Personalization > Colors
        public static bool SystemUsesDark()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(PersonalizeKey))
                {
                    object value = key != null ? key.GetValue("AppsUseLightTheme") : null;
                    return value is int && (int)value == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsDark(ThemeMode mode)
        {
            return mode == ThemeMode.Dark || (mode == ThemeMode.System && SystemUsesDark());
        }

        public static void SetDarkTitleBar(IntPtr hwnd, bool dark)
        {
            int value = dark ? 1 : 0;
            // Windows 10 1809 to 1909 used the attribute's earlier, undocumented number
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref value, sizeof(int)) != 0)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref value, sizeof(int));
        }
    }

    // Flat dark look for the status bar and the right-click menu
    internal class DarkToolStripRenderer : ToolStripProfessionalRenderer
    {
        public DarkToolStripRenderer() : base(new DarkColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush back = new SolidBrush(e.ToolStrip is ToolStripDropDown ? Theme.InputBack : Theme.Back))
                e.Graphics.FillRectangle(back, e.AffectedBounds);
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (!(e.ToolStrip is ToolStripDropDown))
                return;
            using (Pen edge = new Pen(Theme.Border))
                e.Graphics.DrawRectangle(edge, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen line = new Pen(Theme.Border))
                e.Graphics.DrawLine(line, 4, y, e.Item.Width - 4, y);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            // Links already carry their link colour
            ToolStripLabel label = e.Item as ToolStripLabel;
            if (label == null || !label.IsLink)
                e.TextColor = e.Item.Enabled ? Theme.Text : Theme.DimText;
            base.OnRenderItemText(e);
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Theme.Text;
            base.OnRenderArrow(e);
        }

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected { get { return Theme.Hover; } }
            public override Color MenuItemBorder { get { return Theme.Hover; } }
            public override Color MenuItemSelectedGradientBegin { get { return Theme.Hover; } }
            public override Color MenuItemSelectedGradientEnd { get { return Theme.Hover; } }
            public override Color MenuBorder { get { return Theme.Border; } }
            public override Color ToolStripDropDownBackground { get { return Theme.InputBack; } }
            public override Color StatusStripGradientBegin { get { return Theme.Back; } }
            public override Color StatusStripGradientEnd { get { return Theme.Back; } }
            public override Color GripDark { get { return Theme.Border; } }
            public override Color GripLight { get { return Theme.Back; } }
        }
    }
}
