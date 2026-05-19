using ApplicationShell;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HeroVirtualDesktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            StartApplication();
        }
        public static Bootstrapper appBootstrapper { get; set; }
        private void StartApplication()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture =
                System.Threading.Thread.CurrentThread.CurrentCulture;

            appBootstrapper = new Bootstrapper();
            appBootstrapper.Run();
        }
    }
}
