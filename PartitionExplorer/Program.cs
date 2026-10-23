using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PartitionExplorer
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args = null)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // .NET 8 defaults to Segoe UI 9pt; keep the font the fixed layout was designed for
            Application.SetDefaultFont(new Font("Microsoft Sans Serif", 8.25F));
            string path = "";
            if (args.Length == 0)
                path = "";
            else
                path = args[0];
            Application.Run(new Form1(path));
        }
    }
}
