using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HeroVirtualTabletop.InstallationHelper
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            string installDir = "";
            if (args != null && args.Length > 0)
            {
                installDir = args[0];
                installDir = installDir.Replace(@"\\coh.exe", "");
            }
            Application.Run(new Form1(installDir));
        }
    }
}
