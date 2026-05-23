using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;

using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
namespace HeroVTT.OptionGroups
{
    public interface IOptionGroupViewModel
    {
        IOptionGroup OptionGroup { get; }
        event EventHandler EditModeEnter;
        event EventHandler EditModeLeave;
        bool IsReadOnlyMode { get; set; }
        void RemoveOption(int index);
        void InsertOption(int index, ICharacterOption option);
        void SaveOptionGroup();
        void RemoveDesktopKeyEventHandlers();
    }

    public class OptionGroupViewModel<T> : BaseViewModel, IOptionGroupViewModel where T : ICharacterOption
    {
        private readonly EventAggregator _eventAggregator;
        private readonly IOptionGroupGameState _gameState;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDesktopKeyEventHandler _desktopKeyEventHandler;
        private OptionGroup<T> _optionGroup;
        private Timer _clickTimer = new Timer();

        public event EventHandler EditModeEnter;
        public void OnEditModeEnter(object sender, EventArgs e)
        {
            if (EditModeEnter != null)
                EditModeEnter(sender, e);
        }

        public event EventHandler EditModeLeave;
        public void OnEditModeLeave(object sender, EventArgs e)
        {
            if (EditModeLeave != null)
                EditModeLeave(sender, e);
        }

        public OptionGroup<T> OptionGroup
        {
            get { return _optionGroup; }
            private set { _optionGroup = value; OnPropertyChanged("OptionGroup"); }
        }

        IOptionGroup IOptionGroupViewModel.OptionGroup
        {
            get { return OptionGroup as IOptionGroup; }
        }

        private T _selectedOption;
        public T SelectedOption
        {
            get { return GetSelectedOption(); }
            set
            {
                if (value == null || (value != null && value.IsEnabled))
                    SetSelectedOption(value);
                OnPropertyChanged("SelectedOption");
                OnPropertyChanged("IsCombatMovementSelected");
                OnPropertyChanged("IsNonCombatMovementSelected");
                PlayOptionCommand.RaiseCanExecuteChanged();
                StopOptionCommand.RaiseCanExecuteChanged();
            }
        }

        public T DefaultOption
        {
            get { return GetDefaultOption(); }
            set { SetDefaultOption(value); OnPropertyChanged("DefaultOption"); SaveOptionGroup(); }
        }

        public T ActiveOption
        {
            get { return GetActiveOption(); }
            set { SetActiveOption(value); OnPropertyChanged("ActiveOption"); }
        }

        private bool _isReadOnlyMode;
        public bool IsReadOnlyMode
        {
            get { return _isReadOnlyMode; }
            set { _isReadOnlyMode = value; OnPropertyChanged("IsReadOnlyMode"); }
        }

        private bool _showOptions;
        public bool ShowOptions
        {
            get { return _showOptions; }
            set { _showOptions = value; OnPropertyChanged("ShowOptions"); }
        }

        private string _addOptionTooltip;
        public string AddOptionTooltip
        {
            get { return _addOptionTooltip; }
            set { _addOptionTooltip = value; OnPropertyChanged("AddOptionTooltip"); }
        }

        private string _removeOptionTooltip;
        public string RemoveOptionTooltip
        {
            get { return _removeOptionTooltip; }
            set { _removeOptionTooltip = value; OnPropertyChanged("RemoveOptionTooltip"); }
        }

        private string _loadingOptionName;
        public string LoadingOptionName
        {
            get { return _loadingOptionName; }
            set
            {
                _loadingOptionName = value;
                if (string.IsNullOrEmpty(value)) return;
                var optionToLoad = OptionGroup.FirstOrDefault(o => o.Name == value);
                if (optionToLoad == null) return;
                ShowOptions = true;
                SelectedOption = optionToLoad;
                TogglePlayOption(optionToLoad);
            }
        }

        private Character _owner;
        public Character Owner
        {
            get { return _owner; }
            private set { _owner = value; }
        }

