using System;
using System.Collections.Generic;
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
            string path = "";
            if (args.Length == 0)
                path = "";
            else
                path = args[0];
            Application.Run(new Form1(path));
        }
    }
}
