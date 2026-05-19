using Framework.WPF.Library;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library;
using Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Identities;
using Module.Shared;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Sevices;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.HCSIntegration;

namespace Module.HeroVirtualTabletop
{
    public class HeroVirtualTabletopModule : BaseModule
    {
        #region Private Fields

        private readonly IRegionManager regionManager;
        private readonly IUnityContainer container;

        private HeroVirtualTabletopModuleView view;
        private IRegion mainRegion;

        #endregion Private Fields

        #region Constructor

        public HeroVirtualTabletopModule(IRegionManager regionManager, IUnityContainer container)
        {
            this.container = container;
            this.regionManager = regionManager;
        }

        #endregion Constructor
        
        #region BaseModule Members

        protected override object ModuleView
        {
            get { return view; }
        }

        protected override IRegion ModuleRegion
        {
            get { return mainRegion; }
        }

        public override void Initialize()
        {
            this.RegisterViewsAndRepositories();

            view = this.container.Resolve<HeroVirtualTabletopModuleView>();
            mainRegion = this.regionManager.Regions[RegionNames.Instance.MainRegion];
            mainRegion.Add(view);

            IPopupService popupService = this.container.Resolve<IPopupService>();
            //popupService.Register("CharacterCrowdMainView", typeof(CharacterCrowdMainView));
            popupService.Register("ActiveCharacterWidgetView", typeof(ActiveCharacterWidgetView));
            //popupService.Register("ActiveAttackView", typeof(ActiveAttackView));
            popupService.Register("AttackTargetSelectionView", typeof(AttackTargetSelectionView));
            popupService.Register("AutoFireAttackConfigurationView", typeof(AutoFireAttackConfigurationView));
            popupService.Register("AttackConfigurationView", typeof(AttackConfigurationView));

            this.regionManager.RegisterViewWithRegion(RegionNames.Instance.HeroVirtualTabletopRegion, typeof(HeroVirtualTabletopMainView));
        }

        protected void RegisterViewsAndRepositories()
        {
            this.container.RegisterType<HeroVirtualTabletopModuleView, HeroVirtualTabletopModuleView>();
            
            this.container.RegisterType<HeroVirtualTabletopMainView, HeroVirtualTabletopMainView>();
            this.container.RegisterType<HeroVirtualTabletopMainViewModel, HeroVirtualTabletopMainViewModel>();
            
            this.container.RegisterType<CharacterExplorerView, CharacterExplorerView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<CharacterExplorerViewModel, CharacterExplorerViewModel>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<RosterExplorerView, RosterExplorerView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<RosterExplorerViewModel, RosterExplorerViewModel>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<CharacterEditorView, CharacterEditorView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<CharacterEditorViewModel, CharacterEditorViewModel>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AbilityEditorView, AbilityEditorView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AbilityEditorViewModel, AbilityEditorViewModel>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<CharacterCrowdMainView, CharacterCrowdMainView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<CharacterCrowdMainViewModel, CharacterCrowdMainViewModel>(new ContainerControlledLifetimeManager());

            //Registering with ContainerControlledLifeTimeMangager should act like declaring classes as Singleton's
            //Return a new one the first time only, then always the already instantiated one
            this.container.RegisterType<IdentityEditorView, IdentityEditorView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IdentityEditorViewModel, IdentityEditorViewModel>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<CrowdFromModelsView, CrowdFromModelsView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<CrowdFromModelsViewModel, CrowdFromModelsViewModel>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<ActiveCharacterWidgetView, ActiveCharacterWidgetView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ActiveCharacterWidgetViewModel, ActiveCharacterWidgetViewModel>(new ContainerControlledLifetimeManager());
            //this.container.RegisterType<ActiveAttackView, ActiveAttackView>(new ContainerControlledLifetimeManager());
            //this.container.RegisterType<ActiveAttackViewModel, ActiveAttackViewModel>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AttackTargetSelectionView, AttackTargetSelectionView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AttackTargetSelectionViewModel, AttackTargetSelectionViewModel>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AutoFireAttackConfigurationView, AutoFireAttackConfigurationView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AutoFireAttackConfigurationViewModel, AutoFireAttackConfigurationViewModel>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AttackConfigurationView, AttackConfigurationView>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<AttackConfigurationViewModel, AttackConfigurationViewModel>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<ActiveAttackView, ActiveAttackView>(new PerResolveLifetimeManager());
            this.container.RegisterType<ActiveAttackViewModel, ActiveAttackViewModel>(new PerResolveLifetimeManager());

            this.container.RegisterType<ICrowdRepository, CrowdRepository>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IResourceRepository, ResourceRepository>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<ITargetObserver, TargetObserver>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IDesktopKeyEventHandler, DesktopKeyEventHandler>(new ContainerControlledLifetimeManager());
            this.container.RegisterType<IHCSIntegrator, HCSIntegrator>(new ContainerControlledLifetimeManager());

            this.container.RegisterType<OptionGroupViewModel<Identity>, OptionGroupViewModel<Identity>>(new PerResolveLifetimeManager(), new InjectionProperty("IsReadOnlyMode", false), new InjectionProperty("LoadingOptionName", string.Empty), new InjectionProperty("ShowOptions", false));
            this.container.RegisterType<OptionGroupViewModel<AnimatedAbility>, OptionGroupViewModel<AnimatedAbility>>(new PerResolveLifetimeManager(), new InjectionProperty("IsReadOnlyMode", false), new InjectionProperty("LoadingOptionName", string.Empty), new InjectionProperty("ShowOptions", false));
            this.container.RegisterType<OptionGroupViewModel<CharacterMovement>, OptionGroupViewModel<CharacterMovement>>(new PerResolveLifetimeManager(), new InjectionProperty("IsReadOnlyMode", false), new InjectionProperty("LoadingOptionName", string.Empty), new InjectionProperty("ShowOptions", false));
            this.container.RegisterType<OptionGroupViewModel<CharacterOption>, OptionGroupViewModel<CharacterOption>>(new PerResolveLifetimeManager(), new InjectionProperty("IsReadOnlyMode", false), new InjectionProperty("LoadingOptionName", string.Empty), new InjectionProperty("ShowOptions", false));
        }

        #endregion BaseModule Members
    }
}
