using System;
using System.Windows.Forms;
using FFXIVClientManager.Forms; // Corrected: reference the proper Forms namespace

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
            // Initialize application configuration for Windows Forms (available in .NET 6+ / .NET 8)
            ApplicationConfiguration.Initialize();

            // Run the main form
            Application.Run(new MainForm());
        }
    }
}
