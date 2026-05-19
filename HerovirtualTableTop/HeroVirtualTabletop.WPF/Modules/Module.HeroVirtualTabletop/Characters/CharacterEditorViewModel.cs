using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

[assembly: InternalsVisibleTo("Module.UnitTest")]
namespace Module.HeroVirtualTabletop.Characters
{
    public class CharacterEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;
        private Character editedCharacter;
        private HashedObservableCollection<ICrowdMemberModel, string> characterCollection;
        private IDesktopKeyEventHandler desktopKeyEventHandler;


        #endregion

        #region Public Properties

        public Character EditedCharacter
        {
            get
            {
                return editedCharacter;
            }
            set
            {
                editedCharacter = value;
                OnPropertyChanged("EditedCharacter");
                this.AddOptionGroupCommand.RaiseCanExecuteChanged();
                this.SaveCharacterCommand.RaiseCanExecuteChanged();
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

        private IOptionGroup selectedOptionGroup;
        public IOptionGroup SelectedOptionGroup
        {
            get
            {
                return selectedOptionGroup;
            }
            set
            {
                selectedOptionGroup = value;
                OnPropertyChanged("SelectedOptionGroup");
                this.RemoveOptionGroupCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Commands

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand<object> SavePositionCommand { get; private set; }
        public DelegateCommand<object> PlaceCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetedCommand { get; private set; }
        public DelegateCommand<object> TargetAndFollowCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCameraCommand { get; private set; }
        public DelegateCommand<object> ToggleManeuverWithCameraCommand { get; private set; }
        public DelegateCommand<object> AddOptionGroupCommand { get; private set; }
        public DelegateCommand<object> RemoveOptionGroupCommand { get; private set; }
        public DelegateCommand<object> SaveCharacterCommand { get; private set; }

        #endregion

        #region Constructor

        public CharacterEditorViewModel(IBusyService busyService, IUnityContainer container, IDesktopKeyEventHandler keyEventHandler, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.desktopKeyEventHandler = keyEventHandler;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditCharacterEvent>().Subscribe(this.LoadCharacter);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(this.UnLoadCharacter);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(this.AttackEnded);
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(this.Spawn, this.CanSpawn);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(this.ClearFromDesktop, this.CanClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(this.ToggleTargeted, this.CanToggleTargeted);
            this.SavePositionCommand = new DelegateCommand<object>(this.SavePostion, this.CanSavePostion);
            this.PlaceCommand = new DelegateCommand<object>(this.Place, this.CanPlace);
            this.TargetAndFollowCommand = new DelegateCommand<object>(this.TargetAndFollow, this.CanTargetAndFollow);
            this.MoveTargetToCameraCommand = new DelegateCommand<object>(this.MoveTargetToCamera, this.CanMoveTargetToCamera);
            this.ToggleManeuverWithCameraCommand = new DelegateCommand<object>(this.ToggleManeuverWithCamera, this.CanToggleManeuverWithCamera);
            this.AddOptionGroupCommand = new DelegateCommand<object>(delegate (object state) { this.AddOptionGroup(); }, this.CanAddOptionGroup);
            this.RemoveOptionGroupCommand = new DelegateCommand<object>(delegate (object state) { this.RemoveOptionGroup(); }, this.CanRemoveOptionGroup);
            this.SaveCharacterCommand = new DelegateCommand<object>(this.SaveCharacter, this.CanSaveCharacter);
        }
        
        private void InitializeDesktopKeyEventHandlers()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        public void LoadCharacter(object state)
        {
            if (this.OptionGroups != null)
            {
                foreach (IOptionGroupViewModel ogVM in this.OptionGroups)
                {
                    ogVM.RemoveDesktopKeyEventHandlers();
                }
            }
            Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>> tuple = state as Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>;
            if (tuple != null)
            {
                Character character = tuple.Item1 as Character;
                HashedObservableCollection<ICrowdMemberModel, string> collection;
                if (tuple.Item2 != null)
                    collection = new HashedObservableCollection<ICrowdMemberModel, string>(tuple.Item2, x => x.Name);
                else
                {
                    collection = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name);
                    collection.Add(character as CrowdMemberModel);
                }
                if(character != null && collection != null)
                {
                    character.AddDefaultAbilities();
                    this.OptionGroups = new ObservableCollection<IOptionGroupViewModel>();
                    foreach (IOptionGroup group in character.OptionGroups)
                    {
                        bool showOptionsInGroup = false;
                        if(character.OptionGroupExpansionStates.ContainsKey(group.Name))
                            showOptionsInGroup = character.OptionGroupExpansionStates[group.Name];
                        switch (group.Type)
                        {
                            case OptionType.Ability:
                                OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                                new ParameterOverride("optionGroup", group),
                                new ParameterOverride("owner", character),
                                new PropertyOverride("ShowOptions", showOptionsInGroup)
                                ));
                                break;
                            case OptionType.Identity:
                                OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<Identity>>(
                                new ParameterOverride("optionGroup", group),
                                new ParameterOverride("owner", character),
                                new PropertyOverride("ShowOptions", showOptionsInGroup)
                                ));
                                break;
                            case OptionType.CharacterMovement:
                                OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterMovement>>(
                                new ParameterOverride("optionGroup", group),
                                new ParameterOverride("owner", character),
                                new PropertyOverride("ShowOptions", showOptionsInGroup)
                                ));
                                break;
                            case OptionType.Mixed:
                                OptionGroups.Add(this.Container.Resolve<OptionGroupViewModel<CharacterOption>>(
                                new ParameterOverride("optionGroup", group),
                                new ParameterOverride("owner", character),
                                new PropertyOverride("ShowOptions", showOptionsInGroup)
                                ));
                                break;
                        }
                    }

                    this.EditedCharacter = character;
                    this.characterCollection = collection;
                }
            }
        }

