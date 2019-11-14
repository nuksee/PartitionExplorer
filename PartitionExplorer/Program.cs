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
            try
            {
                Microsoft.Win32.RegistryKey key;
                Microsoft.Win32.RegistryKey key2;
                Microsoft.Win32.RegistryKey key3;
                key = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("Directory").OpenSubKey("shell");
                key2 = key.CreateSubKey("OpenInPartitionExplorer");
                key2.SetValue("(Default)", "Open In PartitionExplorer");
                key3 = key2.CreateSubKey("command");
                key3.SetValue("(Default)", "\"C:\\Program Files\\PartitionExplorer\\PartitionExplorer.exe\" \"%1\"");

                key3.Close();
                key2.Close();
                key.Close();
            }
            catch (Exception ex)
            {
            }


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
