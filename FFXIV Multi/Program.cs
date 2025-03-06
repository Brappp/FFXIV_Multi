using System;
using System.Windows.Forms;
using FFXIV_Multi.Forms;
using FFXIVClientManager.Forms;

namespace FFXIVClientManager
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}