        private void UnLoadCharacter(object state)
        {
            ICrowdMemberModel model = state as ICrowdMemberModel;
            //if (model != null && this.EditedCharacter.Name == model.Name)
            //    this.EditedCharacter = null;
        }

        #endregion

        #region Methods

        private void AttackInitiated(Tuple<Character, Attack> tuple)
        {
            this.Commands_RaiseCanExecuteChanged();
        }

        private void AttackEnded(object state)
        {
            this.Commands_RaiseCanExecuteChanged();
        }

        private void Commands_RaiseCanExecuteChanged()
        {
            this.SpawnCommand.RaiseCanExecuteChanged();
            this.ClearFromDesktopCommand.RaiseCanExecuteChanged();
            this.ToggleTargetedCommand.RaiseCanExecuteChanged();
            this.SavePositionCommand.RaiseCanExecuteChanged();
            this.PlaceCommand.RaiseCanExecuteChanged();
            this.TargetAndFollowCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            this.ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
            this.AddOptionGroupCommand.RaiseCanExecuteChanged();
            this.RemoveOptionGroupCommand.RaiseCanExecuteChanged();
        }

        #region Save Character
        private bool CanSaveCharacter(object state)
        {
            return EditedCharacter != null;
        }

        public void SaveCharacter(object state)
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
        #endregion

        #region Spawn
        private bool CanSpawn(object state)
        {
            return EditedCharacter != null;
        }

