using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.Shared;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace HeroVTT.Movements
{
    public class MovementEditorViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDesktopKeyEventHandler _desktopKeyEventHandler;
        private readonly IMovementGlobals _movementGlobals;
        private readonly MovementAuthoring _authoring;
        private Character _defaultCharacter;

        public event EventHandler<CustomEventArgs<bool>> MovementAdded;
        public event EventHandler EditModeEnter;
        public event EventHandler EditModeLeave;

        #region Public Properties

        private CharacterMovement currentCharacterMovement;
        public CharacterMovement CurrentCharacterMovement
        {
            get { return currentCharacterMovement; }
            set
            {
                currentCharacterMovement = value;
                IsDefaultMovementLoaded = currentCharacterMovement != null
                    && currentCharacterMovement.Character != null
                    && currentCharacterMovement.Character.DefaultMovement == currentCharacterMovement;
                OnPropertyChanged("CurrentCharacterMovement");
                RaiseMovementStateCommands();
            }
        }

        private MovementMember selectedMovementMember;
        public MovementMember SelectedMovementMember
        {
            get { return selectedMovementMember; }
            set
            {
                selectedMovementMember = value;
                if (selectedMovementMember != null && selectedMovementMember.MemberAbility != null)
                {
                    selectedMovementMember.MemberAbility.PropertyChanged -= SelectedMemberAbility_PropertyChanged;
                    selectedMovementMember.MemberAbility.PropertyChanged += SelectedMemberAbility_PropertyChanged;
                }
                OnPropertyChanged("SelectedMovementMember");
            }
        }

        private ObservableCollection<System.Windows.Forms.Keys> availableKeys;
        public ObservableCollection<System.Windows.Forms.Keys> AvailableKeys
        {
            get { return availableKeys; }
            set { availableKeys = value; OnPropertyChanged("AvailableKeys"); }
        }

        private ObservableCollection<Movement> availableMovements;
        public ObservableCollection<Movement> AvailableMovements
        {
            get { return availableMovements; }
            set { availableMovements = value; OnPropertyChanged("AvailableMovements"); }
        }

        private Movement selectedMovement;
        public Movement SelectedMovement
        {
            get { return selectedMovement; }
            set
            {
                selectedMovement = value;
                ApplySelectedMovementToCharacter();
                SaveMovement(null);
                OnPropertyChanged("SelectedMovement");
                RemoveMovementCommand.RaiseCanExecuteChanged();
                ToggleGravityForMovementCommand.RaiseCanExecuteChanged();
            }
        }

        private bool isShowingMovementEditor;
        public bool IsShowingMovementEditor
        {
            get { return isShowingMovementEditor; }
            set
            {
                isShowingMovementEditor = value;
                if (value)
                    _movementGlobals.CurrentActiveWindowName = Constants.MOVEMENT_EDITOR;
                else
                    _eventAggregator.GetEvent<PanelClosedEvent>().Publish(Constants.MOVEMENT_EDITOR);
                OnPropertyChanged("IsShowingMovementEditor");
            }
        }

        private bool isDefaultMovementLoaded;
        public bool IsDefaultMovementLoaded
        {
            get { return isDefaultMovementLoaded; }
            set { isDefaultMovementLoaded = value; OnPropertyChanged("IsDefaultMovementLoaded"); }
        }

        private CollectionViewSource referenceAbilitiesCVS;
        public CollectionViewSource ReferenceAbilitiesCVS
        {
            get { return referenceAbilitiesCVS; }
        }

        private string filter;
        public string Filter
        {
            get { return filter; }
            set
            {
                filter = value;
                if (referenceAbilitiesCVS != null && referenceAbilitiesCVS.View != null)
                    referenceAbilitiesCVS.View.Refresh();
                OnPropertyChanged("Filter");
            }
        }

        public bool CanEditMovementOptions
        {
            get { return !_movementGlobals.IsPlayingAttack; }
        }

        public string OriginalName { get; set; }

        #endregion

        #region Commands

        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> EnterMovementEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitMovementRenameCommand { get; private set; }
        public DelegateCommand<object> CancelMovementEditModeCommand { get; private set; }
        public DelegateCommand<object> AddMovementCommand { get; private set; }
        public DelegateCommand<object> SaveMovementCommand { get; private set; }
        public DelegateCommand<object> RemoveMovementCommand { get; private set; }
        public DelegateCommand<object> LoadResourcesCommand { get; private set; }
        public DelegateCommand<object> SetDefaultMovementCommand { get; private set; }
        public DelegateCommand<object> DemoDirectionalMoveCommand { get; private set; }
        public DelegateCommand<object> PlayMovementCommand { get; private set; }
        public DelegateCommand<object> LoadAbilityEditorCommand { get; private set; }
        public DelegateCommand<object> ToggleGravityForMovementCommand { get; private set; }
        public DelegateCommand<object> ToggleSetCombatMovementCommand { get; private set; }

        #endregion

        public MovementEditorViewModel(
            IBusyService busyService,
            IUnityContainer container,
            IMessageBoxService messageBoxService,
            IDesktopKeyEventHandler keyEventHandler,
            EventAggregator eventAggregator,
            IMovementGlobals movementGlobals)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _messageBoxService = messageBoxService;
            _desktopKeyEventHandler = keyEventHandler;
            _movementGlobals = movementGlobals;
            _authoring = new MovementAuthoring();

            InitializeCommands();
            SubscribeToEvents();
            InitializeMovementSelections();
            _desktopKeyEventHandler.AddKeyEventHandler(RetrieveEventFromKeyInput);
        }

        #region Initialization

        private void InitializeCommands()
        {
            CloseEditorCommand = new DelegateCommand<object>(CloseEditor);
            SubmitMovementRenameCommand = new DelegateCommand<object>(SubmitMovementRename);
            SaveMovementCommand = new DelegateCommand<object>(SaveMovement, CanSaveMovement);
            EnterMovementEditModeCommand = new DelegateCommand<object>(EnterMovementEditMode);
            CancelMovementEditModeCommand = new DelegateCommand<object>(CancelMovementEditMode);
            AddMovementCommand = new DelegateCommand<object>(AddMovement, CanAddMovement);
            RemoveMovementCommand = new DelegateCommand<object>(RemoveMovement, CanRemoveMovement);
            SetDefaultMovementCommand = new DelegateCommand<object>(SetDefaultMovement, CanSetDefaultMovement);
            LoadResourcesCommand = new DelegateCommand<object>(LoadResources);
            DemoDirectionalMoveCommand = new DelegateCommand<object>(DemoDirectionalMovement, CanDemoDirectionalMovement);
            LoadAbilityEditorCommand = new DelegateCommand<object>(LoadAbilityEditor, CanLoadAbilityEditor);
            PlayMovementCommand = new DelegateCommand<object>(DemoMovement, CanDemoMovement);
            ToggleGravityForMovementCommand = new DelegateCommand<object>(ToggleGravityForMovement, CanToggleGravityForMovement);
            ToggleSetCombatMovementCommand = new DelegateCommand<object>(ToggleSetCombatMovement, CanToggleSetCombatMovement);
        }

        private void SubscribeToEvents()
        {
            _eventAggregator.GetEvent<Module.HeroVirtualTabletop.Library.Events.EditMovementEvent>().Subscribe(LoadMovement);
            _eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(LoadReferenceResource);
            _eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(AttackInitiated);
            _eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(AttackEnded);
            _eventAggregator.GetEvent<PlayMovementConfirmedEvent>().Subscribe(PlayMovement);
            _eventAggregator.GetEvent<StopMovementEvent>().Subscribe(StopMovement);
        }

        private void InitializeMovementSelections()
        {
            CurrentCharacterMovement = null;
            if (availableKeys != null) return;

            availableKeys = new ObservableCollection<System.Windows.Forms.Keys>();
            foreach (var key in Enum.GetValues(typeof(System.Windows.Forms.Keys)).Cast<System.Windows.Forms.Keys>())
            {
                if (!IsMovementKey(key))
                    availableKeys.Add(key);
            }
        }

        #endregion

        #region Load Movement

        private void LoadMovement(CharacterMovement characterMovement)
        {
            InitializeMovementSelections();
            IsShowingMovementEditor = true;
            CurrentCharacterMovement = characterMovement;
            referenceAbilitiesCVS = null;
            _eventAggregator.GetEvent<NeedDefaultCharacterRetrievalEvent>().Publish(LoadAvailableMovements);
            SelectedMovement = characterMovement.Movement;
        }

        private void LoadAvailableMovements(Character defaultCharacter)
        {
            _defaultCharacter = defaultCharacter;
            string currentMovementName = CurrentCharacterMovement.Movement != null ? CurrentCharacterMovement.Movement.Name : "";
            AvailableMovements = _authoring.BuildAvailableMovements(defaultCharacter, CurrentCharacterMovement.Character, currentMovementName);
        }

        #endregion

        #region Rename Movement

        private void EnterMovementEditMode(object state)
        {
            if (SelectedMovement == null) return;
            OriginalName = SelectedMovement.Name;
            if (EditModeEnter != null) EditModeEnter(state, EventArgs.Empty);
        }

        private void CancelMovementEditMode(object state)
        {
            SelectedMovement.Name = OriginalName;
            if (EditModeLeave != null) EditModeLeave(state, EventArgs.Empty);
        }

        private void SubmitMovementRename(object state)
        {
            string originalName = OriginalName;
            if (!_authoring.TrySubmitRename(state, ref originalName, SelectedMovement, CurrentCharacterMovement, _defaultCharacter, _messageBoxService, CancelMovementEditMode)) return;
            OriginalName = originalName;
            if (EditModeLeave != null) EditModeLeave(state, EventArgs.Empty);
            SaveMovement(null);
        }

        #endregion

        #region Add Movement

        private bool CanAddMovement(object state) { return !_movementGlobals.IsPlayingAttack; }

        private void AddMovement(object state)
        {
            var result = _authoring.AddMovement(_defaultCharacter, CurrentCharacterMovement, AvailableMovements);
            ApplyAddMovementResult(result);
        }

        private void ApplyAddMovementResult(MovementAddResult result)
        {
            AvailableMovements = result.AvailableMovements;
            if (result.UpdatedCurrent != null) CurrentCharacterMovement = result.UpdatedCurrent;
            SelectedMovement = result.Movement;
            if (MovementAdded != null) MovementAdded(result.Movement, null);
            SaveMovement(null);
        }

        public string GetNewValidMovementName(string name = "Movement")
        {
            return _authoring.GetNewValidMovementName(_defaultCharacter, name);
        }

        #endregion

        #region Remove Movement

        private bool CanRemoveMovement(object state) { return SelectedMovement != null && !_movementGlobals.IsPlayingAttack; }

        private void RemoveMovement(object state)
        {
            _eventAggregator.GetEvent<Module.HeroVirtualTabletop.Library.Events.RemoveMovementEvent>().Publish(SelectedMovement.Name);
            SaveMovement(null);
            CloseEditor();
        }

        #endregion

        #region Save / Close

        private bool CanSaveMovement(object state) { return !_movementGlobals.IsPlayingAttack; }

        private void SaveMovement(object state)
        {
            _eventAggregator.GetEvent<SaveCrowdEvent>().Publish(state);
        }

        private void CloseEditor(object state = null)
        {
            CurrentCharacterMovement = null;
            IsShowingMovementEditor = false;
        }

        #endregion

        #region Demo Movement

        private bool CanDemoDirectionalMovement(object state)
        {
            MovementMember member = state as MovementMember;
            return member != null && member.MemberAbility != null && member.MemberAbility.Reference != null;
        }

        private void DemoDirectionalMovement(object state)
        {
            MovementMember member = (MovementMember)state;
            member.MemberAbility.Reference.Play(false, CurrentCharacterMovement.Character);
        }

        private bool CanDemoMovement(object state)
        {
            return CurrentCharacterMovement != null && CurrentCharacterMovement.Movement != null && !CurrentCharacterMovement.IsActive;
        }

        private void DemoMovement(object state)
        {
            _eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(CurrentCharacterMovement);
        }

        #endregion

        #region Play / Stop Movement

        private void PlayMovement(Tuple<CharacterMovement, List<Character>> tuple)
        {
            CurrentCharacterMovement = tuple.Item1;
            CurrentCharacterMovement.Character.ActiveMovement = tuple.Item1;
            tuple.Item1.ActivateMovement(tuple.Item2);
        }

        private void StopMovement(CharacterMovement characterMovement)
        {
            characterMovement.DeactivateMovement();
        }

        #endregion

        #region Toggle Gravity / Combat Movement

        private bool CanToggleGravityForMovement(object state) { return SelectedMovement != null; }

        private void ToggleGravityForMovement(object state)
        {
            SaveMovement(null);
        }

        private bool CanToggleSetCombatMovement(object state) { return CurrentCharacterMovement != null; }

        private void ToggleSetCombatMovement(object state)
        {
            SaveMovement(null);
            _eventAggregator.GetEvent<CombatMovementChangedEvent>().Publish(CurrentCharacterMovement);
        }

        #endregion

        #region Load Ability Editor

        private bool CanLoadAbilityEditor(object state) { return !_movementGlobals.IsPlayingAttack; }

        private void LoadAbilityEditor(object state)
        {
            MovementMember member = (MovementMember)state;
            _eventAggregator.GetEvent<EditAbilityEvent>().Publish(
                new Tuple<AnimatedAbility, Character>(member.MemberAbility.Reference, _defaultCharacter));
        }

        #endregion

        #region Set Default Movement

        private bool CanSetDefaultMovement(object state) { return CurrentCharacterMovement != null && !_movementGlobals.IsPlayingAttack; }

        private void SetDefaultMovement(object state)
        {
            _authoring.ApplyDefaultMovement(CurrentCharacterMovement.Character, CurrentCharacterMovement, IsDefaultMovementLoaded);
            SaveMovement(null);
        }

        #endregion

        #region Attack Consistency

        private void AttackInitiated(Tuple<Character, Attack> tuple) { RaiseAttackStateCommands(); }
        private void AttackEnded(object state) { RaiseAttackStateCommands(); }

        private void RaiseAttackStateCommands()
        {
            SaveMovementCommand.RaiseCanExecuteChanged();
            AddMovementCommand.RaiseCanExecuteChanged();
            RemoveMovementCommand.RaiseCanExecuteChanged();
            OnPropertyChanged("CanEditMovementOptions");
        }

        #endregion

        #region Animation Resources

        private void SelectedMemberAbility_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Resource") return;
            AnimationElement element = (AnimationElement)sender;
            element.DisplayName = Path.GetFileNameWithoutExtension(element.Resource);
            SaveMovement(null);
        }

        private void LoadResources(object state)
        {
            _eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }

        private void LoadReferenceResource(ObservableCollection<AnimatedAbility> abilityCollection)
        {
            if (referenceAbilitiesCVS == null)
            {
                referenceAbilitiesCVS = new CollectionViewSource();
                var refAbilityCollection = abilityCollection
                    .Where(a => !a.IsAttack)
                    .Select(x => new AnimationResource(x, x.Name))
                    .OrderBy(x => x, new ReferenceAbilityResourceComparer());
                referenceAbilitiesCVS.Source = new ObservableCollection<AnimationResource>(refAbilityCollection);
                referenceAbilitiesCVS.View.Filter += ResourcesCVS_Filter;
            }
            else
            {
                SyncReferenceResources(abilityCollection);
            }
            OnPropertyChanged("ReferenceAbilitiesCVS");
        }

        private void SyncReferenceResources(ObservableCollection<AnimatedAbility> abilityCollection)
        {
            var updatedResources = abilityCollection
                .Where(a => !a.IsAttack)
                .Select(x => new AnimationResource(x, x.Name));
            var currentResources = referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>;

            var added = updatedResources
                .Where(a => currentResources.FirstOrDefault(ca => ca.Name == a.Name && OwnerMatches(ca, a)) == null);

            if (added.Any())
            {
                foreach (var resource in added)
                    currentResources.Add(resource);
            }
            else
            {
                var deleted = currentResources
                    .Where(ca => updatedResources.FirstOrDefault(a => a.Name == ca.Name && OwnerMatches(a, ca)) == null)
                    .ToList();
                foreach (var resource in deleted)
                    currentResources.Remove(resource);
            }
        }

        private static bool OwnerMatches(AnimationResource a, AnimationResource b)
        {
            return a.Reference != null && a.Reference.Owner != null
                && b.Reference != null && b.Reference.Owner != null
                && a.Reference.Owner.Name == b.Reference.Owner.Name;
        }

        private bool ResourcesCVS_Filter(object item)
        {
            AnimationResource animationRes = (AnimationResource)item;
            if (string.IsNullOrWhiteSpace(Filter)) return true;

            bool matchesReference = false;
            if (animationRes.Reference != null && animationRes.Reference.Owner != null)
            {
                matchesReference = Regex.IsMatch(animationRes.Reference.Name, Filter, RegexOptions.IgnoreCase)
                    || Regex.IsMatch(animationRes.Reference.Owner.Name, Filter, RegexOptions.IgnoreCase);
            }

            return Regex.IsMatch(animationRes.TagLine, Filter, RegexOptions.IgnoreCase) || matchesReference;
        }

        #endregion

        #region Desktop Key Handling

        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, Key inputKey)
        {
            if (_movementGlobals.CurrentActiveWindowName != Constants.MOVEMENT_EDITOR) return null;

            if ((inputKey == Key.OemPlus || inputKey == Key.Add) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (AddMovementCommand.CanExecute(null)) AddMovementCommand.Execute(null);
            }
            else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (RemoveMovementCommand.CanExecute(null)) RemoveMovementCommand.Execute(null);
            }
            else if (inputKey == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (SelectedMovementMember != null && DemoDirectionalMoveCommand.CanExecute(SelectedMovementMember))
                    DemoDirectionalMoveCommand.Execute(SelectedMovementMember);
            }
            else if (inputKey == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (SelectedMovementMember != null && LoadAbilityEditorCommand.CanExecute(SelectedMovementMember))
                    LoadAbilityEditorCommand.Execute(SelectedMovementMember);
            }
            else if (inputKey == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (PlayMovementCommand.CanExecute(null)) PlayMovementCommand.Execute(null);
            }
            return null;
        }

        #endregion

        #region Private Helpers

        private void ApplySelectedMovementToCharacter()
        {
            if (selectedMovement == null || CurrentCharacterMovement == null) return;
            if (CurrentCharacterMovement.Character == _defaultCharacter) return;

            CurrentCharacterMovement.Movement = selectedMovement;
            string prevName = CurrentCharacterMovement.Name;
            CurrentCharacterMovement.Name = selectedMovement.Name;
            CurrentCharacterMovement.Character.Movements.UpdateKey(prevName, selectedMovement.Name);
        }

        private void RaiseMovementStateCommands()
        {
            if (SaveMovementCommand != null) SaveMovementCommand.RaiseCanExecuteChanged();
            if (SetDefaultMovementCommand != null) SetDefaultMovementCommand.RaiseCanExecuteChanged();
            if (PlayMovementCommand != null) PlayMovementCommand.RaiseCanExecuteChanged();
            if (ToggleSetCombatMovementCommand != null) ToggleSetCombatMovementCommand.RaiseCanExecuteChanged();
        }

        private bool IsMovementKey(System.Windows.Forms.Keys key)
        {
            return key == System.Windows.Forms.Keys.W
                || key == System.Windows.Forms.Keys.A
                || key == System.Windows.Forms.Keys.S
                || key == System.Windows.Forms.Keys.D
                || key == System.Windows.Forms.Keys.X
                || key == System.Windows.Forms.Keys.Z
                || key == System.Windows.Forms.Keys.Space
                || key == System.Windows.Forms.Keys.Left
                || key == System.Windows.Forms.Keys.Right
                || key == System.Windows.Forms.Keys.Up
                || key == System.Windows.Forms.Keys.Down
                || key == System.Windows.Forms.Keys.Alt
                || key == System.Windows.Forms.Keys.Control
                || key == System.Windows.Forms.Keys.Shift
                || key == System.Windows.Forms.Keys.Enter
                || key == System.Windows.Forms.Keys.CapsLock
                || key == System.Windows.Forms.Keys.Escape
                || key == System.Windows.Forms.Keys.M
                || key == System.Windows.Forms.Keys.P
                || key == System.Windows.Forms.Keys.Oemplus
                || key == System.Windows.Forms.Keys.Add
                || key == System.Windows.Forms.Keys.Subtract
                || key == System.Windows.Forms.Keys.OemMinus
                || key == System.Windows.Forms.Keys.Delete
                || key == System.Windows.Forms.Keys.Back;
        }

        #endregion
    }
}
