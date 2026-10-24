using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PartitionExplorer
{
    // Squarified treemap of the current folder. Each top-level box is split into its own
    // subfolders one level down; clicking a box raises ItemClicked with that box's Tag.
    public class TreemapControl : Control
    {
        public class Item
        {
            public string Name;
            public long Size;
            public object Tag;
            public Color Color;
            public List<Item> Children = new List<Item>();
        }

        private class Box
        {
            public RectangleF Rect;
            public Item Item;
            public Item Parent;
        }

        private const int HeaderMinWidth = 60;
        private const int HeaderMinHeight = 40;

        private List<Item> items = new List<Item>();
        private List<Box> boxes = new List<Box>();
        private long total;
        private object selectedTag;
        private Box hoverBox;
        private bool darkMode;
        private ToolTip toolTip = new ToolTip();

        public event EventHandler<object> ItemClicked;

        // Tells the control whether a tag is the selected row (folders are compared by path)
        public Func<object, object, bool> TagEquals = (a, b) => Equals(a, b);

        public TreemapControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            BackColor = Color.White;
        }

        public void SetItems(IEnumerable<Item> newItems)
        {
            items = newItems.Where(i => i.Size > 0).OrderByDescending(i => i.Size).ToList();
            total = items.Sum(i => i.Size);
            hoverBox = null;
            UpdateLayout();
            Invalidate();
        }

        // Subfolder boxes are blended towards BackColor, so they come out lighter than their
        // parent on white and darker on a dark background; dark mode switches the text to suit
        public bool DarkMode
        {
            get { return darkMode; }
            set
            {
                darkMode = value;
                Invalidate();
            }
        }

        public object SelectedTag
        {
            get { return selectedTag; }
            set
            {
                selectedTag = value;
                Invalidate();
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateLayout();
        }

        private void UpdateLayout()
        {
            boxes.Clear();
            RectangleF area = new RectangleF(2, 2, Width - 4, Height - 4);
            if (area.Width <= 0 || area.Height <= 0 || total <= 0)
                return;

            foreach (KeyValuePair<Item, RectangleF> top in Squarify(items, area))
            {
                boxes.Add(new Box { Rect = top.Value, Item = top.Key });
                RectangleF inner = ChildArea(top.Value);
                if (inner.IsEmpty || top.Key.Children.Count < 2)
                    continue;
                List<Item> kids = top.Key.Children.Where(c => c.Size > 0).OrderByDescending(c => c.Size).ToList();
                foreach (KeyValuePair<Item, RectangleF> kid in Squarify(kids, inner))
                    boxes.Add(new Box { Rect = kid.Value, Item = kid.Key, Parent = top.Key });
            }
        }

        private int HeaderHeight
        {
            get { return Font.Height + 4; }
        }

        private RectangleF ChildArea(RectangleF r)
        {
            if (r.Width < HeaderMinWidth || r.Height < HeaderMinHeight)
                return RectangleF.Empty;
            return new RectangleF(r.X + 3, r.Y + HeaderHeight, r.Width - 6, r.Height - HeaderHeight - 3);
        }

        // Bruls, Huizing and van Wijk's squarified layout: fill rows along the short side,
        // adding items while that keeps the row's worst aspect ratio from getting worse.
        private static List<KeyValuePair<Item, RectangleF>> Squarify(List<Item> list, RectangleF area)
        {
            List<KeyValuePair<Item, RectangleF>> result = new List<KeyValuePair<Item, RectangleF>>();
            double sum = list.Sum(i => (double)i.Size);
            if (sum <= 0)
                return result;
            double scale = area.Width * area.Height / sum;
            Queue<Item> rest = new Queue<Item>(list);
            double x = area.X, y = area.Y, w = area.Width, h = area.Height;

            while (rest.Count > 0)
            {
                double side = Math.Min(w, h);
                List<Item> row = new List<Item>();
                double rowArea = 0, best = double.MaxValue;
                while (rest.Count > 0)
                {
                    double a = rest.Peek().Size * scale;
                    double newArea = rowArea + a;
                    double maxA = Math.Max(row.Count > 0 ? row.Max(i => i.Size * scale) : 0, a);
                    double minA = Math.Min(row.Count > 0 ? row.Min(i => i.Size * scale) : double.MaxValue, a);
                    double worst = Math.Max(side * side * maxA / (newArea * newArea), newArea * newArea / (side * side * minA));
                    if (row.Count > 0 && worst > best)
                        break;
                    best = worst;
                    row.Add(rest.Dequeue());
                    rowArea = newArea;
                }

                if (w >= h)
                {
                    double rw = rowArea / h, yy = y;
                    foreach (Item i in row)
                    {
                        double ih = i.Size * scale / rw;
                        result.Add(new KeyValuePair<Item, RectangleF>(i, new RectangleF((float)x, (float)yy, (float)rw, (float)ih)));
                        yy += ih;
                    }
                    x += rw;
                    w -= rw;
                }
                else
                {
                    double rh = rowArea / w, xx = x;
                    foreach (Item i in row)
                    {
                        double iw = i.Size * scale / rh;
                        result.Add(new KeyValuePair<Item, RectangleF>(i, new RectangleF((float)xx, (float)y, (float)iw, (float)rh)));
                        xx += iw;
                    }
                    y += rh;
                    h -= rh;
                }
            }
            return result;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            if (boxes.Count == 0)
            {
                TextRenderer.DrawText(g, "Nothing to show", Font, ClientRectangle, darkMode ? Theme.DimText : SystemColors.GrayText,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            using (Font bold = new Font(Font, FontStyle.Bold))
            using (Pen gap = new Pen(BackColor, 2))
            {
                foreach (Box b in boxes)
                {
                    Rectangle r = Rectangle.Round(b.Rect);
                    if (b.Parent == null)
                    {
                        using (SolidBrush fill = new SolidBrush(b.Item.Color))
                            g.FillRectangle(fill, r);
                        g.DrawRectangle(gap, r);
                        if (!ChildArea(b.Rect).IsEmpty)
                            DrawLabel(g, b.Item.Name + " · " + Form1.FormatSize(b.Item.Size), bold, Color.White,
                                new Rectangle(r.X + 4, r.Y + 2, r.Width - 8, HeaderHeight - 2));
                        else if (r.Width > 30 && r.Height > Font.Height)
                            DrawLabel(g, b.Item.Name, Font, Color.White, new Rectangle(r.X + 2, r.Y + 1, r.Width - 4, Font.Height));
                    }
                    else
                    {
                        int index = b.Parent.Children.IndexOf(b.Item);
                        // Pushed further on dark, where the bright palette colours need to come down more
                        float amount = (darkMode ? 0.35f : 0.2f) + 0.12f * (index % 3);
                        Color color = Blend(b.Parent.Color, BackColor, amount);
                        using (SolidBrush fill = new SolidBrush(color))
                            g.FillRectangle(fill, r);
                        using (Pen edge = new Pen(b.Parent.Color))
                            g.DrawRectangle(edge, r);
                        if (r.Width > 50 && r.Height > Font.Height * 2 + 4)
                        {
                            bool lightText = darkMode && Luma(color) < 0.55;
                            Color sizeColor = lightText ? Color.FromArgb(200, 200, 200) : darkMode ? Color.FromArgb(50, 50, 50) : Color.DimGray;
                            DrawLabel(g, b.Item.Name, Font, lightText ? Theme.Text : Color.Black, new Rectangle(r.X + 3, r.Y + 2, r.Width - 6, Font.Height));
                            DrawLabel(g, Form1.FormatSize(b.Item.Size), Font, sizeColor,
                                new Rectangle(r.X + 3, r.Y + 2 + Font.Height, r.Width - 6, Font.Height));
                        }
                    }
                }
            }

            Box selected = boxes.FirstOrDefault(b => b.Parent == null && selectedTag != null && b.Item.Tag != null && TagEquals(b.Item.Tag, selectedTag));
            if (selected != null)
            {
                using (Pen outline = new Pen(darkMode ? Color.White : Color.FromArgb(17, 17, 17), 3) { Alignment = PenAlignment.Inset })
                    g.DrawRectangle(outline, Rectangle.Round(selected.Rect));
            }
            if (hoverBox != null && hoverBox.Item.Tag is System.IO.DirectoryInfo)
            {
                using (Pen hover = new Pen(Color.FromArgb(160, darkMode ? Color.White : Color.Black), 1) { DashStyle = DashStyle.Dash })
                    g.DrawRectangle(hover, Rectangle.Round(hoverBox.Rect));
            }
        }

        private static void DrawLabel(Graphics g, string text, Font font, Color color, Rectangle r)
        {
            if (r.Width <= 0 || r.Height <= 0)
                return;
            TextRenderer.DrawText(g, text, font, r, color,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine);
        }

        private static double Luma(Color c)
        {
            return (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255;
        }

        private static Color Blend(Color c, Color toward, float amount)
        {
            return Color.FromArgb(
                (int)(c.R + (toward.R - c.R) * amount),
                (int)(c.G + (toward.G - c.G) * amount),
                (int)(c.B + (toward.B - c.B) * amount));
        }

        // Subfolder boxes sit on top of their parent, so search them first
        private Box BoxAt(Point p)
        {
            for (int i = boxes.Count - 1; i >= 0; i--)
            {
                if (boxes[i].Rect.Contains(p))
                    return boxes[i];
            }
            return null;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            Box b = BoxAt(e.Location);
            if (b == hoverBox)
                return;
            hoverBox = b;
            Cursor = b != null && b.Item.Tag is System.IO.DirectoryInfo ? Cursors.Hand : Cursors.Default;
            if (b == null)
            {
                toolTip.Hide(this);
            }
            else
            {
                string text = b.Item.Name + "\n" + Form1.FormatSize(b.Item.Size);
                if (total > 0)
                    text += " (" + (b.Item.Size * 100.0 / total).ToString("0.0") + " % of this folder)";
                if (b.Parent != null)
                    text += "\nin " + b.Parent.Name;
                toolTip.Show(text, this, e.X + 16, e.Y + 16);
            }
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverBox = null;
            toolTip.Hide(this);
            Cursor = Cursors.Default;
            Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Box b = BoxAt(e.Location);
            if (b != null && b.Item.Tag != null && ItemClicked != null)
                ItemClicked(this, b.Item.Tag);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                toolTip.Dispose();
            base.Dispose(disposing);
        }
    }
}
