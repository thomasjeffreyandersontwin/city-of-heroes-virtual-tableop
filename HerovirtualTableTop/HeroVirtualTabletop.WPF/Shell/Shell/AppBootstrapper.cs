namespace Shell {
    using System;
    using System.Collections.Generic;
    using Caliburn.Micro;
    using HeroVirtualTableTop.Common;
    using System.Reflection;

    public class AppBootstrapper : BootstrapperBase {
        SimpleContainer container;

        public AppBootstrapper() {
            Initialize();
        }

        protected override void Configure() {
            container = new SimpleContainer();

            container.Singleton<IWindowManager, WindowManager>();
            container.Singleton<IEventAggregator, EventAggregator>();
            container.PerRequest<IShell, ShellViewModelImpl>();
            container.Singleton<HeroVirtualTabletopMainViewModel, HeroVirtualTabletopMainViewModelImpl>();

            ViewLocator.NameTransformer.AddRule("ModelImpl$", "");
            
        }

        protected override object GetInstance(Type service, string key) {
            return container.GetInstance(service, key);
        }

        protected override IEnumerable<object> GetAllInstances(Type service) {
            return container.GetAllInstances(service);
        }

        protected override void BuildUp(object instance) {
            container.BuildUp(instance);
        }

        protected override void OnStartup(object sender, System.Windows.StartupEventArgs e) {
            DisplayRootViewFor<IShell>();
        }

        protected override IEnumerable<Assembly> SelectAssemblies()
        {
            return new[] { Assembly.GetExecutingAssembly(), Assembly.Load("HeroVirtualTableTop"), };
        }
    }
}