        public bool IsStandardOptionGroup
        {
            get
            {
                return OptionGroup.Name == Constants.IDENTITY_OPTION_GROUP_NAME
                    || OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME
                    || OptionGroup.Name == Constants.MOVEMENT_OPTION_GROUP_NAME;
            }
        }

        public bool IsCombatMovementSelected
        {
            get { return SelectedOption != null && SelectedOption is CharacterMovement && !(SelectedOption as CharacterMovement).IsNonCombatMovement; }
        }

        public bool IsNonCombatMovementSelected
        {
            get { return SelectedOption != null && SelectedOption is CharacterMovement && (SelectedOption as CharacterMovement).IsNonCombatMovement; }
        }

        public bool NewOptionGroupAdded { get; set; }
        public string OriginalName { get; set; }

        public DelegateCommand<object> AddOptionCommand { get; private set; }
        public DelegateCommand<object> RemoveOptionCommand { get; private set; }
        public DelegateCommand<object> SetDefaultOptionCommand { get; private set; }
        public DelegateCommand<object> EditOptionCommand { get; private set; }
        public DelegateCommand<object> PlayOptionCommand { get; private set; }
        public DelegateCommand<object> StopOptionCommand { get; private set; }
        public DelegateCommand<object> TogglePlayOptionCommand { get; private set; }
        public DelegateCommand SetActiveOptionCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitOptionGroupRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }
        public DelegateCommand<object> RenameNewOptionGroupCommand { get; private set; }
        public DelegateCommand<object> ShowHideCharacterOptionCommand { get; private set; }
        public DelegateCommand<object> ActivateOptionGroupCommand { get; private set; }
        public DelegateCommand<object> DeactivateOptionGroupCommand { get; private set; }
        public DelegateCommand<object> SetNonCombatMovementCommand { get; private set; }

        public OptionGroupViewModel(
            IBusyService busyService,
            IUnityContainer container,
            IMessageBoxService messageBoxService,
            IOptionGroupGameState gameState,
            IDesktopKeyEventHandler keyEventHandler,
            EventAggregator eventAggregator,
            OptionGroup<T> optionGroup,
            Character owner)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _messageBoxService = messageBoxService;
            _gameState = gameState;
            _desktopKeyEventHandler = keyEventHandler;
            Owner = owner;
            Owner.PropertyChanged += Owner_PropertyChanged;
            OptionGroup = optionGroup;

            _eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(_ => UpdateCommands());
            _eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(OnAttackExecutionFinished);
            _eventAggregator.GetEvent<CombatMovementChangedEvent>().Subscribe(_ =>
            {
                OnPropertyChanged("IsNonCombatMovementSelected");
                OnPropertyChanged("IsCombatMovementSelected");
            });
            if (!IsStandardOptionGroup)
                _eventAggregator.GetEvent<RemoveOptionEvent>().Subscribe(RemoveOption);

