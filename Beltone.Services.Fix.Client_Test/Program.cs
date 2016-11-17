using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Beltone.Services.Fix.Client_Test
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Utilities.SystemConfigurations.Initialize();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmTest());
        }
    }
}
