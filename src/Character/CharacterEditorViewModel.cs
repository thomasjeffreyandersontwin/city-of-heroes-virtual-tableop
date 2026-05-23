using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using Module.HeroVirtualTabletop.OptionGroups;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Desktop;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using Library = Module.HeroVirtualTabletop.Library;
using Roster = Module.HeroVirtualTabletop.Roster;
[assembly: InternalsVisibleTo("Module.UnitTest")]
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
namespace HeroVTT.Characters
{
    public class CharacterEditorViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;
        private readonly ICharacterGameState _gameState;
        private readonly IDesktopKeyEventHandler _desktopKeyEventHandler;
        private Character _editedCharacter;
        private HashedObservableCollection<ICrowdMemberModel, string> _characterCollection;

        public Character EditedCharacter
        {
            get { return _editedCharacter; }
            set
            {
                _editedCharacter = value;
                OnPropertyChanged("EditedCharacter");
                AddOptionGroupCommand.RaiseCanExecuteChanged();
                SaveCharacterCommand.RaiseCanExecuteChanged();
            }
        }

        private ObservableCollection<IOptionGroupViewModel> _optionGroups;
        public ObservableCollection<IOptionGroupViewModel> OptionGroups
        {
            get { return _optionGroups; }
            private set { _optionGroups = value; OnPropertyChanged("OptionGroups"); }
        }

        private IOptionGroup _selectedOptionGroup;
        public IOptionGroup SelectedOptionGroup
        {
            get { return _selectedOptionGroup; }
            set
            {
                _selectedOptionGroup = value;
                OnPropertyChanged("SelectedOptionGroup");
                RemoveOptionGroupCommand.RaiseCanExecuteChanged();
            }
        }

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