            _clickTimer.AutoReset = false;
            _clickTimer.Interval = 2000;
            _clickTimer.Elapsed += ClickTimer_Elapsed;
            InitializeCommands();
            SetTooltips();
            _desktopKeyEventHandler.AddKeyEventHandler(RetrieveEventFromKeyInput);
        }

        public void RemoveDesktopKeyEventHandlers()
        {
            _desktopKeyEventHandler.RemoveKeyEventHandler(RetrieveEventFromKeyInput);
        }

        private void InitializeCommands()
        {
            AddOptionCommand = new DelegateCommand<object>(AddOption, _ => !_gameState.IsPlayingAttack);
            RemoveOptionCommand = new DelegateCommand<object>(RemoveOption, _ => !_gameState.IsPlayingAttack);
            SetDefaultOptionCommand = new DelegateCommand<object>(_ => DefaultOption = SelectedOption, CanSetDefaultOption);
            EditOptionCommand = new DelegateCommand<object>(EditOption, _ => !_gameState.IsPlayingAttack);
            PlayOptionCommand = new DelegateCommand<object>(PlayOption, CanPlayOption);
            StopOptionCommand = new DelegateCommand<object>(StopOption, CanPlayOption);
            TogglePlayOptionCommand = new DelegateCommand<object>(TogglePlayOption, _ => !_gameState.IsPlayingAttack);
            ShowHideCharacterOptionCommand = new DelegateCommand<object>(ShowHideCharacterOption);
            EnterEditModeCommand = new DelegateCommand<object>(EnterEditMode, _ => !IsStandardOptionGroup);
            SubmitOptionGroupRenameCommand = new DelegateCommand<object>(SubmitRename);
            CancelEditModeCommand = new DelegateCommand<object>(CancelEditMode);
            RenameNewOptionGroupCommand = new DelegateCommand<object>(RenameOptionGroupOnAdd);
            SetNonCombatMovementCommand = new DelegateCommand<object>(SetNonCombatMovement);
        }

        private void SetTooltips()
        {
            switch (OptionGroup.Type)
            {
                case OptionType.Ability:
                    AddOptionTooltip = "Add Power (Ctrl+P)";
                    RemoveOptionTooltip = "Remove Power (Alt+P)";
                    break;
                case OptionType.CharacterMovement:
                    AddOptionTooltip = "Add Movement (Ctrl+M)";
                    RemoveOptionTooltip = "Remove Movement (Alt+M)";
                    break;
                case OptionType.Identity:
                    AddOptionTooltip = "Add Identity (Ctrl+I)";
                    RemoveOptionTooltip = "Remove Identity (Alt+I)";
                    break;
                default:
                    AddOptionTooltip = "Add Custom Option";
                    RemoveOptionTooltip = "Remove Custom Option (Alt+X)";
                    break;
            }
        }

        private void UpdateCommands()
        {
            AddOptionCommand.RaiseCanExecuteChanged();
            RemoveOptionCommand.RaiseCanExecuteChanged();
            EditOptionCommand.RaiseCanExecuteChanged();
            TogglePlayOptionCommand.RaiseCanExecuteChanged();
            PlayOptionCommand.RaiseCanExecuteChanged();
            StopOptionCommand.RaiseCanExecuteChanged();
        }

        private void Owner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (OptionGroup.Type == OptionType.Identity)
            {
                if (e.PropertyName == "ActiveIdentity") { OnPropertyChanged("ActiveOption"); OnPropertyChanged("SelectedOption"); }
                else if (e.PropertyName == "DefaultIdentity") OnPropertyChanged("DefaultOption");
            }
            else if (OptionGroup.Type == OptionType.Ability)
            {
                if (e.PropertyName == "ActiveAbility") { OnPropertyChanged("ActiveOption"); OnPropertyChanged("SelectedOption"); }
            }
            else if (OptionGroup.Type == OptionType.CharacterMovement)
            {
                if (e.PropertyName == "ActiveMovement") { OnPropertyChanged("ActiveOption"); OnPropertyChanged("SelectedOption"); }
                else if (e.PropertyName == "DefaultMovement") OnPropertyChanged("DefaultOption");
            }
            else if (OptionGroup.Type == OptionType.Mixed)
            {
                if (e.PropertyName == "ActiveIdentity" || e.PropertyName == "ActiveMovement" || e.PropertyName == "ActiveAbility")
                { OnPropertyChanged("ActiveOption"); OnPropertyChanged("SelectedOption"); }
                if (e.PropertyName == "DefaultIdentity" || e.PropertyName == "DefaultMovement")
                    OnPropertyChanged("DefaultOption");
            }
        }

        private void AddOption(object state)
        {
            if (typeof(T) == typeof(Identity)) AddIdentity();
            else if (typeof(T) == typeof(AnimatedAbility)) AddAbility();
            else if (typeof(T) == typeof(CharacterMovement)) AddCharacterMovement();
            SaveUpdatedOptions();
        }

        private void RemoveOption(object state)
        {
            var optionToRemove = SelectedOption;
            if (typeof(T) == typeof(Identity)) RemoveIdentity();
            else if (typeof(T) == typeof(CharacterMovement)) RemoveCharacterMovement();
            else _optionGroup.Remove(SelectedOption);

            if (IsStandardOptionGroup)
                _eventAggregator.GetEvent<RemoveOptionEvent>().Publish(optionToRemove);
            SaveUpdatedOptions();
        }

        private void RemoveOption(ICharacterOption option)
        {
            T optionToRemove = (T)option;
            if (_optionGroup.Contains(optionToRemove))
                _optionGroup.Remove(optionToRemove);
        }

        private void SaveUpdatedOptions()
        {
            _eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Subscribe(SaveOptionGroupCompletedCallback);
            SaveOptionGroup();
        }

        public void SaveOptionGroup()
        {
            _eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        private void SaveOptionGroupCompletedCallback(object state)
        {
            _eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Unsubscribe(SaveOptionGroupCompletedCallback);
        }

        private T GetDefaultOption()
        {
            T defaultOption = default(T);
            if (OptionGroup.Type == OptionType.Identity)
                defaultOption = (T)Convert.ChangeType(_owner.DefaultIdentity, typeof(T));
            else if (OptionGroup.Type == OptionType.CharacterMovement)
                defaultOption = (T)Convert.ChangeType(_owner.DefaultMovement, typeof(T));
            else if (OptionGroup.Type == OptionType.Ability)
                defaultOption = (T)Convert.ChangeType(_owner.DefaultAbility, typeof(Attack));
            else if (OptionGroup.Type == OptionType.Mixed)
            {
                if (SelectedOption is Identity)
                    defaultOption = (T)Convert.ChangeType(_owner.DefaultIdentity, typeof(Identity));
                else if (SelectedOption is CharacterMovement)
                    defaultOption = (T)Convert.ChangeType(_owner.DefaultMovement, typeof(CharacterMovement));
            }
            return defaultOption;
        }

        private void SetDefaultOption(T value)
        {
            if (OptionGroup.Type == OptionType.Identity)
                _owner.DefaultIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            else if (OptionGroup.Type == OptionType.CharacterMovement)
                _owner.DefaultMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            else if (OptionGroup.Type == OptionType.Ability)
                _owner.DefaultAbility = (Attack)Convert.ChangeType(value, typeof(Attack));
        }

        private bool CanSetDefaultOption(object state)
        {
            return typeof(T) == typeof(Identity) || typeof(T) == typeof(CharacterMovement) || typeof(T) == typeof(AnimatedAbility);
        }

        private T GetActiveOption()
        {
            T activeOption = default(T);
            if (OptionGroup.Type == OptionType.Identity)
                activeOption = (T)Convert.ChangeType(_owner.ActiveIdentity, typeof(T));
            else if (OptionGroup.Type == OptionType.Ability)
                activeOption = (T)Convert.ChangeType(_owner.ActiveAbility, typeof(Attack));
            else if (OptionGroup.Type == OptionType.CharacterMovement)
                activeOption = (T)Convert.ChangeType(_owner.ActiveMovement, typeof(T));
            else if (OptionGroup.Type == OptionType.Mixed)
            {
                if (SelectedOption is Identity) activeOption = (T)Convert.ChangeType(_owner.ActiveIdentity, typeof(Identity));
                else if (SelectedOption is CharacterMovement) activeOption = (T)Convert.ChangeType(_owner.ActiveMovement, typeof(CharacterMovement));
                else if (SelectedOption is AnimatedAbility) activeOption = (T)Convert.ChangeType(_owner.ActiveAbility, typeof(Attack));
            }
            return activeOption;
        }

        private void SetActiveOption(T value)
        {
            if (OptionGroup.Type == OptionType.Identity)
                _owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            else if (OptionGroup.Type == OptionType.CharacterMovement)
                _owner.ActiveMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
        }

        private T GetSelectedOption()
        {
            return _selectedOption;
        }

        private void SetSelectedOption(T value)
        {
            if (_selectedOption != null && _selectedOption is AnimatedAbility)
            {
                AnimatedAbility ability = _selectedOption as AnimatedAbility;
                if (_selectedOption as AnimatedAbility != value as AnimatedAbility && ability.IsActive && !ability.Persistent)
                    StopAnimatedAbility(ability);
            }
            _selectedOption = value;
            if (value is Identity)
            {
                if (!Owner.HasBeenSpawned) SpawnAndTargetOwnerCharacter();
                _owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else if (value is CharacterMovement)
            {
                if (!Owner.HasBeenSpawned) SpawnAndTargetOwnerCharacter();
                _owner.ActiveMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            }
        }

        private void ClickTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            _clickTimer.Stop();
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_owner.ActiveAbility != null && !_owner.ActiveAbility.Persistent && !_owner.ActiveAbility.IsAttack && _owner.ActiveAbility != _gameState.GlobalDefaultSweepAbility)
                    DeActivateAnimatedAbility(_owner.ActiveAbility);
            }));
        }

        private bool CanPlayOption(object arg)
        {
            return (_selectedOption is AnimatedAbility || _selectedOption is CharacterMovement) && !_gameState.IsPlayingAttack;
        }

        private void PlayOption(object state)
        {
            if (_selectedOption is AnimatedAbility)
            {
                var ability = _selectedOption as AnimatedAbility;
                if (ability != null) { PlayAnimatedAbility(ability); _clickTimer.Start(); }
            }
            else
            {
                _eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(_selectedOption as CharacterMovement);
            }
        }

        private void PlayAnimatedAbility(AnimatedAbility ability)
        {
            OptionGroupCommands<T>.PlayAnimatedAbility(ability, _owner, _eventAggregator, () => ResolvePlayTarget(ability));
        }

        private void StopOption(object state)
        {
            if (_selectedOption is AnimatedAbility)
            {
                var abilityToStop = state as AnimatedAbility;
                var ability = _selectedOption as AnimatedAbility;
                if (ability != null && abilityToStop != null && ability == abilityToStop)
                    StopAnimatedAbility(ability);
            }
            else
            {
                OptionGroupCommands<T>.StopMovement(_selectedOption as CharacterMovement, _owner, _eventAggregator);
            }
        }

        private void StopAnimatedAbility(AnimatedAbility ability)
        {
            Character currentTarget = ResolvePlayTarget(ability);
            Owner.ActiveAbility = null;
            _eventAggregator.GetEvent<StopAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(currentTarget, ability));
        }

        private void DeActivateAnimatedAbility(AnimatedAbility ability)
        {
            Character currentTarget = ResolvePlayTarget(ability);
            _owner.ActiveAbility = null;
            ability.DeActivate(Target: currentTarget);
        }

        private Character ResolvePlayTarget(AnimatedAbility ability)
        {
            if (!ability.PlayOnTargeted)
            {
                SpawnAndTargetOwnerCharacter();
                return Owner;
            }
            var rostExpVM = Container.Resolve<Module.HeroVirtualTabletop.Roster.RosterExplorerViewModel>();
            Character currentTarget = rostExpVM.GetCurrentTarget() as Character;
            if (currentTarget == null)
            {
                SpawnAndTargetOwnerCharacter();
                return Owner;
            }
            return currentTarget;
        }

        private void TogglePlayOption(object obj)
        {
            if (SelectedOption != null && SelectedOption is AnimatedAbility && !(obj is AnimatedAbility))
                StopOption(SelectedOption);

            if (SelectedOption is AnimatedAbility)
            {
                AnimatedAbility ability = obj as AnimatedAbility;
                if (!ability.IsActive) PlayOption(obj); else StopOption(obj);
            }
            else if (SelectedOption is CharacterMovement)
            {
                CharacterMovement characterMovement = obj as CharacterMovement;
                if (!characterMovement.IsActive) PlayOption(obj); else StopOption(obj);
            }
        }

        private void OnAttackExecutionFinished(object state)
        {
            Owner.ActiveAbility = null;
            UpdateCommands();
        }

        private void SpawnAndTargetOwnerCharacter()
        {
            if (!Owner.HasBeenSpawned)
            {
                Crowds.CrowdMemberModel member = Owner as Crowds.CrowdMemberModel;
                if (member.RosterCrowd == null)
                    _eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(
                        new Tuple<Crowds.CrowdMemberModel, Crowds.CrowdModel>(member, member.RosterCrowd as Crowds.CrowdModel));
                member.Spawn(false);
            }
            Owner.Target();
        }

        private void SetNonCombatMovement(object state)
        {
            CharacterMovement characterMovement = SelectedOption as CharacterMovement;
            characterMovement.IsNonCombatMovement = state != null;
            OnPropertyChanged("IsNonCombatMovementSelected");
            OnPropertyChanged("IsCombatMovementSelected");
        }

        private void AddIdentity()
        {
            (_optionGroup as OptionGroup<Identity>).Add(
                new Identity("Model_Statesman", IdentityType.Model, _optionGroup.GetNewValidOptionName("Identity")));
            _eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Publish(null);
        }

        private void RemoveIdentity()
        {
            if (SelectedOption == null) return;
            if (OptionGroup.Count == 1) { MessageBox.Show("Every character must have at least 1 Identity"); return; }
            _optionGroup.Remove(SelectedOption);
        }

        private void RemoveCharacterMovement()
        {
            if (SelectedOption == null) return;
            CharacterMovement characterMovement = SelectedOption as CharacterMovement;
            if (Owner.DefaultMovement == characterMovement)
                Owner.DefaultMovement = null;
            _optionGroup.Remove(SelectedOption);
        }

        private void AddAbility()
        {
            Attack attack = new Attack(_optionGroup.GetNewValidOptionName("Ability"), owner: Owner);
            (_optionGroup as OptionGroup<AnimatedAbility>).Add(attack);
            _eventAggregator.GetEvent<AddOptionEvent>().Publish(attack);
        }

        private void AddCharacterMovement()
        {
            CharacterMovement characterMovement = new CharacterMovement(_optionGroup.GetNewValidOptionName("Movement"), Owner);
            (_optionGroup as OptionGroup<CharacterMovement>).Add(characterMovement);
            SelectedOption = (T)Convert.ChangeType(characterMovement, typeof(CharacterMovement));
            _eventAggregator.GetEvent<EditMovementEvent>().Publish(characterMovement);
        }

        private void EditOption(object obj)
        {
            if (SelectedOption is Identity)
            {
                Identity identity = (Identity)Convert.ChangeType(SelectedOption, typeof(Identity));
                _eventAggregator.GetEvent<EditIdentityEvent>().Publish(new Tuple<Identity, Character>(identity, Owner));
            }
            else if (SelectedOption is AnimatedAbility)
            {
                Attack attack = (Attack)Convert.ChangeType(SelectedOption, typeof(Attack));
                _eventAggregator.GetEvent<EditAbilityEvent>().Publish(new Tuple<AnimatedAbility, Character>(attack, Owner));
            }
            else if (SelectedOption is CharacterMovement)
            {
                CharacterMovement characterMovement = (CharacterMovement)Convert.ChangeType(SelectedOption, typeof(CharacterMovement));
                _eventAggregator.GetEvent<EditMovementEvent>().Publish(characterMovement);
            }
        }

        private void EnterEditMode(object state)
        {
            OriginalName = OptionGroup.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelEditMode(object state)
        {
            OptionGroup.Name = OriginalName;
            OriginalName = null;
            OnEditModeLeave(state, null);
        }

        private void RenameOptionGroupOnAdd(object state)
        {
            if (!NewOptionGroupAdded) return;
            NewOptionGroupAdded = false;
            EnterEditMode(null);
        }

        private void SubmitRename(object state)
        {
            if (OriginalName == null) return;
            string updatedName = Helper.GetTextFromControlObject(state);
            bool duplicateName = OriginalName != updatedName && Owner.OptionGroups.ContainsKey(updatedName);
            if (!duplicateName)
            {
                RenameOptionGroup(updatedName);
                OnEditModeLeave(state, null);
                SaveOptionGroup();
            }
            else
            {
                _messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, Messages.DUPLICATE_NAME_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error);
                CancelEditMode(state);
            }
        }

        private void RenameOptionGroup(string updatedName)
        {
            if (OriginalName == updatedName) { OriginalName = null; return; }
            OptionGroup.Name = updatedName;
            Owner.OptionGroups.UpdateKey(OriginalName, updatedName);
            OriginalName = null;
        }

        public void RemoveOption(int index)
        {
            (_optionGroup as OptionGroup<T>).RemoveAt(index);
        }

        public void InsertOption(int index, ICharacterOption characterOption)
        {
            OptionGroup<T> group = _optionGroup as OptionGroup<T>;
            var existingIndex = group.IndexOf((T)characterOption);
            if (existingIndex >= 0)
            {
                group.RemoveAt(existingIndex);
                if (index > 0 && index >= group.Count)
                    index -= 1;
            }
            group.Insert(index, (T)characterOption);
        }

        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (!IsReadOnlyMode && Keyboard.Modifiers == ModifierKeys.Control && _gameState.CurrentActiveWindowName == Constants.CHARACTER_EDITOR)
            {
                if (inputKey == Key.I && OptionGroup.Type == OptionType.Identity && AddOptionCommand.CanExecute(null))
                    AddIdentity();
                else if (inputKey == Key.M && OptionGroup.Type == OptionType.CharacterMovement && AddOptionCommand.CanExecute(null))
                    AddCharacterMovement();
                else if (inputKey == Key.P && OptionGroup.Type == OptionType.Ability && AddOptionCommand.CanExecute(null))
                    AddAbility();
                SaveUpdatedOptions();
            }
            else if (!IsReadOnlyMode && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                var optionToRemove = SelectedOption;
                if (inputKey == Key.I && OptionGroup.Type == OptionType.Identity && RemoveOptionCommand.CanExecute(null))
                    RemoveIdentity();
                else if (inputKey == Key.M && OptionGroup.Type == OptionType.CharacterMovement && RemoveOptionCommand.CanExecute(null))
                    RemoveCharacterMovement();
                else if (inputKey == Key.P && OptionGroup.Type == OptionType.Ability && RemoveOptionCommand.CanExecute(null))
                    _optionGroup.Remove(SelectedOption);
                else if (inputKey == Key.X && OptionGroup.Type == OptionType.Mixed && RemoveOptionCommand.CanExecute(null))
                    _optionGroup.Remove(SelectedOption);
                _eventAggregator.GetEvent<RemoveOptionEvent>().Publish(optionToRemove);
                SaveUpdatedOptions();
            }
            return null;
        }

        private void ShowHideCharacterOption(object state)
        {
            if (Owner.OptionGroupExpansionStates.ContainsKey(OptionGroup.Name))
                Owner.OptionGroupExpansionStates[OptionGroup.Name] = ShowOptions;
            else
                Owner.OptionGroupExpansionStates.Add(OptionGroup.Name, ShowOptions);
        }
    }

    internal static class OptionGroupCommands<T> where T : class, ICharacterOption
    {
        public static void PlayAnimatedAbility(AnimatedAbility ability, Character owner,
            EventAggregator eventAggregator, Func<Character> resolveTarget)
        {
            Action d = delegate ()
            {
                if (!ability.IsAttack)
                {
                    IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                    WindowsUtilities.SetForegroundWindow(winHandle);
                }
                Character currentTarget = resolveTarget();
                owner.ActiveAbility = ability;
                currentTarget.Target();
                eventAggregator.GetEvent<PlayAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(currentTarget, ability));
            };
            new AsyncDelegateExecuter(d, 5).ExecuteAsyncDelegate();
        }

        public static void StopMovement(CharacterMovement movement, Character owner, EventAggregator eventAggregator)
        {
            if (movement != null && movement.Movement != null && movement.IsActive)
            {
                owner.ActiveMovement = null;
                eventAggregator.GetEvent<StopMovementEvent>().Publish(movement);
            }
        }
    }
}
