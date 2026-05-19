using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Roster;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Crowds
{
    public class CharacterCrowdMainViewModel : BaseViewModel
    {
        #region Private Members
        private EventAggregator eventAggregator;

        #endregion

        #region Events
        public event EventHandler ViewLoaded;
        public void OnViewLoaded(object sender, EventArgs e)
        {
            if (ViewLoaded != null)
                ViewLoaded(sender, e);
        }
        #endregion

        #region Public Properties

        private bool isCharacterExplorerExpanded;
        public bool IsCharacterExplorerExpanded
        {
            get
            {
                return isCharacterExplorerExpanded;
            }
            set
            {
                isCharacterExplorerExpanded = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.CHARACTER_EXPLORER;
                else
                    ActivateOneOfTheMainPanels(Constants.CHARACTER_EXPLORER);
                OnPropertyChanged("IsCharacterExplorerExpanded");
            }
        }

        private bool isRosterExplorerExpanded;
        public bool IsRosterExplorerExpanded
        {
            get
            {
                return isRosterExplorerExpanded;
            }
            set
            {
                isRosterExplorerExpanded = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.ROSTER_EXPLORER;
                else
                    ActivateOneOfTheMainPanels(Constants.ROSTER_EXPLORER);
                OnPropertyChanged("IsRosterExplorerExpanded");
            }
        }

        private bool isCharacterEditorExpanded;
        public bool IsCharacterEditorExpanded
        {
            get
            {
                return isCharacterEditorExpanded;
            }
            set
            {
                isCharacterEditorExpanded = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.CHARACTER_EDITOR;
                else
                    ActivateOneOfTheMainPanels(Constants.CHARACTER_EDITOR);
                OnPropertyChanged("IsCharacterEditorExpanded");
            }
        }

        private bool isIdentityEditorExpanded;
        public bool IsIdentityEditorExpanded
        {
            get
            {
                return isIdentityEditorExpanded;
            }
            set
            {
                isIdentityEditorExpanded = value;
                //if (value)
                //    Helper.GlobalVariables_CurrentActiveWindowName = Constants.IDENTITY_EDITOR;
                //else
                //    ActivateOneOfTheMainPanels(Constants.IDENTITY_EDITOR);
                OnPropertyChanged("IsIdentityEditorExpanded");
            }
        }

        private bool isAbilityEditorExpanded;
        public bool IsAbilityEditorExpanded
        {
            get
            {
                return isAbilityEditorExpanded;
            }
            set
            {
                isAbilityEditorExpanded = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.ABILITY_EDITOR;
                else
                    ActivateOneOfTheMainPanels(Constants.ABILITY_EDITOR);
                OnPropertyChanged("IsAbilityEditorExpanded");
            }
        }

        private bool isMovementEditorExpanded;
        public bool IsMovementEditorExpanded
        {
            get
            {
                return isMovementEditorExpanded;
            }
            set
            {
                isMovementEditorExpanded = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.MOVEMENT_EDITOR;
                else
                    ActivateOneOfTheMainPanels(Constants.MOVEMENT_EDITOR);
                OnPropertyChanged("IsMovementEditorExpanded");
            }
        }

        private bool isCrowdFromModelsExpanded;
        public bool IsCrowdFromModelsExpanded
        {
            get
            {
                return isCrowdFromModelsExpanded;
            }
            set
            {
                isCrowdFromModelsExpanded = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.CROWD_FROM_MODELS_VIEW;
                else
                    ActivateOneOfTheMainPanels(Constants.CROWD_FROM_MODELS_VIEW);
                OnPropertyChanged("IsCrowdFromModelsExpanded");
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> CollapsePanelCommand { get; private set; }
        public DelegateCommand<string> ActivatePanelCommand { get; private set; }
        public DelegateCommand<string> DeactivatePanelCommand { get; private set; }

        #endregion

        #region Constructor
        public CharacterCrowdMainViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator) 
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe((IEnumerable<CrowdMemberModel> models) =>{ this.IsRosterExplorerExpanded = true; });
            this.eventAggregator.GetEvent<EditCharacterEvent>().Subscribe((Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>> tuple) => { this.IsCharacterEditorExpanded = true; });
            this.eventAggregator.GetEvent<EditIdentityEvent>().Subscribe((Tuple<Identity, Character> tuple) => { this.IsIdentityEditorExpanded = true; });
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe((Tuple<AnimatedAbility, Character> tuple) => { this.IsAbilityEditorExpanded = true; });
            this.eventAggregator.GetEvent<EditMovementEvent>().Subscribe((CharacterMovement cm) => { this.IsMovementEditorExpanded = true; });
            this.eventAggregator.GetEvent<CreateCrowdFromModelsEvent>().Subscribe((CrowdModel crowd) => { this.IsCrowdFromModelsExpanded = true; });
            this.eventAggregator.GetEvent<PanelClosedEvent>().Subscribe(this.ActivateOneOfTheMainPanels);
            //this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe((Tuple<Character, Attack> tuple) => { this.IsRosterExplorerExpanded = true; });
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            this.CollapsePanelCommand = new DelegateCommand<object>(this.CollapsePanel);
            this.ActivatePanelCommand = new DelegateCommand<string>(this.ActivatePanel);
            this.DeactivatePanelCommand = new DelegateCommand<string>(this.DeactivatePanel);
        }

        #endregion

        #region Methods
        public void LoadCharacterExplorer()
        {
            CharacterExplorerView view = this.Container.Resolve<CharacterExplorerView>();
            OnViewLoaded(view, null);
        }
        public void LoadRosterExplorer()
        {
            RosterExplorerView view = this.Container.Resolve<RosterExplorerView>();
            OnViewLoaded(view, null);
        }
        public void LoadCharacterEditor()
        {
            CharacterEditorView view = this.Container.Resolve<CharacterEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadIdentityEditor()
        {
            IdentityEditorView view = this.Container.Resolve<IdentityEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadAbilityEditor()
        {
            AbilityEditorView view = this.Container.Resolve<AbilityEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadMovementEditor()
        {
            MovementEditorView view = this.Container.Resolve<MovementEditorView>();
            OnViewLoaded(view, null);
        }
        public void LoadCrowdFromModelsView()
        {
            CrowdFromModelsView view = this.Container.Resolve<CrowdFromModelsView>();
            OnViewLoaded(view, null);
        }

        private void CollapsePanel(object state)
        {
            switch(state.ToString())
            {
                case "CharacterExplorer":
                    this.IsCharacterExplorerExpanded = false;
                    break;
                case "RosterExplorer":
                    this.IsRosterExplorerExpanded = false;
                    break;
                case "CharacterEditor":
                    this.IsCharacterEditorExpanded = false;
                    break;
                case "IdentityEditor":
                    this.IsIdentityEditorExpanded = false;
                    break;
                case "AbilityEditor":
                    this.IsAbilityEditorExpanded = false;
                    break;
                case "MovementEditor":
                    this.IsMovementEditorExpanded = false;
                    break;
                case "CrowdFromModelsView":
                    this.IsCrowdFromModelsExpanded = false;
                    break;
            }
        }
        #region Activate/Deactivate Panels for Handling keyboard events

        private void ActivatePanel(string panelName)
        {
            Helper.GlobalVariables_CurrentActiveWindowName = panelName;
        }

        private void DeactivatePanel(string panelName)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == panelName)
                Helper.GlobalVariables_CurrentActiveWindowName = "";
        }

        private void ActivateOneOfTheMainPanels(string closingPanelName)
        {
            var popupService = this.Container.Resolve<IPopupService>();
            if (popupService.IsOpen("ActiveAttackView"))
            {
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.ACTIVE_ATTACK_WIDGET;
            }
            else if (popupService.IsOpen("ActiveCharacterWidgetView"))
            {
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.ACTIVE_CHARACTER_WIDGET;
            }
            else if (IsRosterExplorerExpanded)
            {
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.ROSTER_EXPLORER;
            }
            else if (IsCharacterEditorExpanded)
            {
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.CHARACTER_EDITOR;
            }
            else if (IsCharacterExplorerExpanded)
            {
                Helper.GlobalVariables_CurrentActiveWindowName = Constants.CHARACTER_EXPLORER;
            }
            else if (IsAbilityEditorExpanded)
            {
                var abilityEditorVM = this.Container.Resolve<AbilityEditorViewModel>();
                if (abilityEditorVM.IsShowingAbilityEditor)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.ABILITY_EDITOR;
            }
            else if (IsMovementEditorExpanded)
            {
                var movementEditorVM = this.Container.Resolve<MovementEditorViewModel>();
                if (movementEditorVM.IsShowingMovementEditor)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.MOVEMENT_EDITOR;
            }
            //else if (IsIdentityEditorExpanded)
            //{
            //    var identityEditorVM = this.Container.Resolve<IdentityEditorViewModel>();
            //    if (identityEditorVM.Visibility == System.Windows.Visibility.Visible)
            //        Helper.GlobalVariables_CurrentActiveWindowName = Constants.IDENTITY_EDITOR;
            //}
            else if (IsCrowdFromModelsExpanded)
            {
                var crowdFromModelsVM = this.Container.Resolve<CrowdFromModelsViewModel>();
                if (crowdFromModelsVM.Visibility == System.Windows.Visibility.Visible)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.CROWD_FROM_MODELS_VIEW;
            }
            else
            {
                Helper.GlobalVariables_CurrentActiveWindowName = string.Empty;
            }
        }

        #endregion

        #endregion
    }
}
