using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PartitionExplorer
{
    // Flat folder and file icons drawn at whatever size the list needs, so they stay
    // sharp at any DPI and no binary image streams are needed in the .resx
    internal static class FlatIcons
    {
        public const int Folder = 0;
        public const int File = 1;

        private static readonly Color FolderBack = Color.FromArgb(226, 165, 52);
        private static readonly Color FolderFront = Color.FromArgb(248, 201, 75);
        private static readonly Color PageFill = Color.White;
        private static readonly Color PageEdge = Color.FromArgb(138, 138, 138);
        private static readonly Color PageFold = Color.FromArgb(218, 218, 218);
        private static readonly Color PageLines = Color.FromArgb(190, 196, 204);

        public static ImageList Create(int size)
        {
            ImageList list = new ImageList();
            list.ColorDepth = ColorDepth.Depth32Bit;
            list.ImageSize = new Size(size, size);
            list.Images.Add(Draw(size, DrawFolder));
            list.Images.Add(Draw(size, DrawFile));
            return list;
        }

        // Monochrome toolbar glyphs, drawn in the theme's text colour
        public static Bitmap RefreshGlyph(int size, Color color)
        {
            return Draw(size, (g, u, s) =>
            {
                using (Pen pen = new Pen(color, 1.4f * u))
                using (SolidBrush brush = new SolidBrush(color))
                {
                    // Clockwise from the upper right round to the top, then an arrowhead
                    g.DrawArc(pen, 2.6f * u, 2.6f * u, 10.8f * u, 10.8f * u, -30, 300);
                    g.FillPolygon(brush, new[] {
                        new PointF(10.6f * u, 2.6f * u), new PointF(7.4f * u, 0.4f * u), new PointF(7.4f * u, 4.8f * u) });
                }
            });
        }

        public static Bitmap OpenGlyph(int size, Color color)
        {
            return Draw(size, (g, u, s) =>
            {
                using (Pen pen = new Pen(color, 1.4f * u) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
                {
                    // A box left open at the top right, and an arrow leaving through the gap
                    g.DrawLines(pen, new[] {
                        new PointF(7 * u, 3 * u), new PointF(2.5f * u, 3 * u), new PointF(2.5f * u, 13.5f * u),
                        new PointF(13 * u, 13.5f * u), new PointF(13 * u, 9 * u) });
                    g.DrawLine(pen, 7.5f * u, 8.5f * u, 13.5f * u, 2.5f * u);
                    g.DrawLines(pen, new[] { new PointF(9.5f * u, 2.5f * u), new PointF(13.5f * u, 2.5f * u), new PointF(13.5f * u, 6.5f * u) });
                }
            });
        }

        private delegate void Painter(Graphics g, float u, int size);

        // Shapes are laid out on a 16x16 grid and scaled by u
        private static Bitmap Draw(int size, Painter paint)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                paint(g, size / 16f, size);
            }
            return bmp;
        }

        private static void DrawFolder(Graphics g, float u, int size)
        {
            using (GraphicsPath back = new GraphicsPath())
            using (GraphicsPath front = RoundedRect(new RectangleF(1 * u, 5.5f * u, 14 * u, 8.5f * u), 1.2f * u))
            using (SolidBrush backBrush = new SolidBrush(FolderBack))
            using (SolidBrush frontBrush = new SolidBrush(FolderFront))
            {
                // Tab on the top left, then the body behind the front panel
                back.AddPolygon(new[] {
                    new PointF(1 * u, 3 * u), new PointF(6 * u, 3 * u), new PointF(7.5f * u, 4.3f * u),
                    new PointF(15 * u, 4.3f * u), new PointF(15 * u, 12 * u), new PointF(1 * u, 12 * u) });
                g.FillPath(backBrush, back);
                g.FillPath(frontBrush, front);
            }
        }

        private static void DrawFile(Graphics g, float u, int size)
        {
            PointF[] page = {
                new PointF(3 * u, 1 * u), new PointF(10 * u, 1 * u), new PointF(13 * u, 4 * u),
                new PointF(13 * u, 15 * u), new PointF(3 * u, 15 * u) };
            PointF[] fold = { new PointF(10 * u, 1 * u), new PointF(10 * u, 4 * u), new PointF(13 * u, 4 * u) };
            using (SolidBrush fill = new SolidBrush(PageFill))
            using (SolidBrush foldBrush = new SolidBrush(PageFold))
            using (Pen edge = new Pen(PageEdge, System.Math.Max(1f, u * 0.8f)))
            {
                g.FillPolygon(fill, page);
                g.FillPolygon(foldBrush, fold);
                g.DrawPolygon(edge, page);
                g.DrawLines(edge, fold);
            }
            // Text lines only read well from 24px up
            if (size >= 24)
            {
                using (Pen line = new Pen(PageLines, u * 0.7f))
                {
                    for (float y = 7; y <= 12.5f; y += 1.8f)
                        g.DrawLine(line, 5 * u, y * u, 11 * u, y * u);
                }
            }
        }

        private static GraphicsPath RoundedRect(RectangleF r, float radius)
        {
            float d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