        private void Spawn(object state)
        {
            //Check if is in roster, if not add to it
            if ((EditedCharacter as CrowdMemberModel).RosterCrowd == null)
            {
                eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<CrowdMemberModel, CrowdModel>(EditedCharacter as CrowdMemberModel, null));
            }
            EditedCharacter.Spawn();
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region Clear from Desktop
        private bool CanClearFromDesktop(object state)
        {
            return EditedCharacter != null && EditedCharacter.HasBeenSpawned && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void ClearFromDesktop(object state)
        {
            EditedCharacter.ClearFromDesktop();
            Commands_RaiseCanExecuteChanged();
        }

        #endregion

        #region Save Positon

        private bool CanSavePostion(object state)
        {
            bool canSavePosition = false;
            if (this.EditedCharacter != null && this.EditedCharacter.HasBeenSpawned)
            {
                canSavePosition = true;
            }
            return canSavePosition;
        }

        private void SavePostion(object state)
        {
            (EditedCharacter as CrowdMemberModel).SavePosition();
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.PlaceCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Place
        private bool CanPlace(object state)
        {
            bool canPlace = false;
            if (this.EditedCharacter != null)
            {
                var crowdMemberModel = EditedCharacter as CrowdMemberModel;
                if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME && crowdMemberModel.SavedPosition != null)
                {
                    canPlace = true;
                }
                else if (crowdMemberModel != null && crowdMemberModel.RosterCrowd.Name != Constants.ALL_CHARACTER_CROWD_NAME)
                {
                    CrowdModel rosterCrowdModel = crowdMemberModel.RosterCrowd as CrowdModel;
                    if (rosterCrowdModel.SavedPositions.ContainsKey(crowdMemberModel.Name))
                    {
                        canPlace = true;
                    }
                }
            }
            return canPlace;
        }
        private void Place(object state)
        {
            (EditedCharacter as CrowdMemberModel).Place();
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region ToggleTargeted

        private bool CanToggleTargeted(object state)
        {
            bool canToggleTargeted = false;
            if (this.EditedCharacter != null && EditedCharacter.HasBeenSpawned)
            {
                canToggleTargeted = true;
            }
            return canToggleTargeted;
        }

        private void ToggleTargeted(object obj)
        {
            EditedCharacter.ToggleTargeted();
        }

        #endregion

        #region Target And Follow

        private bool CanTargetAndFollow(object state)
        {
            return CanToggleTargeted(state);
        }

        private void TargetAndFollow(object obj)
        {
            editedCharacter.TargetAndFollow();
        }

        #endregion

        #region Move Target to Camera

        private bool CanMoveTargetToCamera(object arg)
        {
            bool canMoveTargetToCamera = true;
            if (this.EditedCharacter == null)
            {
                canMoveTargetToCamera = false;
                return canMoveTargetToCamera;
            }
            else
            {
                return editedCharacter.HasBeenSpawned;
            }
        }

        private void MoveTargetToCamera(object obj)
        {
            EditedCharacter.MoveToCamera();
        }

        #endregion

        #region ToggleManeuverWithCamera


        private bool CanToggleManeuverWithCamera(object arg)
        {
            bool canManeuverWithCamera = false;
            if (this.EditedCharacter != null && (EditedCharacter.HasBeenSpawned || EditedCharacter.ManeuveringWithCamera))
            {
                canManeuverWithCamera = true;
            }
            return canManeuverWithCamera;
        }

        private void ToggleManeuverWithCamera(object obj)
        {
            EditedCharacter.ToggleManueveringWithCamera();
        }

        #endregion

        #region Add/Remove OptionGroups

        private bool CanAddOptionGroup(object state)
        {
            return this.editedCharacter != null && this.editedCharacter.OptionGroups != null && !Helper.GlobalVariables_IsPlayingAttack;
        }
        
        private bool CanRemoveOptionGroup(object arg)
        {
            return SelectedOptionGroup != null && SelectedOptionGroup.Name != Constants.ABILITY_OPTION_GROUP_NAME && SelectedOptionGroup.Name != Constants.IDENTITY_OPTION_GROUP_NAME && SelectedOptionGroup.Name != Constants.MOVEMENT_OPTION_GROUP_NAME && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void RemoveOptionGroup()
        {
            if (SelectedOptionGroup != null)
            {
                IOptionGroup toBeRemoved = SelectedOptionGroup;
                this.EditedCharacter.RemoveOptionGroup(toBeRemoved);
                this.OptionGroups.Remove(this.OptionGroups.First((optG) => { return optG.OptionGroup == toBeRemoved; }));
                this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null); 
            }
        }

        private void AddOptionGroup()
        {
            string baseName = "Custom Option Group";
            string validName = baseName;
            int i = 1;
            while (this.editedCharacter.OptionGroups.ContainsKey(validName))
            {
                validName = string.Format("{0} ({1})", baseName, i++);
            }
            IOptionGroup optGroup = new OptionGroup<CharacterOption>(validName);
            this.EditedCharacter.AddOptionGroup(optGroup);
            var optGroupViewModel = this.Container.Resolve<OptionGroupViewModel<CharacterOption>>(
                                new ParameterOverride("optionGroup", optGroup),
                                new ParameterOverride("owner", editedCharacter)
                                );
            OptionGroups.Add(optGroupViewModel);
            optGroupViewModel.NewOptionGroupAdded = true;
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        #endregion

        #region ReOrder Option Groups

        public void ReOrderOptionGroups(int sourceIndex, int targetIndex)
        {
            IOptionGroupViewModel sourceViewModel = this.OptionGroups[sourceIndex];
            this.OptionGroups.RemoveAt(sourceIndex);
            this.EditedCharacter.RemoveOptionGroupAt(sourceIndex);
            this.OptionGroups.Insert(targetIndex, sourceViewModel);
            this.EditedCharacter.InsertOptionGroup(targetIndex, sourceViewModel.OptionGroup);

            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        #endregion

        #region Keyboard Hooks

        internal EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (this.EditedCharacter != null && Helper.GlobalVariables_CurrentActiveWindowName == Constants.CHARACTER_EDITOR)
            {
                if ((inputKey == Key.OemPlus || inputKey == Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.AddOptionGroup;
                }
                else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return RemoveOptionGroup;
                }
            }
            return null;
        }

        #endregion

        #endregion
    }
}
