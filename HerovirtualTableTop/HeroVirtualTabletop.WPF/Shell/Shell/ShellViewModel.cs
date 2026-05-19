using Caliburn.Micro;
using HeroVirtualTableTop.Common;

namespace Shell {
    //public class ShellViewModel : Caliburn.Micro.PropertyChangedBase, IShell { }

    public class ShellViewModelImpl : Conductor<object>, IShell
    {
        public ShellViewModelImpl()
        {
            var heroVirtualTabletopMainViewModel = IoC.Get<HeroVirtualTabletopMainViewModel>();
            ActivateItem(heroVirtualTabletopMainViewModel);
        }
    }
}