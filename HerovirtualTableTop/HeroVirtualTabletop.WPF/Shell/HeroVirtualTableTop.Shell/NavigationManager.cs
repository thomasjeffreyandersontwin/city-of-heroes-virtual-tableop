using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop;
using Module.Shared;
using Module.Shared.Enumerations;
using Prism.Modularity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HeroVirtualDesktop;

namespace ApplicationShell.Models.Navigation
{
    public class NavigationManager
    {
        public static void Navigate(ModuleEnum module)
        {
            IUnityContainer container = App.appBootstrapper.Container;
            IModuleManager moduleManager = container.Resolve<IModuleManager>();

            if (module == ModuleEnum.HeroVirtualTabletop)
            {
                moduleManager.LoadModule(ModuleNames.HeroVirtualTabletop);
                HeroVirtualTabletopModule hvtModule = container.Resolve<HeroVirtualTabletopModule>();
                hvtModule.ActivateModule();
            }
        }
    }
}
