using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.OptionGroups
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
        #region Private Fields

        private EventAggregator eventAggregator;
        private OptionGroup<T> optionGroup;

        private Timer clickTimer_AbilityPlay = new Timer();
        private IDesktopKeyEventHandler desktopKeyEventHandler;

        #endregion

        #region Events
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

        #endregion

        #region Public Properties

        public OptionGroup<T> OptionGroup
        {
            get
            {
                return optionGroup;
            }
            private set
            {
                optionGroup = value;
                OnPropertyChanged("OptionGroup");
            }
        }


        IOptionGroup IOptionGroupViewModel.OptionGroup
        {
            get
            {
                return OptionGroup as IOptionGroup;
            }
        }

        private T selectedOption;
        public T SelectedOption
        {
            get
            {
                return GetSelectedOption();
            }
            set
            {
                if(value == null || (value != null && value.IsEnabled))
                    SetSelectedOption(value);
                OnPropertyChanged("SelectedOption");
                OnPropertyChanged("IsCombatMovementSelected");
                OnPropertyChanged("IsNonCombatMovementSelected");
                this.PlayOptionCommand.RaiseCanExecuteChanged();
                this.StopOptionCommand.RaiseCanExecuteChanged();
            }
        }

        public T DefaultOption
        {
            get
            {
                return GetDefaultOption();
            }
            set
            {
                SetDefaultOption(value);
                OnPropertyChanged("DefaultOption");
                this.SaveOptionGroup();
            }
        }

        public T ActiveOption
        {
            get
            {
                return GetActiveOption();
            }
            set
            {
                SetActiveOption(value);
                OnPropertyChanged("ActiveOption");
            }
        }

        private bool isReadOnlyMode;
        public bool IsReadOnlyMode
        {
            get
            {
                return isReadOnlyMode;
            }
            set
            {
                isReadOnlyMode = value;
                OnPropertyChanged("IsReadOnlyMode");
            }
        }

        private bool showOptions;
        public bool ShowOptions
        {
            get
            {
                return showOptions;
            }
            set
            {
                showOptions = value; 
                OnPropertyChanged("ShowOptions");
            }
        }

        private string addOptionTooltip;
        public string AddOptionTooltip
        {
            get
            {
                return addOptionTooltip;
            }
            set
            {
                addOptionTooltip = value;
                OnPropertyChanged("OptionTooltip");
            }
        }
        private string removeOptionTooltip;
        public string RemoveOptionTooltip
        {
            get
            {
                return removeOptionTooltip;
            }
            set
            {
                removeOptionTooltip = value;
                OnPropertyChanged("RemoveOptionTooltip");
            }
        }

        private string loadingOptionName;
        public string LoadingOptionName
        {
            get
            {
                return loadingOptionName;
            }
            set
            {
                loadingOptionName = value;
                if (!string.IsNullOrEmpty(value))
                {
                    var optionToLoad = OptionGroup.FirstOrDefault(o => o.Name == value);
                    if (optionToLoad != null)
                    {
                        ShowOptions = true;
                        this.SelectedOption = optionToLoad;
                        this.TogglePlayOption(optionToLoad);
                    }
                }
            }
        }

        private Character owner;
        public Character Owner
        {
            get { return owner; }
            private set { owner = value; }
        }
        public bool IsStandardOptionGroup
        {
            get
            {
                return OptionGroup.Name == Constants.IDENTITY_OPTION_GROUP_NAME || OptionGroup.Name == Constants.ABILITY_OPTION_GROUP_NAME || OptionGroup.Name == Constants.MOVEMENT_OPTION_GROUP_NAME;
            }
        }
        public bool IsCombatMovementSelected
        {
            get
            {
                return this.SelectedOption != null && this.SelectedOption is CharacterMovement && !(this.SelectedOption as CharacterMovement).IsNonCombatMovement;
            }
        }
        public bool IsNonCombatMovementSelected
        {
            get
            {
                return this.SelectedOption != null && this.SelectedOption is CharacterMovement && (this.SelectedOption as CharacterMovement).IsNonCombatMovement;
            }
        }

        private IMessageBoxService messageBoxService;

        public bool NewOptionGroupAdded { get; set; }

        public string OriginalName { get; set; }
        DesktopKeyEventHandler KeyHandler { get; set; }

        #endregion

        #region Commands

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

        #endregion

        #region Constructor

        public OptionGroupViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, IDesktopKeyEventHandler keyEventHandler, EventAggregator eventAggregator, OptionGroup<T> optionGroup, Character owner)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.desktopKeyEventHandler = keyEventHandler;
            this.Owner = owner;
            this.Owner.PropertyChanged += Owner_PropertyChanged;
            this.OptionGroup = optionGroup;
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(this.OnAttackExecutionFinished);
            this.eventAggregator.GetEvent<CombatMovementChangedEvent>().Subscribe((CharacterMovement cm) => {
                OnPropertyChanged("IsNonCombatMovementSelected");
                OnPropertyChanged("IsCombatMovementSelected");
            });
            if (!this.IsStandardOptionGroup)
            {
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Subscribe(this.RemoveOption);
            }

            clickTimer_AbilityPlay.AutoReset = false;
            clickTimer_AbilityPlay.Interval = 2000;
            clickTimer_AbilityPlay.Elapsed +=
                new ElapsedEventHandler(clickTimer_AbilityPlay_Elapsed);

            InitializeCommands();
            SetTooltips();
            this.InitializeDesktopKeyEventHandlers();
        }

        private void InitializeDesktopKeyEventHandlers()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        public void RemoveDesktopKeyEventHandlers()
        {
            this.desktopKeyEventHandler.RemoveKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        private void SetTooltips()
        {
            if(this.OptionGroup.Type == OptionType.Ability)
            {
                this.AddOptionTooltip = "Add Power (Ctrl+P)";
                this.RemoveOptionTooltip = "Remove Power (Alt+P)";
            }
            else if(this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                this.AddOptionTooltip = "Add Movement (Ctrl+M)";
                this.RemoveOptionTooltip = "Remove Movement (Alt+M)";
            }
            else if(this.OptionGroup.Type == OptionType.Identity)
            {
                this.AddOptionTooltip = "Add Identity (Ctrl+I)";
                this.RemoveOptionTooltip = "Remove Identity (Alt+I)";
            }
            else
            {
                this.AddOptionTooltip = "Add Custom Option"; // Not needed
                this.RemoveOptionTooltip = "Remove Custom Option (Alt+X)";
            }
        }

        private void Owner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                switch (e.PropertyName)
                {
                    case "ActiveIdentity":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultIdentity":
                        OnPropertyChanged("DefaultOption");
                        break;
                }
            }
            else if (this.OptionGroup.Type == OptionType.Ability)
            {
                switch (e.PropertyName)
                {
                    case "ActiveAbility":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                }
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                switch (e.PropertyName)
                {
                    case "ActiveMovement":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultMovement":
                        OnPropertyChanged("DefaultOption");
                        break;
                }
            }
            else if (this.OptionGroup.Type == OptionType.Mixed)
            {
                switch (e.PropertyName)
                {
                    case "ActiveIdentity":
                    case "ActiveMovement":
                    case "ActiveAbility":
                        OnPropertyChanged("ActiveOption");
                        OnPropertyChanged("SelectedOption");
                        break;
                    case "DefaultIdentity":
                    case "DefaultMovement":
                        OnPropertyChanged("DefaultOption");
                        break;
                }
            }
        }

        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            this.AddOptionCommand = new DelegateCommand<object>(this.AddOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });
            this.RemoveOptionCommand = new DelegateCommand<object>(this.RemoveOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });

            this.SetDefaultOptionCommand = new DelegateCommand<object>(this.SetDefaultOption, this.CanSetDefaultOption);
            this.EditOptionCommand = new DelegateCommand<object>(this.EditOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });
            this.PlayOptionCommand = new DelegateCommand<object>(this.PlayOption, this.CanPlayOption);
            this.StopOptionCommand = new DelegateCommand<object>(this.StopOption, this.CanStopOption);
            this.TogglePlayOptionCommand = new DelegateCommand<object>(this.TogglePlayOption, (object state) => { return !Helper.GlobalVariables_IsPlayingAttack; });
            this.ShowHideCharacterOptionCommand = new DelegateCommand<object>(this.ShowHideCharacterOption);
            this.EnterEditModeCommand = new DelegateCommand<object>(this.EnterEditMode, this.CanEnterEditMode);
            this.SubmitOptionGroupRenameCommand = new DelegateCommand<object>(this.SubmitRename);
            this.CancelEditModeCommand = new DelegateCommand<object>(this.CancelEditMode);
            this.RenameNewOptionGroupCommand = new DelegateCommand<object>(this.RenameOptionGroup);
            this.SetNonCombatMovementCommand = new DelegateCommand<object>(this.SetNonCombatMovement);
        }

        #endregion

        #region Methods

        private void UpdateCommands()
        {
            this.AddOptionCommand.RaiseCanExecuteChanged();
            this.RemoveOptionCommand.RaiseCanExecuteChanged();
            this.EditOptionCommand.RaiseCanExecuteChanged();
            this.TogglePlayOptionCommand.RaiseCanExecuteChanged();
            this.PlayOptionCommand.RaiseCanExecuteChanged();
            this.StopOptionCommand.RaiseCanExecuteChanged();
        }

        private void AddOption(object state)
        {
            if (typeof(T) == typeof(Identity))
            {
                AddIdentity(state);
            }
            else if (typeof(T) == typeof(AnimatedAbility))
            {
                AddAbility(state);
            }
            else if (typeof(T) == typeof(CharacterMovement))
            {
                AddCharacterMovement(state);
            }
            SaveUpdatedOptions();
        }

        private void RemoveOption(object state)
        {
            var optionToRemove = SelectedOption;
            if (typeof(T) == typeof(Identity))
            {
                RemoveIdentity(state);
            }
            else if (typeof(T) == typeof(CharacterMovement))
            {
                RemoveCharacterMovement(state);
            }
            else
            {
                optionGroup.Remove(SelectedOption);
            }
            if (this.IsStandardOptionGroup)
            {
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Publish(optionToRemove);
            }
            SaveUpdatedOptions();
        }

        private void SaveUpdatedOptions()
        {
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Subscribe(this.SaveOptionGroupCompletedCallback);
            this.SaveOptionGroup();
        }

        private void RemoveOption(ICharacterOption option)
        {
            T optionToRemove = (T)option;
            if (optionGroup.Contains(optionToRemove))
            {
                optionGroup.Remove(optionToRemove);
            }
        }

        public void SaveOptionGroup()
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        private void SaveOptionGroupCompletedCallback(object state)
        {
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            //this.eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Publish(null);
            this.eventAggregator.GetEvent<SaveCrowdCompletedEvent>().Unsubscribe(this.SaveOptionGroupCompletedCallback);
        }

        private T GetDefaultOption()
        {
            T defaultOption = default(T);
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                defaultOption = (T)Convert.ChangeType(owner.DefaultIdentity, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                defaultOption = (T)Convert.ChangeType(owner.DefaultMovement, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.Ability)
            {
                defaultOption = (T)Convert.ChangeType(owner.DefaultAbility, typeof(Attack));
            }
            else if (this.OptionGroup.Type == OptionType.Mixed)
            {
                // return the default option of the selected type
                if (SelectedOption is Identity)
                {
                    defaultOption = (T)Convert.ChangeType(owner.DefaultIdentity, typeof(Identity));
                }
                else if (SelectedOption is CharacterMovement)
                {
                    defaultOption = (T)Convert.ChangeType(owner.DefaultMovement, typeof(CharacterMovement));
                }
                else if (this.OptionGroup.Type == OptionType.Ability)
                {
                    defaultOption = (T)Convert.ChangeType(owner.DefaultAbility, typeof(Attack));
                }
            }

            return defaultOption;
        }

        private void SetDefaultOption(T value)
        {
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                owner.DefaultIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                owner.DefaultMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            }
            else if (this.OptionGroup.Type == OptionType.Ability)
            {
                owner.DefaultAbility = (Attack)Convert.ChangeType(value, typeof(Attack));
            }
        }
        private bool CanSetDefaultOption(object state)
        {
            return typeof(T) == typeof(Identity) || typeof(T) == typeof(CharacterMovement) || typeof(T) == typeof(AnimatedAbility); 
        }

        private void SetDefaultOption(object state)
        {
            DefaultOption = SelectedOption;
        }

        private T GetActiveOption()
        {
            T activeOption = default(T);
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                activeOption = (T)Convert.ChangeType(owner.ActiveIdentity, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.Ability)
            {
                activeOption = (T)Convert.ChangeType(owner.ActiveAbility, typeof(Attack));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                activeOption = (T)Convert.ChangeType(owner.ActiveMovement, typeof(T));
            }
            else if (this.OptionGroup.Type == OptionType.Mixed)
            {
                if (SelectedOption is Identity)
                {
                    activeOption = (T)Convert.ChangeType(owner.ActiveIdentity, typeof(Identity));
                }
                else if (SelectedOption is CharacterMovement)
                {
                    activeOption = (T)Convert.ChangeType(owner.ActiveMovement, typeof(CharacterMovement));
                }
                else if (SelectedOption is AnimatedAbility)
                {
                    activeOption = (T)Convert.ChangeType(owner.ActiveAbility, typeof(Attack));
                }
            }

            return activeOption;
        }

        private void SetActiveOption(T value)
        {
            if (this.OptionGroup.Type == OptionType.Identity)
            {
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else if (this.OptionGroup.Type == OptionType.CharacterMovement)
            {
                owner.ActiveMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            }
        }

        private T GetSelectedOption()
        {
            return selectedOption;
        }

        private void SetSelectedOption(T value)
        {
            if (selectedOption != null && selectedOption is AnimatedAbility)
            {
                if (selectedOption as AnimatedAbility != value as AnimatedAbility)
                {
                    AnimatedAbility ability = selectedOption as AnimatedAbility;
                    if (ability.IsActive && !ability.Persistent)
                        StopAnimatedAbility(ability);
                }
            }
            selectedOption = value;
            if (value is Identity)
            {
                if (!this.Owner.HasBeenSpawned)
                    this.SpawnAndTargetOwnerCharacter();
                owner.ActiveIdentity = (Identity)Convert.ChangeType(value, typeof(Identity));
            }
            else if (value is CharacterMovement)
            {
                if (!this.Owner.HasBeenSpawned)
                    this.SpawnAndTargetOwnerCharacter();
                owner.ActiveMovement = (CharacterMovement)Convert.ChangeType(value, typeof(CharacterMovement));
            }
        }

        private void clickTimer_AbilityPlay_Elapsed(object sender, ElapsedEventArgs e)
        {
            clickTimer_AbilityPlay.Stop();
            Action d = delegate()
            {
                if(owner.ActiveAbility != null && !owner.ActiveAbility.Persistent && !owner.ActiveAbility.IsAttack && owner.ActiveAbility != Helper.GlobalDefaultSweepAbility)
                {
                    DeActivateAnimatedAbility(owner.ActiveAbility);
                    //owner.ActiveAbility.IsActive = false;
                    //OnPropertyChanged("IsActive");
                }
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        private bool CanPlayOption(object arg)
        {
            return ((selectedOption is AnimatedAbility) || (selectedOption is CharacterMovement) && !Helper.GlobalVariables_IsPlayingAttack);
        }

        private void PlayOption(object state)
        {
            if(selectedOption is AnimatedAbility)
            {
                AnimatedAbility ability = selectedOption as AnimatedAbility;
                if (ability != null)
                {
                    PlayAnimatedAbility(ability);
                    clickTimer_AbilityPlay.Start();
                }
            }
            else
            {
                CharacterMovement characterMovement = selectedOption as CharacterMovement;
                this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(characterMovement);
            }
        }

        private void PlayAnimatedAbility(AnimatedAbility ability)
        {
            Action d = delegate ()
            {
                if (!ability.IsAttack)
                {
                    IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                    WindowsUtilities.SetForegroundWindow(winHandle);
                }

                Character currentTarget = null;
                if (!ability.PlayOnTargeted)
                {
                    this.SpawnAndTargetOwnerCharacter();
                    currentTarget = this.Owner;
                }
                else
                {
                    Roster.RosterExplorerViewModel rostExpVM = this.Container.Resolve<Roster.RosterExplorerViewModel>();
                    currentTarget = rostExpVM.GetCurrentTarget() as Character;
                    if (currentTarget == null)
                    {
                        this.SpawnAndTargetOwnerCharacter();
                        currentTarget = this.Owner;
                    }
                }
                owner.ActiveAbility = ability;
                currentTarget.Target();
                //ability.Play(Target: currentTarget);
                this.eventAggregator.GetEvent<PlayAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(currentTarget, ability));
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 5);
            adex.ExecuteAsyncDelegate();
        }

        private bool CanStopOption(object arg)
        {
            return CanPlayOption(arg);
        }

        private void StopOption(object state)
        {
            if(selectedOption is AnimatedAbility)
            {
                AnimatedAbility abilityToStop = state as AnimatedAbility;
                AnimatedAbility ability = selectedOption as AnimatedAbility;
                if (ability != null && abilityToStop != null && ability == abilityToStop)
                    StopAnimatedAbility(ability);
            }
            else
            {
                CharacterMovement characterMovement = selectedOption as CharacterMovement;
                if (characterMovement != null && characterMovement.Movement != null && characterMovement.IsActive)
                {
                    owner.ActiveMovement = null;
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(characterMovement);
                }
            }
        }

        private void StopAnimatedAbility(AnimatedAbility ability)
        {
            Character currentTarget = null;
            if (!ability.PlayOnTargeted)
            {
                this.SpawnAndTargetOwnerCharacter();
                currentTarget = this.Owner;
            }
            else
            {
                Roster.RosterExplorerViewModel rostExpVM = this.Container.Resolve<Roster.RosterExplorerViewModel>();
                currentTarget = rostExpVM.GetCurrentTarget() as Character;
                if (currentTarget == null)
                {
                    this.SpawnAndTargetOwnerCharacter();
                    currentTarget = this.Owner;
                }
            }
            this.Owner.ActiveAbility = null;
            //ability.Stop(Target: currentTarget);
            this.eventAggregator.GetEvent<StopAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(currentTarget, ability));
        }

        private void DeActivateAnimatedAbility(AnimatedAbility ability)
        {
            Character currentTarget = null;
            if (!ability.PlayOnTargeted)
            {
                this.SpawnAndTargetOwnerCharacter();
            }
            else
            {
                Roster.RosterExplorerViewModel rostExpVM = this.Container.Resolve<Roster.RosterExplorerViewModel>();
                currentTarget = rostExpVM.GetCurrentTarget() as Character;
                if (currentTarget == null)
                {
                    this.SpawnAndTargetOwnerCharacter();
                    currentTarget = this.Owner;
                }
            }
            owner.ActiveAbility = null;
            ability.DeActivate(Target: currentTarget);
        }

        private void TogglePlayOption(object obj)
        {
            if (SelectedOption != null && SelectedOption is AnimatedAbility && !(obj is AnimatedAbility))
            {
                StopOption(SelectedOption);
            }

            //SelectedOption = (T)obj;
            if (SelectedOption is AnimatedAbility)
            {
                AnimatedAbility ability = obj as AnimatedAbility;
                if (!ability.IsActive)
                {
                    PlayOption(obj);
                }
                else
                {
                    StopOption(obj);
                }
            }
            else if (SelectedOption is CharacterMovement)
            {
                CharacterMovement characterMovement = obj as CharacterMovement;
                if (!characterMovement.IsActive)
                {
                    PlayOption(obj);
                }
                else
                {
                    StopOption(obj);
                }
            }
        }
        private void AttackInitiated(object state)
        {
            this.UpdateCommands();
        }
        private void OnAttackExecutionFinished(object state)
        {
            this.Owner.ActiveAbility = null;
            this.UpdateCommands();
        }
        private void SpawnAndTargetOwnerCharacter()
        {
            if (!this.Owner.HasBeenSpawned)
            {
                Crowds.CrowdMemberModel member = this.Owner as Crowds.CrowdMemberModel;
                if (member.RosterCrowd == null)
                    this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<Crowds.CrowdMemberModel, Crowds.CrowdModel>(member, member.RosterCrowd as Crowds.CrowdModel));
                member.Spawn(false);
            }
            this.Owner.Target();
        }

        private void SetNonCombatMovement(object state)
        {
            CharacterMovement characterMovement = this.SelectedOption as CharacterMovement;
            if (state != null)
            {
                characterMovement.IsNonCombatMovement = true;
            }
            else
            {
                characterMovement.IsNonCombatMovement = false;
            }
            OnPropertyChanged("IsNonCombatMovementSelected");
            OnPropertyChanged("IsCombatMovementSelected");
        }
        private void AddIdentity(object state)
        {
            (optionGroup as OptionGroup<Identity>).Add(GetNewIdentity());
            this.eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Publish(null);
        }

        private void RemoveIdentity(object state)
        {
            if (SelectedOption == null)
                return;
            if (OptionGroup.Count == 1)
            {
                MessageBox.Show("Every character must have at least 1 Identity");
                return;
            }
            optionGroup.Remove(SelectedOption);
        }
        private void RemoveCharacterMovement(object state)
        {
            if (SelectedOption == null)
                return;
            CharacterMovement characterMovement = SelectedOption as CharacterMovement;
            if (this.Owner.DefaultMovement == characterMovement)
                this.Owner.DefaultMovement = null;
            optionGroup.Remove(SelectedOption);
        }
        private Identity GetNewIdentity()
        {
            return new Identity("Model_Statesman", IdentityType.Model, optionGroup.GetNewValidOptionName("Identity"));
        }

        private void AddAbility(object state)
        {
            Attack attack = GetNewAttackAbility();
            (optionGroup as OptionGroup<AnimatedAbility>).Add(attack);

            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            this.eventAggregator.GetEvent<AddOptionEvent>().Publish(attack);
        }

        private Attack GetNewAttackAbility()
        {
            return new Attack(optionGroup.GetNewValidOptionName("Ability"), owner: this.Owner);
        }

        private void AddCharacterMovement(object state)
        {
            CharacterMovement characterMovement = GetNewCharacterMovement();
            (optionGroup as OptionGroup<CharacterMovement>).Add(characterMovement);
            this.SelectedOption = (T)Convert.ChangeType(characterMovement, typeof(CharacterMovement));
            eventAggregator.GetEvent<EditMovementEvent>().Publish((characterMovement));
        }

        private CharacterMovement GetNewCharacterMovement()
        {
            return new CharacterMovement(optionGroup.GetNewValidOptionName("Movement"), this.Owner);
        }

        private void EditOption(object obj)
        {
            if (SelectedOption is Identity)
            {
                Identity identity = (Identity)Convert.ChangeType(SelectedOption, typeof(Identity));
                eventAggregator.GetEvent<EditIdentityEvent>().Publish(new Tuple<Identity, Character>(identity, Owner));
            }
            else if (SelectedOption is AnimatedAbility)
            {
                Attack attack = (Attack)Convert.ChangeType(SelectedOption, typeof(Attack));
                eventAggregator.GetEvent<EditAbilityEvent>().Publish(new Tuple<AnimatedAbility, Character>(attack, Owner));
            }
            else if (SelectedOption is CharacterMovement)
            {
                CharacterMovement characterMovement = (CharacterMovement)Convert.ChangeType(SelectedOption, typeof(CharacterMovement));
                eventAggregator.GetEvent<EditMovementEvent>().Publish((characterMovement));
            }
        }

        #region Rename Option Group

        private bool CanEnterEditMode(object arg)
        {
            return !this.IsStandardOptionGroup;
        }
        private void EnterEditMode(object state)
        {
            this.OriginalName = OptionGroup.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelEditMode(object state)
        {
            OptionGroup.Name = this.OriginalName;
            this.OriginalName = null;
            OnEditModeLeave(state, null);
        }

        private void RenameOptionGroup(object state)
        {
            if (this.NewOptionGroupAdded)
            {
                this.NewOptionGroupAdded = false;
                this.EnterEditMode(null);
            }
        }

        private void SubmitRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);
                bool duplicateName = CheckDuplicateName(updatedName);
                if (!duplicateName)
                {
                    RenameOptionGroup(updatedName);
                    OnEditModeLeave(state, null);
                    this.SaveOptionGroup();
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, Messages.DUPLICATE_NAME_CAPTION, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    this.CancelEditMode(state);
                }
            }
        }

        private void RenameOptionGroup(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            this.OptionGroup.Name = updatedName;
            this.Owner.OptionGroups.UpdateKey(this.OriginalName, updatedName);
            this.OriginalName = null;
        }

        private bool CheckDuplicateName(string updatedName)
        {
            return this.OriginalName != updatedName && this.Owner.OptionGroups.ContainsKey(updatedName);
        }

        #endregion

        #region ReOrder Options

        public void RemoveOption(int index)
        {
            (optionGroup as OptionGroup<T>).RemoveAt(index);
        }

        public void InsertOption(int index, ICharacterOption characterOption)
        {
            OptionGroup<T> group = (optionGroup as OptionGroup<T>);
            var existingIndex = group.IndexOf((T)characterOption);
            if (existingIndex >= 0)
            {
                group.RemoveAt(existingIndex);
                if (index > 0 && index >= group.Count)
                    index -= 1;
            }
            group.Insert(index, (T)characterOption);
        }

        #endregion

        #region Key event handling

        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (!this.IsReadOnlyMode && Keyboard.Modifiers == ModifierKeys.Control && Helper.GlobalVariables_CurrentActiveWindowName == Constants.CHARACTER_EDITOR)
            {
                if (inputKey == Key.I
                            && this.OptionGroup.Type == OptionType.Identity && this.AddOptionCommand.CanExecute(null))
                    this.AddIdentity(null);
                else if (inputKey == Key.M
                    && this.OptionGroup.Type == OptionType.CharacterMovement && this.AddOptionCommand.CanExecute(null))
                    this.AddCharacterMovement(null);
                else if (inputKey == Key.P
                    && this.OptionGroup.Type == OptionType.Ability && this.AddOptionCommand.CanExecute(null))
                    this.AddAbility(null);
                SaveUpdatedOptions();
            }
            else if (!this.IsReadOnlyMode && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                var optionToRemove = SelectedOption;
                if (inputKey == Key.I && this.OptionGroup.Type == OptionType.Identity && this.RemoveOptionCommand.CanExecute(null))
                    this.RemoveIdentity(null);
                else if (inputKey == Key.M && this.OptionGroup.Type == OptionType.CharacterMovement && this.RemoveOptionCommand.CanExecute(null))
                    this.RemoveCharacterMovement(null);
                else if (inputKey == Key.P && this.OptionGroup.Type == OptionType.Ability && this.RemoveOptionCommand.CanExecute(null))
                    optionGroup.Remove(SelectedOption);
                else if (inputKey == Key.X && this.OptionGroup.Type == OptionType.Mixed && this.RemoveOptionCommand.CanExecute(null))
                    optionGroup.Remove(SelectedOption);
                this.eventAggregator.GetEvent<RemoveOptionEvent>().Publish(optionToRemove);
                SaveUpdatedOptions();
            }
            return null;
        }

        #endregion

        #region Show Hide CharacterOption

        private void ShowHideCharacterOption(object state)
        {
            if (this.Owner.OptionGroupExpansionStates.ContainsKey(this.OptionGroup.Name))
                this.Owner.OptionGroupExpansionStates[this.OptionGroup.Name] = ShowOptions;
            else
                this.Owner.OptionGroupExpansionStates.Add(this.OptionGroup.Name, ShowOptions);
        }

        #endregion

        #endregion
    }
}
