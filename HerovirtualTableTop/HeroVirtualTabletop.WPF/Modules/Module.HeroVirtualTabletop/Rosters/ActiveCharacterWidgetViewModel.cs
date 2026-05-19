using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Prism.Events;
using Microsoft.Practices.Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Module.HeroVirtualTabletop.Desktop;
using Module.Shared;

namespace Module.HeroVirtualTabletop.Roster
{
    public class ActiveCharacterWidgetViewModel : BaseViewModel
    {

        #region Private Fields and Commands
        private EventAggregator eventAggregator;
        private AnimatedAbility activeAbility;
        public DelegateCommand<string> ActivatePanelCommand { get; private set; }
        public DelegateCommand<string> DeactivatePanelCommand { get; private set; }
        #endregion

        #region Public Properties
        private Character activeCharacter;
        public Character ActiveCharacter
        {
            get
            {
                return activeCharacter;
            }
            set
            {
                activeCharacter = value;
                OnPropertyChanged("ActiveCharacter");
                OnPropertyChanged("ActiveCharacterName");
            }
        }

        public string ActiveCharacterName
        {
            get
            {
                if (ActiveCharacter == null)
                    return "";
                if (ActiveCharacter.IsGangLeader)
                    return ActiveCharacter.Name + " <Gang Leader>";
                return ActiveCharacter.Name;
            }
        }

        private ObservableCollection<IOptionGroupViewModel> optionGroups;
        public ObservableCollection<IOptionGroupViewModel> OptionGroups
        {
            get
            {
                return optionGroups;
            }
            private set
            {
                optionGroups = value;
                OnPropertyChanged("OptionGroups");
            }
        }
        #endregion

        #region Constructor
        public ActiveCharacterWidgetViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<ActivateCharacterEvent>().Subscribe(this.LoadCharacter);
            this.eventAggregator.GetEvent<ActivateGangEvent>().Subscribe(this.LoadGang);

            this.ActivatePanelCommand = new DelegateCommand<string>(this.ActivatePanel);
            this.DeactivatePanelCommand = new DelegateCommand<string>(this.DeactivatePanel);
        }
        #endregion

        private void LoadCharacter(Tuple<Character, string, string> tuple)
        {
            this.UnloadCharacter();

            Character character = tuple.Item1;
            string optionGroupName = tuple.Item2;
            string optionName = tuple.Item3;
            this.ActiveCharacter = character;
            if (character != null)
            {
                character.AddDefaultAbilities();
                this.OptionGroups = new ObservableCollection<IOptionGroupViewModel>();
                foreach (IOptionGroup group in character.OptionGroups)
                {
                    bool loadedOptionExists = group.Name == optionGroupName;
                    bool showOptionsInGroup = false;
                    if (character.OptionGroupExpansionStates.ContainsKey(group.Name))
                        showOptionsInGroup = character.OptionGroupExpansionStates[group.Name];
                    switch (group.Type)
                    {
                        case OptionType.Ability:

                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.Identity:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<Identity>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.CharacterMovement:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterMovement>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                        case OptionType.Mixed:
                            OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterOption>>(
                            new ParameterOverride("optionGroup", group),
                            new ParameterOverride("owner", character),
                            new PropertyOverride("IsReadOnlyMode", true),
                            new PropertyOverride("ShowOptions", showOptionsInGroup),
                            new PropertyOverride("LoadingOptionName", loadedOptionExists ? optionName : "")
                            ));
                            break;
                    }
                }

                //character.Activate();
            }

        }

        private void LoadGang(List<Character> gangMembers)
        {
            Character gangLeader = gangMembers.FirstOrDefault(m => m.IsGangLeader);
            LoadCharacter(new Tuple<Character, string, string>(gangLeader, null, null));
        }
        private void UnloadCharacter()
        {
            if (ActiveCharacter != null)
            {
                ActiveCharacter.Deactivate();
            }

            ActiveCharacter = null;
            if(this.OptionGroups != null)
            {
                foreach(IOptionGroupViewModel ogVM in this.OptionGroups)
                {
                    ogVM.RemoveDesktopKeyEventHandlers();
                }
            }
        }
        private void ActivatePanel(string panelName)
        {
            Helper.GlobalVariables_CurrentActiveWindowName = panelName;
        }

        private void DeactivatePanel(string panelName)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == panelName)
                Helper.GlobalVariables_CurrentActiveWindowName = "";
        }

    }
}