        public CharacterEditorViewModel(
            IBusyService busyService,
            IUnityContainer container,
            ICharacterGameState gameState,
            IDesktopKeyEventHandler keyEventHandler,
            EventAggregator eventAggregator)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _gameState = gameState;
            _desktopKeyEventHandler = keyEventHandler;
            InitializeCommands();
            _eventAggregator.GetEvent<EditCharacterEvent>().Subscribe(LoadCharacter);
            _eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(UnLoadCharacter);
            _eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(_ => RaiseAllCanExecuteChanged());
            _eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(_ => RaiseAllCanExecuteChanged());
        }

        private void InitializeCommands()
        {
            SpawnCommand = new DelegateCommand<object>(_ => Spawn(), _ => EditedCharacter != null);
            ClearFromDesktopCommand = new DelegateCommand<object>(_ => ClearFromDesktop(), _ => CanClearFromDesktop());
            ToggleTargetedCommand = new DelegateCommand<object>(_ => EditedCharacter.ToggleTargeted(), _ => CanToggleTargeted());
            SavePositionCommand = new DelegateCommand<object>(_ => SavePosition(), _ => CanSavePosition());
            PlaceCommand = new DelegateCommand<object>(_ => Place(), _ => CanPlace());
            TargetAndFollowCommand = new DelegateCommand<object>(_ => EditedCharacter.TargetAndFollow(), _ => CanToggleTargeted());
            MoveTargetToCameraCommand = new DelegateCommand<object>(_ => EditedCharacter.MoveToCamera(), _ => CanMoveTargetToCamera());
            ToggleManeuverWithCameraCommand = new DelegateCommand<object>(_ => EditedCharacter.ToggleManueveringWithCamera(), _ => CanToggleManeuverWithCamera());
            AddOptionGroupCommand = new DelegateCommand<object>(_ => AddOptionGroup(), _ => CanAddOptionGroup());
            RemoveOptionGroupCommand = new DelegateCommand<object>(_ => RemoveOptionGroup(), _ => CanRemoveOptionGroup());
            SaveCharacterCommand = new DelegateCommand<object>(_ => SaveCharacter(), _ => EditedCharacter != null);
        }

        private void RaiseAllCanExecuteChanged()
        {
            SpawnCommand.RaiseCanExecuteChanged();
            ClearFromDesktopCommand.RaiseCanExecuteChanged();
            ToggleTargetedCommand.RaiseCanExecuteChanged();
            SavePositionCommand.RaiseCanExecuteChanged();
            PlaceCommand.RaiseCanExecuteChanged();
            TargetAndFollowCommand.RaiseCanExecuteChanged();
            MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
            AddOptionGroupCommand.RaiseCanExecuteChanged();
            RemoveOptionGroupCommand.RaiseCanExecuteChanged();
        }

        public void LoadCharacter(object state)
        {
            if (OptionGroups != null)
                foreach (IOptionGroupViewModel ogVM in OptionGroups)
                    ogVM.RemoveDesktopKeyEventHandlers();

            var tuple = state as Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>;
            if (tuple == null) return;

            Character character = tuple.Item1 as Character;
            HashedObservableCollection<ICrowdMemberModel, string> collection;
            if (tuple.Item2 != null)
                collection = new HashedObservableCollection<ICrowdMemberModel, string>(tuple.Item2, x => x.Name);
            else
            {
                collection = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name);
                collection.Add(character as CrowdMemberModel);
            }
            if (character == null || collection == null) return;

            character.AddDefaultAbilities();
            OptionGroups = new ObservableCollection<IOptionGroupViewModel>();
            foreach (IOptionGroup group in character.OptionGroups)
            {
                bool showOptions = character.OptionGroupExpansionStates.ContainsKey(group.Name)
                    && character.OptionGroupExpansionStates[group.Name];
                ResolveAndAddOptionGroupViewModel(group, character, showOptions);
            }
            EditedCharacter = character;
            _characterCollection = collection;
        }

        private void ResolveAndAddOptionGroupViewModel(IOptionGroup group, Character character, bool showOptions)
        {
            switch (group.Type)
            {
                case OptionType.Ability:
                    OptionGroups.Add(Container.Resolve<OptionGroupViewModel<AnimatedAbility>>(
                        new ParameterOverride("optionGroup", group),
                        new ParameterOverride("owner", character),
                        new PropertyOverride("ShowOptions", showOptions)));
                    break;
                case OptionType.Identity:
                    OptionGroups.Add(Container.Resolve<OptionGroupViewModel<Identity>>(
                        new ParameterOverride("optionGroup", group),
                        new ParameterOverride("owner", character),
                        new PropertyOverride("ShowOptions", showOptions)));
                    break;
                case OptionType.CharacterMovement:
                    OptionGroups.Add(Container.Resolve<OptionGroupViewModel<CharacterMovement>>(
                        new ParameterOverride("optionGroup", group),
                        new ParameterOverride("owner", character),
                        new PropertyOverride("ShowOptions", showOptions)));
                    break;
                case OptionType.Mixed:
                    OptionGroups.Add(Container.Resolve<OptionGroupViewModel<CharacterOption>>(
                        new ParameterOverride("optionGroup", group),
                        new ParameterOverride("owner", character),
                        new PropertyOverride("ShowOptions", showOptions)));
                    break;
            }
        }

        private void UnLoadCharacter(object state) { }

        private void SaveCharacter() => _eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);

        private void Spawn()
        {
            CharacterCommands.EnsureInRosterThenSpawn(EditedCharacter, _eventAggregator);
            RaiseAllCanExecuteChanged();
        }

        private bool CanClearFromDesktop()
        {
            return EditedCharacter != null && EditedCharacter.HasBeenSpawned && !_gameState.IsPlayingAttack;
        }

        private void ClearFromDesktop()
        {
            EditedCharacter.ClearFromDesktop();
            RaiseAllCanExecuteChanged();
        }

        private bool CanSavePosition()
        {
            return EditedCharacter != null && EditedCharacter.HasBeenSpawned;
        }

        private void SavePosition()
        {
            CharacterCommands.SavePositionAndPublish(EditedCharacter, _eventAggregator);
            PlaceCommand.RaiseCanExecuteChanged();
        }

        private bool CanPlace() => CharacterCommands.CanPlace(EditedCharacter);

        private void Place()
        {
            (EditedCharacter as CrowdMemberModel).Place();
            RaiseAllCanExecuteChanged();
        }

        private bool CanToggleTargeted()
        {
            return EditedCharacter != null && EditedCharacter.HasBeenSpawned;
        }

        private bool CanMoveTargetToCamera()
        {
            return EditedCharacter != null && EditedCharacter.HasBeenSpawned;
        }

        private bool CanToggleManeuverWithCamera()
        {
            return EditedCharacter != null && (EditedCharacter.HasBeenSpawned || EditedCharacter.ManeuveringWithCamera);
        }

        private bool CanAddOptionGroup()
        {
            return _editedCharacter != null && _editedCharacter.OptionGroups != null && !_gameState.IsPlayingAttack;
        }

        private bool CanRemoveOptionGroup()
        {
            return SelectedOptionGroup != null
                && SelectedOptionGroup.Name != Constants.ABILITY_OPTION_GROUP_NAME
                && SelectedOptionGroup.Name != Constants.IDENTITY_OPTION_GROUP_NAME
                && SelectedOptionGroup.Name != Constants.MOVEMENT_OPTION_GROUP_NAME
                && !_gameState.IsPlayingAttack;
        }

        private void RemoveOptionGroup()
        {
            CharacterCommands.RemoveOptionGroupAndSave(EditedCharacter, SelectedOptionGroup, OptionGroups, _eventAggregator);
        }

        private void AddOptionGroup()
        {
            var optGroupViewModel = CharacterCommands.CreateAndAddOptionGroup(EditedCharacter, OptionGroups, Container, _eventAggregator);
            optGroupViewModel.NewOptionGroupAdded = true;
        }

        public void ReOrderOptionGroups(int sourceIndex, int targetIndex)
        {
            CharacterCommands.ReOrder(EditedCharacter, OptionGroups, sourceIndex, targetIndex, _eventAggregator);
        }

        internal EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (EditedCharacter == null || _gameState.CurrentActiveWindowName != Constants.CHARACTER_EDITOR)
                return null;

            if ((inputKey == Key.OemPlus || inputKey == Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
                return AddOptionGroup;
            if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
                return RemoveOptionGroup;

            return null;
        }
    }

    internal static class CharacterCommands
    {
        public static void EnsureInRosterThenSpawn(Character character, EventAggregator eventAggregator)
        {
            if ((character as CrowdMemberModel).RosterCrowd == null)
                eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(
                    new Tuple<CrowdMemberModel, CrowdModel>(character as CrowdMemberModel, null));
            character.Spawn();
        }

        public static void SavePositionAndPublish(Character character, EventAggregator eventAggregator)
        {
            (character as CrowdMemberModel).SavePosition();
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        public static bool CanPlace(Character character)
        {
            if (character == null) return false;
            var crowdMember = character as CrowdMemberModel;
            if (crowdMember?.RosterCrowd == null) return false;
            if (crowdMember.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME)
                return crowdMember.SavedPosition != null;
            var rosterCrowdModel = crowdMember.RosterCrowd as CrowdModel;
            return rosterCrowdModel != null && rosterCrowdModel.SavedPositions.ContainsKey(crowdMember.Name);
        }

        public static void RemoveOptionGroupAndSave(
            Character character, IOptionGroup selected,
            ObservableCollection<IOptionGroupViewModel> optionGroups, EventAggregator eventAggregator)
        {
            if (selected == null) return;
            character.RemoveOptionGroup(selected);
            optionGroups.Remove(optionGroups.First(optG => optG.OptionGroup == selected));
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        public static OptionGroupViewModel<CharacterOption> CreateAndAddOptionGroup(
            Character character,
            ObservableCollection<IOptionGroupViewModel> optionGroups,
            IUnityContainer container, EventAggregator eventAggregator)
        {
            string baseName = "Custom Option Group";
            string validName = baseName;
            int i = 1;
            while (character.OptionGroups.ContainsKey(validName))
                validName = string.Format("{0} ({1})", baseName, i++);

            IOptionGroup optGroup = new OptionGroup<CharacterOption>(validName);
            character.AddOptionGroup(optGroup);
            var vm = container.Resolve<OptionGroupViewModel<CharacterOption>>(
                new ParameterOverride("optionGroup", optGroup),
                new ParameterOverride("owner", character));
            optionGroups.Add(vm);
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            return vm;
        }

        public static void ReOrder(
            Character character, ObservableCollection<IOptionGroupViewModel> optionGroups,
            int sourceIndex, int targetIndex, EventAggregator eventAggregator)
        {
            var sourceViewModel = optionGroups[sourceIndex];
            optionGroups.RemoveAt(sourceIndex);
            character.RemoveOptionGroupAt(sourceIndex);
            optionGroups.Insert(targetIndex, sourceViewModel);
            character.InsertOptionGroup(targetIndex, sourceViewModel.OptionGroup);
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
    }
}
