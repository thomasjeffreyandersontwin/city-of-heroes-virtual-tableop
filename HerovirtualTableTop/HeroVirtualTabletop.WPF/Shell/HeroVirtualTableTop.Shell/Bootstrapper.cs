 using ApplicationShell.Models.Navigation;
using ApplicationShell.Views;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop;
using Module.Shared.Enumerations;
using Module.Shared.Error;
using Prism.Events;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace ApplicationShell
{
    public class Bootstrapper : UnityBootstrapper
    {
        public IEventAggregator eventAggregator;

        Module.Shared.Logging.ILogManager logService = new Module.Shared.Logging.FileLogManager(typeof(Bootstrapper));

        /// <summary>
        /// Initialize the shell
        /// </summary>
        /// <returns> reference to shell</returns>

        protected override DependencyObject CreateShell()
        {
            Application.Current.DispatcherUnhandledException += new System.Windows.Threading.DispatcherUnhandledExceptionEventHandler(App_DispatcherUnhandledException);
            System.AppDomain.CurrentDomain.UnhandledException += new System.UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            MainWindowV2 shell = Container.Resolve<MainWindowV2>();
            Application.Current.MainWindow = shell;

            shell.Dispatcher.BeginInvoke((Action)delegate   //This takes advantage of the fact that the Bootstrapper and ModuleLoader run on 
            {                                           //the UI thread and queues a delegate on the Dispatcher that will only 
                shell.Show();                           //get invoked when all the other stuff is done. 
                NavigationManager.Navigate(ModuleEnum.HeroVirtualTabletop);
            });

            return shell;
        }

        void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e)
        {
           logService.Error("Unhandled Exception");
        }

        void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            if (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            string message = ex.Message;
            message += Environment.NewLine + "Stack Trace:";
            message += Environment.NewLine + ex.StackTrace;
            ErrorItems errors = new ErrorItems();
            errors.Errors.Add(
                new ErrorItem
                {
                    ErrorDisplayType = ErrorDisplayType.Error,
                    FriendlyMessage = message,
                    Error = ex,
                });
            eventAggregator = Container.Resolve<IEventAggregator>();
            eventAggregator.GetEvent<Module.Shared.Events.CommonEvents.ErrorOccuredEvent>().Publish(errors);
            Container.Resolve<IBusyService>().HideAllBusy();
            logService.Error(message);
            e.Handled = true;
        }

        protected override IModuleCatalog CreateModuleCatalog() 
        {
            ModuleCatalog moduleCatalog = new Prism.Modularity.ModuleCatalog();
            moduleCatalog.AddModule(typeof(HeroVirtualTabletopModule), InitializationMode.OnDemand);

            return moduleCatalog;
        }

        protected override void ConfigureContainer()
        {
            base.ConfigureContainer();

            Uri iconUri = new Uri
           ("pack://application:,,,/Module.Shared;Component/Resources/Images/appIcon2.gif");// Not showing in the message. Need to investigate later
            //("Resources/Images/application.ico", UriKind.Relative);
            ImageSource icon = BitmapFrame.Create(iconUri);

            Container.RegisterType<HeroVirtualTabletopModule>(new ContainerControlledLifetimeManager());

            Container.RegisterType<IBusyService, BusyService>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IPopupService, PopupService>(new ContainerControlledLifetimeManager());
            Container.RegisterType<IMessageBoxService, MessageBoxService>(new ContainerControlledLifetimeManager(), new InjectionConstructor(icon));
        }


    }
}
