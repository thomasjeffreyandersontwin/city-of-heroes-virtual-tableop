using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Microsoft.Xna.Framework;
using Framework.WPF.Extensions;
using Prism.Events;
using Module.Shared;
using Module.Shared.Enumerations;
using Module.Shared.Events;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Characters = Module.HeroVirtualTabletop.Characters;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using HeroVTT.Desktop;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.GameCommunicator;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Module.HeroVirtualTabletop.Library.Sevices;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.HCSIntegration;
using Module.Shared.Messages;
using System.Threading;

using Module.HeroVirtualTabletop.OptionGroups;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using HeroVTT.Desktop;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using Library = Module.HeroVirtualTabletop.Library;
using Roster = Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
namespace HeroVTT.Roster
{
    public class RosterExplorerViewModel : BaseViewModel
    {
        #region Private Fields

        private IMessageBoxService messageBoxService;
        private ITargetObserver targetObserver;
        private EventAggregator eventAggregator;
        private IDesktopKeyEventHandler desktopKeyEventHandler;
        private IHCSIntegrator hcsIntegrator;

        private readonly IRosterGameState _rosterGameState;
        private readonly Module.HeroVirtualTabletop.Library.GameCommunicator.IGameCommandExecutor _gameCommandExecutor;
        private readonly ActiveCharacterActivation _characterActivation;
        private readonly RosterSelectionOperations _selectionOps;
        private readonly CharacterActivator _characterActivator;
        private readonly CharacterPositioner _characterPositioner;
        private readonly AttackCoordinator _attackCoordinator;

        //refactor to attacking character
        private Attack currentAttack = null;
        private Guid currentAttackConfigKey = Guid.Empty;
        public List<Character> AttackingCharacters = new List<Character>();

        private bool isCharacterReset = false;
        public bool isMultiSelecting = false;

        private bool isCharacterDragDropInProgress = false;
        private Character currentDraggingCharacter = null;
        private DateTime lastDesktopMouseDownTime = DateTime.MinValue;
        private Vector3 lastMouseClickLocation = Vector3.Zero;

        public bool stopSyncingWithDesktop = false;

        private List<Character> targetCharactersForMove = new List<Character>();
        private List<Character> targetCharacters = new List<Character>();
        private List<Character> abortedCharacters = new List<Character>();
        private CombatantsCollection holdingCombatants = null;
        private List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksInProgress = new List<Tuple<Attack, List<Character>, List<Character>, Guid>>();
        private List<CrowdMemberModel> _oldSelection = new List<CrowdMemberModel>();
        private object[] savedAttackState = null;

        Character lastTargetedCharacter = null;
        private Character previousSelectedCharacter = null;
        public bool RosterMouseDoubleClicked = false;
        private bool moveAndAttackModeOn = false;
        public bool CharacterIsMoving
        {
            get
            {
                return _rosterGameState.ActiveCharacterMovement != null;
            }
        }

        private DesktopContextMenu desktopContextMenu;
        private DesktopMouseEventHandler mouseHandler;

        private System.Timers.Timer timer_RespondToDesktop = new System.Timers.Timer();

        #endregion

        public event EventHandler RosterMemberAdded;
        public void OnRosterMemberAdded(object sender, EventArgs e)
        {
            if (RosterMemberAdded != null)
                RosterMemberAdded(sender, e);
        }

        public event EventHandler SequenceUpdated;
        public void OnSequenceUpdated(object sender, EventArgs e)
        {
            if (SequenceUpdated != null)
                SequenceUpdated(sender, e);
        }

        public event EventHandler<CustomEventArgs<string>> ShowNotification;
        public void OnShowNotification(object sender, CustomEventArgs<string> e)
        {
            if (ShowNotification != null)
                ShowNotification(sender, e);
        }

        #region Public Properties

        public bool IsPlayingAttack { get; set; }
        public bool IsPlayingAreaEffect { get; set; }
        public bool IsPlayingAutoFire
        {
            get
            {
                return this.currentAttack != null && this.currentAttack.IsAutoFire;
            }
        }

        public IPopupService PopupService
        {
            get { return this.Container.Resolve<IPopupService>(); }
        }

        private HashedObservableCollection<ICrowdMemberModel, string> participants;
        public HashedObservableCollection<ICrowdMemberModel, string> Participants
        {
            get
            {
                if (participants == null)
                    participants = new HashedObservableCollection<ICrowdMemberModel, string>(x => x.Name, x => x.RosterCrowd.Order, x => x.RosterCrowd.Name, x => x.Name);
                return participants;
            }
            set
            {
                participants = value;
                OnPropertyChanged("Participants");
            }
        }

        private ObservableCollection<Combatant> sequenceParticipants;
        public ObservableCollection<Combatant> SequenceParticipants
        {
            get
            { 
                return sequenceParticipants;
            }
            set
            {
                sequenceParticipants = value;
                OnPropertyChanged("SequenceParticipants");
            }
        }

        private List<ICrowdMemberModel> oldSelections = new List<ICrowdMemberModel>();
        private IList selectedParticipants = new ArrayList();
        public IList SelectedParticipants
        {
            get
            {
                return selectedParticipants;
            }
            set
            {
                MemoryElement targetedBeforeMouseCLick = new MemoryElement();
                if(value != null && value.Count > 0 && value[0] is Combatant)
                {
                    List<ICrowdMemberModel> memberModelList = new List<ICrowdMemberModel>();
                    foreach (Combatant val in value)
                    {
                        memberModelList.Add(val.CombatantCharacter as ICrowdMemberModel);
                    }
                    selectedParticipants = memberModelList;
                }
                else
                    selectedParticipants = value;
                //synchSelectionWithGame();
                OnPropertyChanged("SelectedParticipants");
                OnPropertyChanged("ShowAttackContextMenu");
                OnPropertyChanged("IsGangModeActive");
                ActivateCharacterCommand.RaiseCanExecuteChanged();
                ActivateGangCommand.RaiseCanExecuteChanged();
                ActivateCrowdAsGangCommand.RaiseCanExecuteChanged();
                ResetDistanceCounterCommand.RaiseCanExecuteChanged();
                ToggleGangModeCommand.RaiseCanExecuteChanged();
                this.TargetOrFollow();
                this.RestartDistanceCounting();
                //this.TargetLastSelectedCharacter(oldSelections);
                oldSelections.Clear();
                foreach (var sp in selectedParticipants)
                    oldSelections.Add(sp as ICrowdMemberModel);
                Commands_RaiseCanExecuteChanged();
                MemoryElement target = new MemoryElement();
                if (target.Label != "" && targetedBeforeMouseCLick.Label != target.Label)
                {
                    if (targetedBeforeMouseCLick.Label != "")
                        previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == targetedBeforeMouseCLick.Label) as Character;
                    else
                        previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == target.Label) as Character;
                }
                if (IsHoldingCharacterSelected)
                {
                    this.ActivateHeldCharacter();
                }
            }
        }

        private ICrowdMemberModel activeCharacter;
        public ICrowdMemberModel ActiveCharacter
        {
            get
            {
                if (Participants.Any(p => (p as Character).IsGangLeader))
                    activeCharacter = this.Participants.First(p => (p as Character).IsGangLeader);
                else
                    activeCharacter = this.Participants.FirstOrDefault(p => (p as Character).IsActive);

                return activeCharacter;
            }
            //set
            //{
            //    activeCharacter = value;
            //    _rosterGameState.ActiveCharacter = value as Character;
            //    OnPropertyChanged("ActiveCharacter");
            //}
        }

        private bool isGangActive;
        public bool IsGangActive
        {
            get
            {
                return isGangActive;
            }
            set
            {
                isGangActive = value;
                OnPropertyChanged("IsGangActive");
            }
        }

        private bool isGangModeActive;
        public bool IsGangModeActive
        {
            get
            {
                if (SelectedParticipants != null && SelectedParticipants.Count > 0)
                {
                    var currentRosterCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd;
                    isGangModeActive = currentRosterCrowd != null ? currentRosterCrowd.IsGangMode : false;
                }
                else
                    isGangModeActive = false;
                return isGangModeActive;
            }
            set
            {
                isGangModeActive = value;
                if (SelectedParticipants != null && SelectedParticipants.Count > 0)
                {
                    //(SelectedParticipants[0] as CrowdMemberModel).RosterCrowd.IsGangMode = value;
                    foreach (CrowdMemberModel cm in SelectedParticipants)
                    {
                        cm.RosterCrowd.IsGangMode = value;
                    }
                }
                OnPropertyChanged("IsGangModeActive");
            }
        }

        private bool isCyclingCommandsThroughCrowd;
        public bool IsCyclingCommandsThroughCrowd
        {
            get
            {
                return isCyclingCommandsThroughCrowd;
            }
            set
            {
                isCyclingCommandsThroughCrowd = value;
                OnPropertyChanged("IsCyclingCommandsThroughCrowd");
            }
        }

        private bool targetOnHover;
        public bool TargetOnHover
        {
            get
            {
                return targetOnHover;
            }
            set
            {
                targetOnHover = value;
                OnPropertyChanged("TargetOnHover");
            }
        }

        private bool spawnOnClick;
        public bool SpawnOnClick
        {
            get
            {
                return spawnOnClick;
            }
            set
            {
                spawnOnClick = value;
                OnPropertyChanged("SpawnOnClick");
            }
        }

        private bool cloneAndSpawn;
        public bool CloneAndSpawn
        {
            get
            {
                return cloneAndSpawn;
            }
            set
            {
                cloneAndSpawn = value;
                OnPropertyChanged("CloneAndSpawn");
            }
        }

        private bool overheadMode;
        public bool OverheadMode
        {
            get
            {
                return overheadMode;
            }
            set
            {
                overheadMode = value;
                OnPropertyChanged("OverheadMode");
            }
        }

        private bool useRelativePositioning;
        public bool UseRelativePositioning
        {
            get
            {
                return useRelativePositioning;
            }
            set
            {
                useRelativePositioning = value;
                OnPropertyChanged("UseRelativePositioning");
            }
        }

        private Character currentDistanceCountingCharacter;
        public Character CurrentDistanceCountingCharacter
        {
            get
            {
                return currentDistanceCountingCharacter;
            }
            set
            {
                currentDistanceCountingCharacter = value;
                this.ResetDistanceCounterCommand.RaiseCanExecuteChanged();
                OnPropertyChanged("CurrentDistanceCountingCharacter");
            }
        }

        public bool IsOperatingCrowd
        {
            get
            {
                List<Character> characters = GetCharactersToOperateOn();
                Character lastSelectedMember = this.GetLastSelectedCharacter();
                if (!participants.Any(p => p.RosterCrowd == (lastSelectedMember as CrowdMemberModel).RosterCrowd && !characters.Contains(p as Character)))
                {
                    return true;
                }
                return false;
            }
                    
        }                

        public CrowdModel SelectedCrowd
        {
            get
            {
                List<Character> characters = GetCharactersToOperateOn();
                CrowdMemberModel last = this.GetLastSelectedCharacter() as CrowdMemberModel;
                return last.RosterCrowd as CrowdModel;
            }
        }

        public bool IsSingleSpawnedCharacterSelected
        {
            get
            {
                return this.SelectedParticipants != null && SelectedParticipants.Count == 1 && (SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned;
            }
        }

        public bool ShowAttackContextMenu
        {
            get
            {
                bool showAttackContextMenu = false;
                if (this.IsPlayingAttack && this.SelectedParticipants != null && this.SelectedParticipants.Count > 0)
                {
                    showAttackContextMenu = true;
                    foreach (var participant in this.SelectedParticipants)
                    {
                        if (!(participant as Character).HasBeenSpawned)
                        {
                            showAttackContextMenu = false;
                            break;
                        }
                    }
                }

                return showAttackContextMenu;
            }
        }
        private bool isSequenceViewActive;
        public bool IsSequenceViewActive
        {
            get
            {
                return isSequenceViewActive;
            }
            set
            {
                isSequenceViewActive = value;
                OnPropertyChanged("IsSequenceViewActive");
            }
        }

        private string currentPhase;
        public string CurrentPhase
        {
            get
            {
                return currentPhase;
            }
            set
            {
                currentPhase = value;
                OnPropertyChanged("CurrentPhase");
            }
        }

        public bool IsHoldingCharacterSelected
        {
            get
            {
                return this.SelectedParticipants.Count == 1 && this.holdingCombatants != null && this.holdingCombatants.Combatants != null && this.holdingCombatants.Combatants.Any(c => c.CharacterName == (this.SelectedParticipants[0] as ICrowdMemberModel).Name);
            }
        }

        public bool IsSweepAttackInProgress
        {
            get
            {
                return _rosterGameState.DefaultSweepAbility != null && _rosterGameState.DefaultSweepAbility.IsActive;
            }
        }

        public bool IsSpreadableAttackInProgress
        {
            get
            {
                return this.currentAttack != null && this.currentAttack.CanSpread;
            }
        }


        #endregion

        #region Commands

        public DelegateCommand<object> SpawnCommand { get; private set; }
        public DelegateCommand AbortActionCommand { get; private set; }
        public DelegateCommand<object> SavePositionCommand { get; private set; }
        public DelegateCommand<object> PlaceCommand { get; private set; }
        public DelegateCommand<object> ClearFromDesktopCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetedCommand { get; private set; }
        public DelegateCommand<object> TargetAndFollowCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCameraCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToCharacterCommand { get; private set; }
        public DelegateCommand<object> MoveTargetToMouseLocationCommand { get; private set; }
        public DelegateCommand<object> ToggleManeuverWithCameraCommand { get; private set; }
        public DelegateCommand<object> EditCharacterCommand { get; private set; }
        public DelegateCommand<object> ActivateCharacterCommand { get; private set; }
        public DelegateCommand<object> ResetCharacterStateCommand { get; private set; }
        public DelegateCommand<object> AttackTargetCommand { get; private set; }
        public DelegateCommand<object> AttackTargetAndExecuteCommand { get; private set; }
        public DelegateCommand<object> AttackExecuteSweepCommand { get; private set; }
        public DelegateCommand<object> ResetOrientationCommand { get; private set; }
        public DelegateCommand<object> CycleCommandsThroughCrowdCommand { get; private set; }
        public DelegateCommand<object> TargetHoveredCharacterCommand { get; private set; }
        public DelegateCommand<object> DropDraggedCharacterCommand { get; private set; }
        public DelegateCommand<object> ToggleTargetOnHoverCommand { get; private set; }
        public DelegateCommand<object> ToggleRelativePositioningCommand { get; private set; }
        public DelegateCommand ToggleGangModeCommand { get; private set; }
        public DelegateCommand ToggleSpawnOnClickCommand { get; private set; }
        public DelegateCommand ToggleCloneAndSpawnCommand { get; private set; }
        public DelegateCommand ToggleOverheadModeCommand { get; private set; }
        public DelegateCommand<object> TeleportTargetToCameraCommand { get; private set; }
        public DelegateCommand ActivateGangCommand { get; private set; }
        public DelegateCommand AttackTargetAndExecuteCrowdCommand { get; private set; }
        public DelegateCommand ActivateCrowdAsGangCommand { get; private set; }
        public DelegateCommand ResetDistanceCounterCommand { get; private set; }
        public DelegateCommand ScanAndFixMemoryCommand { get; private set; }
        public DelegateCommand ToggleSequenceViewCommand { get; private set; }

        #endregion

        #region Constructor

        public RosterExplorerViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, ITargetObserver targetObserver, 
            IDesktopKeyEventHandler keyEventHandler, IHCSIntegrator hcsIntegrator, EventAggregator eventAggregator,
            IRosterGameState rosterGameState = null, Module.HeroVirtualTabletop.Library.GameCommunicator.IGameCommandExecutor gameCommandExecutor = null)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.targetObserver = targetObserver;
            this.desktopKeyEventHandler = keyEventHandler;
            this.hcsIntegrator = hcsIntegrator;
            _rosterGameState = rosterGameState ?? new HelperBackedRosterGameState();
            _gameCommandExecutor = gameCommandExecutor ?? new GameCommandExecutorAdapter();
            _characterActivation = new ActiveCharacterActivation(_rosterGameState);
            _selectionOps = new RosterSelectionOperations();
            _characterActivator = new CharacterActivator(_rosterGameState, eventAggregator);
            _characterPositioner = new CharacterPositioner(eventAggregator);
            _attackCoordinator = new AttackCoordinator(_rosterGameState, eventAggregator);

            this.eventAggregator.GetEvent<AddToRosterEvent>().Subscribe(AddParticipants);
            this.eventAggregator.GetEvent<DeleteCrowdMemberEvent>().Subscribe(DeleteParticipant);
            this.eventAggregator.GetEvent<CheckRosterConsistencyEvent>().Subscribe(CheckRosterConsistency);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(InitiateRosterCharacterAttack);
            this.eventAggregator.GetEvent<ExecuteSweepAttackEvent>().Subscribe(ExecuteSweepAttack);
            this.eventAggregator.GetEvent<LaunchAttacksEvent>().Subscribe(this.LaunchAttacks);
            this.eventAggregator.GetEvent<CancelActiveAttackEvent>().Subscribe(this.CancelAttack);
            this.eventAggregator.GetEvent<CancelAttacksEvent>().Subscribe(this.CancelAttacks);
            this.eventAggregator.GetEvent<AddOptionEvent>().Subscribe(this.HandleCharacterOptionAddition);
            this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Subscribe(this.PlayMovement);
            this.eventAggregator.GetEvent<PlayMovementConfirmedEvent>().Subscribe(this.RestartDistanceCountingForHCSActivatedCharacter);
            this.eventAggregator.GetEvent<SpawnToRosterEvent>().Subscribe(this.SpawnToRoster);
            this.eventAggregator.GetEvent<StopMovementEvent>().Subscribe(this.OnMovementStopped);
            this.eventAggregator.GetEvent<AttackTargetsConfirmedEvent>().Subscribe(ConfigureAreaAttackWithConfirmedTargets);
            this.eventAggregator.GetEvent<AutoFireAttackShotsAssignedEvent>().Subscribe(ConfigureAutoFireAttack);

            timer_RespondToDesktop.AutoReset = false;
            timer_RespondToDesktop.Interval = 50;
            timer_RespondToDesktop.Elapsed += timer_RespondToDesktop_Elapsed;

            this.eventAggregator.GetEvent<ListenForTargetChanged>().Subscribe((obj) =>
            {
                this.targetObserver.TargetChanged -= TargetObserver_TargetChanged;
                this.targetObserver.TargetChanged += TargetObserver_TargetChanged;
            });
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Subscribe((obj) =>
            {
                this.targetObserver.TargetChanged -= TargetObserver_TargetChanged;
            });

            this.TargetOnHover = true;
            this.UseRelativePositioning = true;

            InitializeCommands();

            InitializeMouseHandlers();

            InitializeDesktopContextMenuHandlers();

            InitializeDesktopKeyHanders();

            InitializeHCSIntegrator();
        }

        #endregion

        #region Initialization

        private void InitializeMouseHandlers()
        {
            mouseHandler = new DesktopMouseEventHandler();
            mouseHandler.OnMouseLeftClick.Add(RespondToMouseClickBasedOnDesktopState);
            //mouseHandler.OnMouseLeftClickUp.Add(DropDraggedCharacter);
            mouseHandler.OnMouseRightClickUp.Add(DisplayCharacterPopupMenue);
            mouseHandler.OnMouseMove.Add(TargetHoveredCharacter);
            mouseHandler.OnMouseDoubleClick.Add(ActivateCharacterOrGang);
        }

        private void InitializeDesktopContextMenuHandlers()
        {
            desktopContextMenu = new DesktopContextMenu(_rosterGameState);
            desktopContextMenu.ActivateCharacterOptionMenuItemSelected += desktopContextMenu_ActivateCharacterOptionMenuItemSelected;
            desktopContextMenu.ActivateMenuItemSelected += desktopContextMenu_ActivateMenuItemSelected;
            desktopContextMenu.ActivateCrowdAsGangMenuItemSelected += desktopContextMenu_ActivateCrowdAsGangMenuItemSelected;
            desktopContextMenu.AttackContextMenuDisplayed += desktopContextMenu_AttackContextMenuDisplayed;
            desktopContextMenu.AttackTargetAndExecuteMenuItemSelected += desktopContextMenu_AttackTargetAndExecuteMenuItemSelected;
            desktopContextMenu.AttackTargetMenuItemSelected += desktopContextMenu_AttackTargetMenuItemSelected;
            desktopContextMenu.AttackTargetAndExecuteCrowdMenuItemSelected += desktopContextMenu_AttackTargetAndExecuteCrowdMenuItemSelected;
            desktopContextMenu.AttackExecuteSweepMenuItemSelected += desktopContextMenu_ExecuteSweepAttackMenuItemSelected;
            desktopContextMenu.AbortMenuItemSelected += desktopContextMenu_AbortMenuItemSelected;
            desktopContextMenu.ClearFromDesktopMenuItemSelected += desktopContextMenu_ClearFromDesktopMenuItemSelected;
            desktopContextMenu.CloneAndLinkMenuItemSelected += desktopContextMenu_CloneAndLinkMenuItemSelected;
            desktopContextMenu.DefaultContextMenuDisplayed += desktopContextMenu_DefaultContextMenuDisplayed;
            desktopContextMenu.ManueverWithCameraMenuItemSelected += desktopContextMenu_ManueverWithCameraMenuItemSelected;
            desktopContextMenu.MoveCameraToTargetMenuItemSelected += desktopContextMenu_MoveCameraToTargetMenuItemSelected;
            desktopContextMenu.MoveTargetToCameraMenuItemSelected += desktopContextMenu_MoveTargetToCameraMenuItemSelected;
            desktopContextMenu.MoveTargetToCharacterMenuItemSelected += desktopContextMenu_MoveTargetToCharacterMenuItemSelected;
            desktopContextMenu.PlaceMenuItemSelected += desktopContextMenu_PlaceMenuItemSelected;
            desktopContextMenu.ResetOrientationMenuItemSelected += desktopContextMenu_ResetOrientationMenuItemSelected;
            desktopContextMenu.SavePositionMenuItemSelected += desktopContextMenu_SavePositionMenuItemSelected;
            desktopContextMenu.SpawnMenuItemSelected += desktopContextMenu_SpawnMenuItemSelected;
            desktopContextMenu.SpreadNumberSelected += desktopContextMenu_SpreadNumberMenuItemSelected;
        }
        private void InitializeDesktopKeyHanders()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        private void InitializeHCSIntegrator()
        {
            hcsIntegrator.ActiveCharacterUpdated += this.HCSIntegrator_ActiveCharacterUpdated;
            hcsIntegrator.SequenceUpdated += this.HCSIntegrator_SequenceUpdated;
            hcsIntegrator.AttackResultsUpdated += this.HCSIntegrator_AttackResultsUpdated;
            hcsIntegrator.SweepAttackResultsUpdated += this.HCSIntegrator_SweepAttackResultsUpdated;
            hcsIntegrator.EligibleCombatantsUpdated += this.HCSIntegrator_EligibleCombatantsUpdated;
        }

        private void InitializeCommands()
        {
            this.SpawnCommand = new DelegateCommand<object>(delegate (object state) { this.Spawn(); });
            this.AbortActionCommand = new DelegateCommand(this.AbortAction, this.CanAbortAction);
            this.ClearFromDesktopCommand = new DelegateCommand<object>(delegate (object state) { this.ClearFromDesktop(); }, this.CanClearFromDesktop);
            this.ToggleTargetedCommand = new DelegateCommand<object>(delegate (object state) { this.ToggleTargeted(); }, this.CanToggleTargeted);
            this.SavePositionCommand = new DelegateCommand<object>(delegate (object state) { this.SavePosition(); }, this.CanSavePostion);
            this.PlaceCommand = new DelegateCommand<object>(delegate (object state) { this.Place(); }, this.CanPlace);
            this.TargetAndFollowCommand = new DelegateCommand<object>(this.TargetAndFollow, this.CanTargetAndFollow);
            this.MoveTargetToCameraCommand = new DelegateCommand<object>(delegate (object state) { this.MoveTargetToCamera(); }, this.CanMoveTargetToCamera);
            this.TeleportTargetToCameraCommand = new DelegateCommand<object>(this.TeleportTargetToCamera, this.CanTeleportTargetToCamera);
            this.MoveTargetToCharacterCommand = new DelegateCommand<object>(delegate (object state) { this.MoveTargetToCharacter(); }, this.CanMoveTargetToCharacter);
            this.MoveTargetToMouseLocationCommand = new DelegateCommand<object>(delegate (object state) { this.MoveTargetToMouseLocation(); }, this.CanMoveTargetToMouseLocation);
            this.ToggleManeuverWithCameraCommand = new DelegateCommand<object>(delegate (object state) { this.ToggleManeuverWithCamera(); }, this.CanToggleManeuverWithCamera);
            this.ToggleTargetOnHoverCommand = new DelegateCommand<object>(this.ToggleTargetOnHover);
            this.EditCharacterCommand = new DelegateCommand<object>(delegate (object state) { this.EditCharacter(); }, this.CanEditCharacter);
            this.ActivateCharacterCommand = new DelegateCommand<object>(delegate (object state) { this.ActivateCharacterOrGang(); }, this.CanActivateCharacterOrGang);
            this.ResetCharacterStateCommand = new DelegateCommand<object>(this.ResetCharacterState);
            this.AttackTargetCommand = new DelegateCommand<object>(this.TargetCharacterForAttack);
            this.AttackTargetAndExecuteCommand = new DelegateCommand<object>(this.TargetAndExecuteAttack);
            this.AttackTargetAndExecuteCrowdCommand = new DelegateCommand(this.TargetAndExecuteCrowd, this.CanTargetAndExecuteCrowd);
            this.AttackExecuteSweepCommand = new DelegateCommand<object>(this.ExecuteSweepAttack);
            this.ResetOrientationCommand = new DelegateCommand<object>(delegate (object state) { this.ResetOrientation(); }, this.CanResetOrientation);
            this.ResetDistanceCounterCommand = new DelegateCommand(this.ResetDistanceCounter, this.CanResetDistanceCounter);
            this.ToggleRelativePositioningCommand = new DelegateCommand<object>(this.ToggleRelativePositioning);
            this.ToggleGangModeCommand = new DelegateCommand(this.ToggleGangMode, this.CanToggleGangMode);
            this.ToggleSpawnOnClickCommand = new DelegateCommand(this.ToggleSpawnOnClick, this.CanToggleSpawnOnClick);
            this.ToggleCloneAndSpawnCommand = new DelegateCommand(this.ToggleCloneAndSpawn, this.CanToggleCloneAndSpawn);
            this.ToggleOverheadModeCommand = new DelegateCommand(this.ToggleOverheadMode, this.CanToggleOverheadMode);
            this.CycleCommandsThroughCrowdCommand = new DelegateCommand<object>(delegate (object state) { this.CycleCommandsThroughCrowd(); }, this.CanCycleCommandsThroughCrowd);
            this.ActivateGangCommand = new DelegateCommand(this.ToggleActivateGang, this.CanActivateGang);
            this.ActivateCrowdAsGangCommand = new DelegateCommand(this.ToggleActivateCrowdAsGang, this.CanActivateCrowdAsGang);
            this.ScanAndFixMemoryCommand = new DelegateCommand(this.ScanAndFixMemoryPointer);
            this.ToggleSequenceViewCommand = new DelegateCommand(this.ToggleSequenceView);
        }

        #endregion

        #region Commands Consistency
        private void Commands_RaiseCanExecuteChanged()
        {
            this.ClearFromDesktopCommand.RaiseCanExecuteChanged();
            this.ToggleTargetedCommand.RaiseCanExecuteChanged();
            this.SavePositionCommand.RaiseCanExecuteChanged();
            this.PlaceCommand.RaiseCanExecuteChanged();
            this.TargetAndFollowCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCharacterCommand.RaiseCanExecuteChanged();
            this.MoveTargetToMouseLocationCommand.RaiseCanExecuteChanged();
            this.MoveTargetToCameraCommand.RaiseCanExecuteChanged();
            this.ToggleManeuverWithCameraCommand.RaiseCanExecuteChanged();
            this.EditCharacterCommand.RaiseCanExecuteChanged();
            this.ActivateCharacterCommand.RaiseCanExecuteChanged();
            this.ResetOrientationCommand.RaiseCanExecuteChanged();
            this.ToggleSpawnOnClickCommand.RaiseCanExecuteChanged();
            this.ToggleCloneAndSpawnCommand.RaiseCanExecuteChanged();
            this.ToggleOverheadModeCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Get Characters to Operate on

        private List<Character> GetCharactersToOperateOn(bool considerGangMode = true)
        {
            return _selectionOps.GetCharactersToOperateOn(this.SelectedParticipants, this.Participants, considerGangMode);
        }

        #endregion

        #region Get Last Selected Character

        private Character GetLastSelectedCharacter()
        {
            return _selectionOps.GetLastSelectedCharacter(this.SelectedParticipants, this.Participants);
        }

        #endregion

        #region Add to Selections

        public void AddCrowdMembersToSelection(string crowdName)
        {
            foreach (Character c in this.Participants.Where(p => p.RosterCrowd != null && p.RosterCrowd.Name == crowdName))
            {
                this.SelectedParticipants.Add(c);
            }
        }

        #endregion

        #region Import Roster Member and Check Consistency
        public void RaiseEventToImportRosterMember()
        {
            this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(null);
        }
        private void CheckRosterConsistency(IEnumerable<CrowdMemberModel> members)
        {
            foreach (CrowdMemberModel member in members)
            {
                if (!Participants.Contains(member))
                {
                    InitializeAttackEventHandlers(member);
                    CheckIfCharacterExistsInGame(member);
                }
            }
            Action d = delegate ()
            {
                foreach (var m in members)
                {
                    if (!Participants.Contains(m))
                        Participants.Add(m);
                }
                this.eventAggregator.GetEvent<RosterSyncCompletedEvent>().Publish(null);
                Participants.Sort(ListSortDirection.Ascending, new RosterCrowdMemberModelComparer());
                OnRosterMemberAdded(null, null);
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Character Targeting and Syncing with Game

        public ICrowdMemberModel GetCurrentTarget()
        {
            return _selectionOps.GetCurrentTarget(this.Participants);
        }
        private void synchSelectionWithGame()
        {
            List<CrowdMemberModel> unselected = _oldSelection.Except(SelectedParticipants.Cast<CrowdMemberModel>()).ToList();
            unselected.ForEach(
                (member) =>
                {
                    if (!member.HasBeenSpawned)
                        return;
                    if (!(this.IsPlayingAttack && this.AttackingCharacters.Count > 0 && this.AttackingCharacters.Contains(member)))
                        member.Deactivate();
                    _oldSelection.Remove(member);
                });
            List<CrowdMemberModel> selected = SelectedParticipants.Cast<CrowdMemberModel>().Except(_oldSelection).ToList();
            if (selected.Count > 1)
                selected.ForEach(
                    (member) =>
                    {
                        if (!member.HasBeenSpawned)
                            return;
                        //if (!(this.IsPlayingAttack && this.AttackingCharacters.Count > 0 && this.AttackingCharacters.Contains(member))) // Don't make the attacking character blue
                        //    member.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                        _oldSelection.Add(member);
                    });
        }
        private void TargetObserver_TargetChanged(object sender, EventArgs e)
        {
            try
            {
                uint currentTargetPointer = targetObserver.CurrentTargetPointer;
                CrowdMemberModel currentTarget = (CrowdMemberModel)Participants.DefaultIfEmpty(null).Where(
                    (p) =>
                    {
                        Character c = p as Character;
                        return c != null && c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                    }).FirstOrDefault();
                if (currentTarget != null)
                    AddDesktopTargetToRosterSelection(currentTarget);
            }
            catch (TaskCanceledException ex)
            {

            }
        }
        public void AddDesktopTargetToRosterSelection(Character currentTarget)
        {
            Dispatcher.Invoke(() =>
            {
                if (SelectedParticipants == null)
                    SelectedParticipants = new ObservableCollection<object>() as IList;
                if (currentTarget == null)
                    return;
                if (Keyboard.Modifiers != ModifierKeys.Shift)
                {
                    if (SelectedParticipants != null && !this.stopSyncingWithDesktop)
                        SelectedParticipants.Clear();
                    //else if (stopSyncingWithDesktop)
                    //stopSyncingWithDesktop = false;
                }
                else
                {
                    //stopSyncingWithDesktop = true;
                }

                if (!SelectedParticipants.Contains(currentTarget))
                {
                    if (!stopSyncingWithDesktop)
                    {
                        SelectedParticipants.Add(currentTarget);
                        OnPropertyChanged("SelectedParticipants");
                    }
                    else
                    {
                        this.stopSyncingWithDesktop = false;
                    }
                }
                if (isMultiSelecting && stopSyncingWithDesktop)
                {
                    isMultiSelecting = false;
                    stopSyncingWithDesktop = false;
                }
            });
        }
        #endregion

        #region Target Hovered Character

        public Character GetHoveredCharacter()
        {
            MouseElement hoveredElement = new MouseElement();
            if (hoveredElement.HoveredInfo != "")
            {
                return (Character)this.Participants.FirstOrDefault(p => p.Name == hoveredElement.Name);
            }
            return null;
        }

        public Character GetHoveredOrTargetedCharacter(MemoryElement targetedBeforeMouseCLick)
        {
            Character hovered = GetHoveredCharacter();
            if (hovered != null)
            {
                return hovered;
            }
            else
            {
                System.Threading.Thread.Sleep(200);
                MemoryElement target = new MemoryElement();
                if (target.Label != "" && (targetedBeforeMouseCLick == null || targetedBeforeMouseCLick.Label != target.Label))
                {
                    return (CrowdMemberModel)GetCurrentTarget();
                }

            }
            return null;
        }

        public void TargetHoveredCharacter()
        {
            Character hoveredCharacter = GetHoveredCharacter();
            if (hoveredCharacter != null)
            {
                if (this.TargetOnHover)
                {
                    if (lastTargetedCharacter == null || hoveredCharacter.Label != lastTargetedCharacter.Label)
                    {
                        hoveredCharacter.Target();
                    }
                    lastTargetedCharacter = hoveredCharacter;
                }
                else if (this.CurrentDistanceCountingCharacter != null)
                {
                    this.CurrentDistanceCountingCharacter.UpdateDistanceCount(hoveredCharacter.CurrentPositionVector);
                }
            }
        }

        #endregion

        #region Drag Drop

        private void ContinueDraggingCharacter()
        {
            MemoryElement targetedBeforeMouseCLick = new MemoryElement();
            CrowdMemberModel hoveredCharacter = (CrowdMemberModel)GetHoveredCharacter();
            if (hoveredCharacter != null)
            {
                this.currentDraggingCharacter = hoveredCharacter;
                this.isCharacterDragDropInProgress = true;
                lastDesktopMouseDownTime = DateTime.UtcNow;
                this.lastTargetedCharacter = hoveredCharacter;
                if (targetedBeforeMouseCLick.Label != (hoveredCharacter as Character).Label)
                {
                    previousSelectedCharacter = this.Participants.FirstOrDefault(p => (p as Character).Label == targetedBeforeMouseCLick.Label) as Character;
                }
            }
            else
                this.lastTargetedCharacter = null;
            return;
        }
        private void DropDraggedCharacter()
        {
            if (currentDraggingCharacter != null && lastDesktopMouseDownTime != DateTime.MinValue && isCharacterDragDropInProgress)
            {
                System.Threading.Thread.Sleep(500);

                Vector3 mouseUpPosition = new MouseElement().Position;
                if (!IsPlayingAttack)
                {
                    if (!currentDraggingCharacter.HasBeenSpawned)
                    {
                        currentDraggingCharacter.Spawn();
                    }
                    if (Vector3.Distance(currentDraggingCharacter.CurrentPositionVector, mouseUpPosition) > 5)
                    {
                        currentDraggingCharacter.UnFollow();
                        if (this.IsGangActive && currentDraggingCharacter.IsActive)
                        {
                            Vector3 startingPosition = currentDraggingCharacter.CurrentPositionVector;
                            Vector3 nextReferenceVector = Vector3.Zero;
                            List<Vector3> usedUpPositions = new List<Vector3>();
                            foreach (Character c in this.Participants.Where(p => (p as Character).IsActive))
                            {

                                if (this.UseRelativePositioning)
                                    c.MoveToLocationWithRelativePositioning(mouseUpPosition, currentDraggingCharacter, startingPosition);
                                else
                                    c.MoveToLocationWithOptimalPositioning(mouseUpPosition, currentDraggingCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                            }
                        }
                        else if (this.IsGangModeActive && (currentDraggingCharacter as CrowdMemberModel).RosterCrowd.IsGangMode)
                        {
                            Vector3 startingPosition = currentDraggingCharacter.CurrentPositionVector;
                            Vector3 nextReferenceVector = Vector3.Zero;
                            List<Vector3> usedUpPositions = new List<Vector3>();
                            foreach (Character c in this.Participants.Where(p => p.RosterCrowd.IsGangMode))
                            {

                                if (this.UseRelativePositioning)
                                    c.MoveToLocationWithRelativePositioning(mouseUpPosition, currentDraggingCharacter, startingPosition);
                                else
                                    c.MoveToLocationWithOptimalPositioning(mouseUpPosition, currentDraggingCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                            }
                        }
                        else
                            currentDraggingCharacter.MoveToLocation(mouseUpPosition);
                    }
                }
            }
            currentDraggingCharacter = null;
            lastDesktopMouseDownTime = DateTime.MinValue;
            isCharacterDragDropInProgress = false;
        }

        #endregion

        #region Display Popup Menu
        int numRetryPopupMenu = 3;

        private void DisplayCharacterPopupMenue()
        {
            Action d = delegate ()
            {
                CrowdMemberModel character = (CrowdMemberModel)GetCurrentTarget();
                if (AttackingCharacters.Contains(character) && numRetryPopupMenu > 0)
                {
                    numRetryPopupMenu--;
                    DisplayCharacterPopupMenue();
                }
                else
                {
                    desktopContextMenu.GenerateAndDisplay(character, AttackingCharacters.Select(ac => ac.Name).ToList(), IsPlayingAttack, IsSequenceViewActive, IsSweepAttackInProgress, IsSpreadableAttackInProgress);
                    numRetryPopupMenu = 3;
                }
                if(this.CurrentDistanceCountingCharacter != null)
                {
                    Vector3 mousePosition = new MouseElement().Position;
                    this.CurrentDistanceCountingCharacter.UpdateDistanceCount(mousePosition);
                }
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 500);
            adex.ExecuteAsyncDelegate();
        }

        #endregion

        #region Move Character to Desktop Position Clicked

        public void MoveCharacterToDesktopPositionClicked()
        {
            Vector3 mouseDirection = new MouseElement().Position;
            Character activeMovementCharacter = _rosterGameState.ActiveCharacterMovement.Character;
            Character target = this.Participants.FirstOrDefault(p => p.Name == activeMovementCharacter.Name) as Character;
            if (this.IsGangActive && target.IsActive)
            {
                Character gangLeader = Participants.FirstOrDefault(p => (p as Character).IsGangLeader) as Character;
                Vector3 startingPosition = gangLeader.CurrentPositionVector;
                Vector3 nextReferenceVector = Vector3.Zero;
                List<Vector3> usedUpPositions = new List<Vector3>();
                foreach (Character c in this.Participants.Where(p => (p as Character).IsActive))
                {
                    if (this.UseRelativePositioning)
                        c.MoveToLocationWithRelativePositioning(mouseDirection, gangLeader, startingPosition);
                    else
                        c.MoveToLocationWithOptimalPositioning(mouseDirection, gangLeader, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                }
            }
            else if (this.IsGangModeActive && (target as CrowdMemberModel).RosterCrowd.IsGangMode)
            {
                Vector3 startingPosition = target.CurrentPositionVector;
                Vector3 nextReferenceVector = Vector3.Zero;
                List<Vector3> usedUpPositions = new List<Vector3>();
                foreach (Character c in this.Participants.Where(p => p.RosterCrowd.IsGangMode))
                {
                    if (this.UseRelativePositioning)
                        c.MoveToLocationWithRelativePositioning(mouseDirection, target, startingPosition);
                    else
                        c.MoveToLocationWithOptimalPositioning(mouseDirection, target, startingPosition, ref nextReferenceVector, ref usedUpPositions);
                }
            }
            else if (target != null)
                target.MoveToLocation(mouseDirection);
        }

        #endregion

        #region Respond to Desktop Click
        public void RespondToMouseClickBasedOnDesktopState()
        {
            currentTarget = this.SelectedParticipants != null && this.SelectedParticipants.Count > 0 ? this.SelectedParticipants[0] as Character : null;
            timer_RespondToDesktop.Start(); // We need this timer to avoid firing click events when desktop is no longer in focus, as click fires before focus lost. This also fixes double attack play bug.
        }
        void timer_RespondToDesktop_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer_RespondToDesktop.Stop();
            Action d = delegate ()
            {
                if (desktopContextMenu.IsDisplayed == false && mouseHandler.IsDesktopActive)
                {
                    if (IsPlayingAttack == false)
                    {
                        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
                        {
                            this.moveAndAttackModeOn = true;
                            Action d1 = delegate ()
                            {
                                this.PlayDefaultAbility();
                            };
                            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d1, 500);
                            adex.ExecuteAsyncDelegate();
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Control)
                        {
                            Action d1 = delegate ()
                            {
                                this.PlayDefaultAbility();
                            };
                            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d1, 500);
                            adex.ExecuteAsyncDelegate();
                        }
                        else if (Keyboard.Modifiers == ModifierKeys.Alt)
                        {
                            Action d1 = delegate ()
                            {
                                this.ActivateDefaultMovementToActivate();
                            };
                            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d1, 500);
                            adex.ExecuteAsyncDelegate();
                        }
                        else if (CharacterIsMoving == true)
                        {
                            MoveCharacterToDesktopPositionClicked();
                        }
                        else if (this.SpawnOnClick)
                        {
                            Vector3 mousePosition = new MouseElement().Position;
                            this.lastMouseClickLocation = mousePosition;
                            if (this.CloneAndSpawn)
                            {
                                InitiateCloneAndSpawn();
                            }
                            else
                                this.Spawn(mousePosition);
                        }
                        else
                            ContinueDraggingCharacter();
                    }
                    else
                    {
                        PlayAttackCycle();
                    }
                }
                else
                    desktopContextMenu.IsDisplayed = false;
            };
            Dispatcher.Invoke(d);
        }

        #endregion

        #region Add Participants
        private void AddParticipants(IEnumerable<CrowdMemberModel> crowdMembers)
        {
            foreach (var crowdMember in crowdMembers)
            {
                Participants.Add(crowdMember);
                InitializeAttackEventHandlers(crowdMember);
                //CheckIfCharacterExistsInGame(crowdMember);
            }
            // preserve selections
            List<Character> savedSelections = new List<Character>();
            if (SelectedParticipants.Count > 0)
            {
                foreach (Character c in SelectedParticipants)
                    savedSelections.Add(c);
            }
            Participants.Sort(ListSortDirection.Ascending, new RosterCrowdMemberModelComparer());
            if (savedSelections != null)
                savedSelections.ForEach(ss => SelectedParticipants.Add(ss));
            OnRosterMemberAdded(null, null);
            if (this.IsSequenceViewActive)
                this.RefreshSequenceView();
        }

        private void CheckIfCharacterExistsInGame(Character crowdMember)
        {
            this.eventAggregator.GetEvent<StopListeningForTargetChanged>().Publish(null);
            MemoryElement oldTargeted = new MemoryElement();
            crowdMember.Target();
            MemoryElement currentTargeted = new MemoryElement();
            if (currentTargeted.Label == crowdMember.Label)
            {
                crowdMember.SetAsSpawned();
                if (crowdMember.ActiveIdentity.Type == IdentityType.Model)
                {
                    if (crowdMember.GhostShadow == null)
                        crowdMember.CreateGhostShadow();
                    CheckIfCharacterExistsInGame(crowdMember.GhostShadow);
                    if (!crowdMember.GhostShadow.HasBeenSpawned)
                        crowdMember.SuperImposeGhost();
                }
            }
            try
            {
                oldTargeted.Target();
            }
            catch { }

            crowdMember.IsSyncedWithGame = true;
            this.eventAggregator.GetEvent<ListenForTargetChanged>().Publish(null);
        }
        #endregion

        #region Delete Participant
        private void DeleteParticipant(ICrowdMemberModel crowdMember)
        {
            if (this.SelectedParticipants == null)
                this.SelectedParticipants = new List<CrowdMemberModel>();
            this.SelectedParticipants.Clear();
            if (crowdMember is CrowdMemberModel)
            {
                this.SelectedParticipants.Add(crowdMember);
            }
            else if (crowdMember is CrowdModel)
            {
                var participants = this.Participants.Where(p => p.RosterCrowd.Name == crowdMember.Name);
                foreach (var participant in participants)
                    this.SelectedParticipants.Add(participant);
            }
            this.ClearFromDesktop();
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }

        #endregion

        #region Spawn

        public void Spawn(List<Character> charactersToOperateOn = null)
        {
            if (CloneAndSpawn)
            {
                InitiateCloneAndSpawn();
                return;
            }
            if (charactersToOperateOn == null)
                charactersToOperateOn = GetCharactersToOperateOn();
            _characterPositioner.SpawnCharacters(charactersToOperateOn);
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }

        public void Spawn(Vector3 locationVector, List<Character> charactersToOperateOn = null)
        {
            if (charactersToOperateOn == null)
                charactersToOperateOn = GetCharactersToOperateOn();
            _characterPositioner.SpawnCharactersAtLocation(charactersToOperateOn, locationVector);
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }

        private void InitiateCloneAndSpawn()
        {
            List<Character> characters = GetCharactersToOperateOn();
            object cloneParam = characters;
            if (characters.Count > 0)
            {
                CrowdMemberModel first = characters[0] as CrowdMemberModel;
                if (!participants.Any(p => p.RosterCrowd == first.RosterCrowd && !characters.Contains(p as Character)))
                {
                    // entire roster crowd needs to clone
                    cloneParam = first.RosterCrowd;
                }
                this.eventAggregator.GetEvent<CloneAndSpawnCrowdMemberEvent>().Publish(cloneParam);
            }
        }

        private void SpawnToRoster(IEnumerable<CrowdMemberModel> rosterMembers)
        {
            AddParticipants(rosterMembers);
            //this.SelectedParticipants.Clear();
            List<Character> charactersToSpawn = new List<Character>();
            foreach (var member in rosterMembers)
            {
                charactersToSpawn.Add(member);
                //this.SelectedParticipants.Add(member);
            }
            if (this.lastMouseClickLocation == Vector3.Zero)
                lastMouseClickLocation = new Camera().GetPositionVector();
            this.Spawn(this.lastMouseClickLocation, charactersToSpawn);
            lastMouseClickLocation = Vector3.Zero;
        }

        #endregion

        #region Clear from Desktop
        private bool CanClearFromDesktop(object state)
        {
            if (SelectedParticipants != null && SelectedParticipants.Count > 0 && !this.IsPlayingAttack)
            {
                return true;
            }
            return false;
        }
        public void ClearFromDesktop()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                member.ClearFromDesktop(false);
            }
            new KeyBindsGenerator().CompleteEvent();
            SelectNextCharacterInCrowdCycle();
            if (charactersToOperateOn.Any(c => c.IsActive))
            {
                this.DeactivateCharacter();
                this.DeactivateGang();
            }
            foreach (CrowdMemberModel participant in charactersToOperateOn)
            {
                Participants.Remove(participant);
                participant.RosterCrowd = null;
            }
            if (this.IsSequenceViewActive)
                this.RefreshSequenceView(); 
            Commands_RaiseCanExecuteChanged();
            eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
        #endregion

        #region Save Positon

        private bool CanSavePostion(object state)
        {
            bool canSavePosition = false;
            if (this.SelectedParticipants != null)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var character = c as Character;
                    if (character != null && character.HasBeenSpawned)
                    {
                        canSavePosition = true;
                        break;
                    }
                }
            }
            return canSavePosition;
        }
        public void SavePosition()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            _characterPositioner.SavePosition(charactersToOperateOn);
            SelectNextCharacterInCrowdCycle();
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            this.PlaceCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Place
        private bool CanPlace(object state)
        {
            bool canPlace = false;
            if (this.SelectedParticipants != null)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var crowdMemberModel = c as CrowdMemberModel;
                    if (crowdMemberModel != null && crowdMemberModel.RosterCrowd != null && crowdMemberModel.RosterCrowd.Name == Constants.ALL_CHARACTER_CROWD_NAME && crowdMemberModel.SavedPosition != null)
                    {
                        canPlace = true;
                        break;
                    }
                    else if (crowdMemberModel != null)
                    {
                        CrowdModel rosterCrowdModel = crowdMemberModel.RosterCrowd as CrowdModel;
                        if (rosterCrowdModel != null && rosterCrowdModel.SavedPositions.ContainsKey(crowdMemberModel.Name))
                        {
                            canPlace = true;
                            break;
                        }
                    }
                }
            }
            return canPlace;
        }
        public void Place()
        {
            var charactersToOperateOn = GetCharactersToOperateOn();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                //member.Place();

                if (!member.HasBeenSpawned)
                    member.Spawn(false);
            }
            var kbg = new KeyBindsGenerator();
            kbg.CompleteEvent();
            foreach (CrowdMember member in charactersToOperateOn)
            {
                member.Target();
                if (member.RosterCrowd != null)
                {
                    member.RosterCrowd.Place(member);
                }
                member.SuperImposeGhost();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }
        #endregion

        #region Abort

        private bool CanAbortAction()
        {
            bool canAbortAction = false;
            if (this.IsSequenceViewActive)
            {
                foreach (var c in this.SelectedParticipants)
                {
                    var character = c as Character;
                    if (character != null && character.HasBeenSpawned)
                    {
                        canAbortAction = true;
                        break;
                    }
                }
            }
            return canAbortAction;
        }
        private void AbortAction()
        {
            List<Character> charactersToOperateOn = this.GetCharactersToOperateOn(false);
            this.abortedCharacters = charactersToOperateOn;
            if (this.IsPlayingAttack)
            {
                this.savedAttackState = new object[] {this.currentAttack, new List<Character>(this.targetCharacters), new List<Character>(this.AttackingCharacters)};
                this.CancelAttack(this.currentAttack, this.targetCharacters, this.currentAttackConfigKey);
            }
            this.hcsIntegrator.AbortAction(charactersToOperateOn);
        }
        #endregion

        #region Toggle Targeted

        private bool CanToggleTargeted(object state)
        {
            bool canToggleTargeted = false;
            if (IsSingleSpawnedCharacterSelected)
            {
                canToggleTargeted = true;
            }
            return canToggleTargeted;
        }

        public void ToggleTargeted()
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                member.ToggleTargeted();
            }
            SelectNextCharacterInCrowdCycle();
        }


        #endregion

        #region Toggle Target on Hover

        private void ToggleTargetOnHover(object state)
        {
            this.TargetOnHover = !this.TargetOnHover;
        }

        #endregion

        #region Toggle Relative Positioning

        private void ToggleRelativePositioning(object state)
        {
            this.UseRelativePositioning = !this.UseRelativePositioning;
        }

        #endregion

        #region Toggle Gang Mode

        private bool CanToggleGangMode()
        {
            return this.SelectedParticipants != null && this.SelectedParticipants.Count > 0;
        }

        private void ToggleGangMode()
        {
            this.IsGangModeActive = !this.IsGangModeActive;
            if (this.IsGangModeActive && this.ActiveCharacter != null)
            {
                CrowdModel gangCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd as CrowdModel;
                if ((this.ActiveCharacter as CrowdMemberModel).RosterCrowd == gangCrowd)
                {
                    ToggleActivateCrowdAsGang();
                }
            }
        }

        #endregion

        #region Toggle Sequence View

        private void ToggleSequenceView()
        {
            this.IsSequenceViewActive = !IsSequenceViewActive;
            _rosterGameState.IntegrateWithHCS = IsSequenceViewActive;
            if (this.IsSequenceViewActive)
            {
                this.hcsIntegrator.StartIntegration();
                this.RefreshSequenceView();
            }
            else
            {
                if (this.abortedCharacters.Count > 0)
                {
                    abortedCharacters.ForEach(c => c.RefreshAbilitiesActivationEligibility());
                }
                this.hcsIntegrator.StopIntegration();
            }
        }

        #endregion

        #region Target And/Or Follow
        private bool CanTargetAndFollow(object state)
        {
            return SelectedParticipants.Count > 0 && (SelectedParticipants[0] as Character).HasBeenSpawned;
        }
        public void TargetAndFollow(object obj)
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            var member = charactersToOperateOn[0];
            if (!member.IsSyncedWithGame)
                CheckIfCharacterExistsInGame(member);
            member.TargetAndFollow();
            SelectNextCharacterInCrowdCycle();
        }

        public void TargetOrFollow()
        {
            if (SelectedParticipants != null && SelectedParticipants.Count == 1)
            {
                CrowdMemberModel member = SelectedParticipants[0] as CrowdMemberModel;
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                if (member.IsTargeted)
                {
                    if (this.IsPlayingAttack)
                        member.Target();
                    else if (this.isCharacterReset) // character has been selected to reset state, so just target
                    {
                        this.isCharacterReset = false; // reset this flag
                        member.Target();
                    }
                    //else
                    //    member.TargetAndFollow();
                }
                else
                    member.ToggleTargeted();

                //if (this.isPlayingAttack)
                //{
                //    if (member.Name != this.AttackingCharacter.Name && member.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && member.Name != Constants.DEFAULT_CHARACTER_NAME)
                //    {
                //        if (!isPlayingAreaEffect)
                //        { 
                //            if (PopupService.IsOpen("ActiveAttackView") == false)
                //            {
                //                if (this.targetCharacters.FirstOrDefault(tc => tc.Name == member.Name) == null)
                //                    this.targetCharacters.Add(member);
                //                ActiveAttackConfiguration attackConfig = new ActiveAttackConfiguration();
                //                attackConfig.AttackMode = AttackMode.Defend;
                //                attackConfig.AttackEffectOption = AttackEffectOption.None;
                //                member.ActiveAttackConfiguration = attackConfig;
                //                member.ActiveAttackConfiguration.IsCenterTarget = true;
                //                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<List<Character>, Attack>(this.targetCharacters, this.currentAttack));
                //            } 
                //        }
                //    }
                //}
            }
        }
        public void TargetAndFollow()
        {
            if (this.CanTargetAndFollow(null))
            {
                this.TargetAndFollow(null);
            }
        }

        public void TargetLastSelectedCharacter(IList oldSelectedParticipants)
        {
            ICrowdMemberModel lastSelected = null;
            if (oldSelectedParticipants != null && oldSelectedParticipants.Count < SelectedParticipants.Count)
            {
                foreach (var sp in SelectedParticipants)
                {
                    if (!oldSelectedParticipants.Contains(sp))
                    {
                        lastSelected = (ICrowdMemberModel)sp;
                        break;
                    }
                }
            }
            else
            {
                if (SelectedParticipants.Count > 0)
                    lastSelected = SelectedParticipants[0] as ICrowdMemberModel;
            }
            if (lastSelected != null)
                (lastSelected as Character).Target();
        }

        #endregion

        #region Move Target to Camera

        private bool CanMoveTargetToCamera(object arg)
        {
            bool canMoveTargetToCamera = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToCamera = false;
                return canMoveTargetToCamera;
            }
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                if (!member.HasBeenSpawned)
                {
                    canMoveTargetToCamera = false;
                    break;
                }
            }
            return canMoveTargetToCamera;
        }

        public void MoveTargetToCamera()
        {
            // Adjust destination based on relative position
            // need to choose a character that is "center" character. Should be the one that's closest to destination....
            float closestDistance;
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            Character closestCharacter = GetClosestCharacter(out closestDistance);
            Vector3 startingPosition = closestCharacter.CurrentPositionVector;
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (Character c in charactersToOperateOn)
            {
                if (this.UseRelativePositioning)
                    c.MoveToLocationWithRelativePositioning(new Camera().GetPositionVector(), closestCharacter, startingPosition);
                else
                    c.MoveToLocationWithOptimalPositioning(new Camera().GetPositionVector(), closestCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
            }

            SelectNextCharacterInCrowdCycle();
        }

        private Character GetClosestCharacter(out float distance)
        {
            var cameraPositionVector = new Camera().GetPositionVector();
            distance = Int32.MaxValue;
            Character closestCharacter = null;
            foreach (Character c in this.SelectedParticipants)
            {
                var distanceFromCamera = Vector3.Distance(c.CurrentPositionVector, cameraPositionVector);
                if (distanceFromCamera < distance)
                {
                    distance = distanceFromCamera;
                    closestCharacter = c;
                }
            }
            return closestCharacter;
        }

        #endregion

        #region Teleport Target to Camera

        private bool CanTeleportTargetToCamera(object state)
        {
            return CanMoveTargetToCamera(state);
        }

        private void TeleportTargetToCamera(object state)
        {
            float closestDistance;
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            Character closestCharacter = GetClosestCharacter(out closestDistance);
            Vector3 startingPosition = closestCharacter.CurrentPositionVector;
            Vector3 nextReferenceVector = Vector3.Zero;
            List<Vector3> usedUpPositions = new List<Vector3>();
            foreach (CrowdMemberModel member in charactersToOperateOn)
            {
                if (this.UseRelativePositioning)
                    member.TeleportToCameraWithRelativePositioning(closestCharacter, startingPosition);
                else
                    member.TeleportToCameraWithOptimalPositioning(closestCharacter, startingPosition, ref nextReferenceVector, ref usedUpPositions);
            }

            SelectNextCharacterInCrowdCycle();
        }

        #endregion

        #region Move Target to Character

        private bool CanMoveTargetToCharacter(object arg)
        {
            bool canMoveTargetToCharacter = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToCharacter = false;
            }
            else
            {
                foreach (CrowdMemberModel member in SelectedParticipants)
                {
                    if (!member.IsSyncedWithGame)
                        CheckIfCharacterExistsInGame(member);
                    if (!member.HasBeenSpawned)
                    {
                        canMoveTargetToCharacter = false;
                        break;
                    }
                }
            }

            return canMoveTargetToCharacter;
        }

        public void MoveTargetToCharacter()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (Character c in charactersToOperateOn)
                this.targetCharactersForMove.Add(c);
            targetObserver.TargetChanged -= RosterTargetUpdated;
            targetObserver.TargetChanged += RosterTargetUpdated;
        }

        #endregion

        #region Move Target to Mouse Location

        private bool CanMoveTargetToMouseLocation(object arg)
        {
            bool canMoveTargetToMouseLocation = true;
            if (this.SelectedParticipants == null)
            {
                canMoveTargetToMouseLocation = false;
            }
            else
            {
                foreach (CrowdMemberModel member in SelectedParticipants)
                {
                    if (!member.IsSyncedWithGame)
                        CheckIfCharacterExistsInGame(member);
                    if (!member.HasBeenSpawned)
                    {
                        canMoveTargetToMouseLocation = false;
                        break;
                    }
                }
            }
            return canMoveTargetToMouseLocation;
        }

        public void MoveTargetToMouseLocation()
        {
            //this.isMoveToMouseLocationEnabled = !this.isMoveToMouseLocationEnabled;
        }

        #endregion

        #region Reset Orientation

        private bool CanResetOrientation(object arg)
        {
            bool canResetOrientation = true;
            if (this.SelectedParticipants == null)
            {
                canResetOrientation = false;
                return canResetOrientation;
            }
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                if (!member.HasBeenSpawned)
                {
                    canResetOrientation = false;
                    break;
                }
            }
            return canResetOrientation;
        }

        public void ResetOrientation()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            _characterPositioner.ResetOrientation(charactersToOperateOn);
            SelectNextCharacterInCrowdCycle();
        }

        #endregion

        #region Activate Default Movement

        public void ActivateDefaultMovementToActivate()
        {
            Character character = ((Character)SelectedParticipants[0]);
            if (this.ActiveCharacter == null)
            {
                character = SelectedParticipants[0] as Character;
            }
            else
                character = this.ActiveCharacter as Character;

            Vector3 facing = new Vector3();
            if (SelectedParticipants.Count > 1)
            {
                facing = character.CurrentFacingVector;
            }

            if (character.ActiveMovement == null || character.ActiveMovement.IsActive == false)
            {
                //foreach (CrowdMemberModel member in SelectedParticipants)
                //{
                //    character = (Character)member;
                //    character.ActiveMovement = character.DefaultMovementToActivate;
                //    if (SelectedParticipants.Count > 1)
                //    {
                //        character.CurrentFacingVector = facing;
                //    }
                //    if (character.ActiveMovement != null)
                //    {
                //        if (!character.ActiveMovement.IsActive)
                //            this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(character.ActiveMovement);
                //    }
                //}
                character.PlayDefaultMovement = true;
                this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(character.DefaultMovementToActivate);
            }
            else
            {
                //foreach (CrowdMemberModel member in SelectedParticipants)
                //{
                //    character = (Character)member;
                //    character.ActiveMovement = character.DefaultMovementToActivate;
                //    if (character.ActiveMovement != null)
                //        this.eventAggregator.GetEvent<StopMovementEvent>().Publish(character.ActiveMovement);
                //}
                if (character.ActiveMovement != null)
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(character.ActiveMovement);
            }
        }


        #endregion

        #region Toggle Maneuver With Camera
        private bool CanToggleManeuverWithCamera(object arg)
        {
            bool canManeuverWithCamera = false;
            if (!IsPlayingAttack && this.SelectedParticipants != null && SelectedParticipants.Count == 1 && ((SelectedParticipants[0] as CrowdMemberModel).HasBeenSpawned || (SelectedParticipants[0] as CrowdMemberModel).ManeuveringWithCamera))
            {
                canManeuverWithCamera = true;
            }
            return canManeuverWithCamera;
        }
        private void ToggleManeuverWithCamera(object state)
        {
            foreach (CrowdMemberModel member in SelectedParticipants)
            {
                if (!member.IsSyncedWithGame)
                    CheckIfCharacterExistsInGame(member);
                member.ToggleManueveringWithCamera();
            }
            SelectNextCharacterInCrowdCycle();
            Commands_RaiseCanExecuteChanged();
        }


        public void ToggleManeuverWithCamera()
        {
            if (this.CanToggleManeuverWithCamera(null))
            {
                this.ToggleManeuverWithCamera(null);
            }
        }

        #endregion

        #region Cycle Commands Through Crowd

        private bool CanCycleCommandsThroughCrowd(object state)
        {
            return true;
        }
        private void CycleCommandsThroughCrowd()
        {
            this.IsCyclingCommandsThroughCrowd = !this.IsCyclingCommandsThroughCrowd;
        }
        private void SelectNextCharacterInCrowdCycle()
        {
            if (this.IsCyclingCommandsThroughCrowd)
            {
                this.stopSyncingWithDesktop = true;
                List<ICrowdMemberModel> cNext = new List<ICrowdMemberModel>();
                ICrowdMemberModel cCurrent = this.GetLastSelectedCharacter() as ICrowdMemberModel;
                if (!IsOperatingCrowd)
                {
                    var index = this.Participants.IndexOf(cCurrent as ICrowdMemberModel);

                    if (index + 1 == this.Participants.Count)
                    {
                        cNext.Add(this.Participants.FirstOrDefault() as ICrowdMemberModel);
                    }
                    else
                    {
                        cNext.Add(this.Participants[index + 1] as ICrowdMemberModel);
                    }
                }
                else
                {
                    CrowdModel nextCrowd = GetNextCrowd();
                    if (nextCrowd != null)
                    {
                        foreach (Character c in this.Participants.Where(p => p.RosterCrowd == nextCrowd))
                            cNext.Add(c as ICrowdMemberModel);
                    }
                }

                if (cNext.Count > 0 && !cNext.Any(c => c == cCurrent))
                {
                    SelectedParticipants.Clear();
                    foreach (var c in cNext)
                        SelectedParticipants.Add(c);
                    OnPropertyChanged("SelectedParticipants");
                }
            }
        }

        private CrowdModel GetNextCrowd()
        {
            var crowd = this.SelectedCrowd;
            var last = this.GetLastSelectedCharacter();
            int currIndex = Participants.IndexOf(last as CrowdMemberModel);
            if (crowd != null)
            {
                var nextChar = Participants.FirstOrDefault(p => p.RosterCrowd != crowd && Participants.IndexOf(p) > currIndex);
                if (nextChar != null)
                    return nextChar.RosterCrowd as CrowdModel;
                else
                {
                    var firstPart = Participants.First();
                    if (firstPart.RosterCrowd != crowd)
                        return firstPart.RosterCrowd as CrowdModel;
                }
            }
            return null;
        }

        #endregion

        #region Distance Counter

        private bool CanResetDistanceCounter()
        {
            return SelectedParticipants != null && SelectedParticipants.Count > 0 && this.CurrentDistanceCountingCharacter != null;
        }

        private void ResetDistanceCounter()
        {
            if (this.CurrentDistanceCountingCharacter != null)
            {
                this.CurrentDistanceCountingCharacter.CurrentDistanceCount = 0f;
                this.CurrentDistanceCountingCharacter.CurrentStartingPositionVectorForDistanceCounting = Vector3.Zero;
            }
        }

        private void RestartDistanceCounting()
        {
            if (SelectedParticipants != null)
            {
                if (this.IsPlayingAttack)
                {
                    if (this.AttackingCharacters.Count > 1 && this.IsGangActive)
                    {
                        this.CurrentDistanceCountingCharacter = Participants.First(p => (p as Character).IsGangLeader) as Character;
                    }
                    else
                    {
                        this.CurrentDistanceCountingCharacter = this.AttackingCharacters.First();
                    }
                    if (SelectedParticipants.Count > 0)
                    {
                        foreach (Character c in SelectedParticipants)
                        {
                            if (!AttackingCharacters.Contains(c))
                            {
                                this.CurrentDistanceCountingCharacter.UpdateDistanceCount(c.CurrentPositionVector);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    ResetDistanceCounter();
                    if (SelectedParticipants.Count > 1 && IsGangActive)
                        this.CurrentDistanceCountingCharacter = Participants.First(p => (p as Character).IsGangLeader) as Character;
                    else if (this.ActiveCharacter != null)
                        this.CurrentDistanceCountingCharacter = this.ActiveCharacter as Character;
                    else if (SelectedParticipants.Count > 0)
                        this.CurrentDistanceCountingCharacter = SelectedParticipants[0] as Character;
                    if (CurrentDistanceCountingCharacter != null)
                    {
                        if (!CurrentDistanceCountingCharacter.HasBeenSpawned)
                            this.CurrentDistanceCountingCharacter = null;
                        else
                        {
                            this.CurrentDistanceCountingCharacter.CurrentStartingPositionVectorForDistanceCounting = this.CurrentDistanceCountingCharacter.CurrentPositionVector;
                        }
                    }
                }
            }
        }

        private void IncrementDistanceCount(Character character)
        {
            if (IsPlayingAttack)
            {

            }
            else
            {

            }
        }

        #endregion

        #region Scan and Fix Memory Pointers

        private void ScanAndFixMemoryPointer()
        {
            var charactersToOperateOn = this.GetCharactersToOperateOn();
            foreach (Character c in charactersToOperateOn)
                c.ScanAndFixMemoryPointer();
        }

        #endregion

        #region Edit Character

        private bool CanEditCharacter(object state)
        {
            return this.SelectedParticipants != null && this.SelectedParticipants.Count == 1 && !this.IsPlayingAttack;
        }

        private void EditCharacter()
        {
            if (CanEditCharacter(null))
            {
                CrowdMemberModel c = this.SelectedParticipants[0] as CrowdMemberModel;
                this.eventAggregator.GetEvent<EditCharacterEvent>().Publish(new Tuple<ICrowdMemberModel, IEnumerable<ICrowdMemberModel>>(c, null));
            }
        }

        #endregion

        #region Spawn On Click

        private bool CanToggleSpawnOnClick()
        {
            return !this.IsPlayingAttack;
        }

        private void ToggleSpawnOnClick()
        {
            this.SpawnOnClick = !this.SpawnOnClick;
        }

        #endregion

        #region Clone and Spawn

        private bool CanToggleCloneAndSpawn()
        {
            return !this.IsPlayingAttack;
        }

        private void ToggleCloneAndSpawn()
        {
            this.CloneAndSpawn = !this.CloneAndSpawn;
        }

        #endregion

        #region Overhead Mode

        private bool CanToggleOverheadMode()
        {
            return true;
        }

        private void ToggleOverheadMode()
        {
            this.OverheadMode = !this.OverheadMode;

            if (this.OverheadMode)
                _gameCommandExecutor.ExecuteCmd("bindloadfile required_keybinds_alt.txt");
            else
                _gameCommandExecutor.ExecuteCmd("bindloadfile required_keybinds.txt");
        }

        #endregion

        #region Activate Character/Gang

        private bool CanActivateCharacterOrGang(object state)
        {
            //return CanToggleTargeted(state) && (SelectedParticipants[0] as Character).HasBeenSpawned ;
            return !this.IsPlayingAttack && SelectedParticipants.Count > 0;
        }

        private void ActivateCharacter()
        {
            if (CanActivateCharacterOrGang(null))
                ToggleActivateCharacter();
        }

        private void ActivateCharacterOrGang()
        {
            if (!IsPlayingAttack)
            {
                if (SelectedParticipants.Count > 1)
                {
                    ToggleActivateGang();
                }
                else if (IsGangModeActive)
                {
                    ToggleActivateCrowdAsGang();
                }
                else
                {
                    ActivateCharacter();
                }
            }
        }

        private void ActivateCharacterFromHCSActiveCharacterInfo(ActiveCharacterInfo activeCharacterInfo)
        {
            Action d = delegate ()
            {
                if (activeCharacterInfo != null)
                {
                    Character activeChar = this.Participants.FirstOrDefault(p => p.Name == activeCharacterInfo.Name) as Character;
                    if (activeChar != null)
                    {
                        if(this.ActiveCharacter != activeChar)
                            this.ToggleActivateCharacter(activeChar);
                        activeChar.RefreshAbilitiesActivationEligibility(activeCharacterInfo.AbilitiesEligibilityCollection);
                        this.RetrieveAttackInfoFromCombatSimulator();
                        this.RetrieveMovementDistanceLimitInfoFromCombatSimulator();
                        this.DisableHTHAttacksOutOfDistanceLimit();
                        if(activeCharacterInfo.CharacterStates != null 
                            && activeCharacterInfo.CharacterStates.IsAbortable.HasValue 
                            && activeCharacterInfo.CharacterStates.IsAbortable.Value
                            && !this.IsCharacterHolding(this.ActiveCharacter as Character)
                            && this.savedAttackState != null)
                        {
                            // NEED To RESUME ATTACK FROM HCSINTEGRATOR
                            Attack attack = savedAttackState[0] as Attack;
                            List<Character> targets = savedAttackState[1] as List<Character>;
                            List<Character> attackers = savedAttackState[2] as List<Character>;
                            this.InitiateRosterCharacterAttack(new Tuple<Character, Attack>(activeChar, attack));
                            if(targets != null && targets.Count > 0 && attackers.Contains(this.ActiveCharacter as Character))
                            {
                                this.SelectedParticipants = targets;
                                this.TargetAndExecuteAttack(this.currentAttack);
                                this.hcsIntegrator.ResumeAttack();
                            }
                            
                            this.savedAttackState = null;
                        }
                    }
                }
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 1000);
            adex.ExecuteAsyncDelegate();
        }

        public void ToggleActivateCharacter(Character character = null, string selectedOptionGroupName = null, string selectedOptionName = null)
        {
            Action action = delegate ()
            {
                if (character == null)
                    if (SelectedParticipants.Count == 0)
                        return;
                    else
                        character = SelectedParticipants[0] as CrowdMemberModel;
                if (character.IsActive)
                {
                    if (this.IsGangActive)
                        DeactivateGang();
                    else
                        DeactivateCharacter(character);
                }
                else
                {
                    if (this.IsGangActive)
                        DeactivateGang();
                    else if (this.ActiveCharacter != null)
                        DeactivateCharacter(this.ActiveCharacter as Character);
                    ActivateCharacter(character, selectedOptionGroupName, selectedOptionName);
                }

            };
            Application.Current.Dispatcher.Invoke(action);
        }

        private void ActivateCharacter(Character character, string selectedOptionGroupName = null, string selectedOptionName = null)
        {
            if (!character.HasBeenSpawned)
                character.Spawn();
            _characterActivation.PrepareMovementsForActivation(
                movement => this.eventAggregator.GetEvent<StopMovementEvent>().Publish(movement));
            _characterActivator.ActivateCharacter(character, this.Participants, selectedOptionGroupName, selectedOptionName);
            _characterActivation.ResumePausedMovementFor(character);
            SelectNextCharacterInCrowdCycle();
        }

        private void DeactivateCharacter(Character character = null)
        {
            {
                if (character == null)
                {
                    if (this.ActiveCharacter != null)
                        character = this.ActiveCharacter as Character;
                    else if (SelectedParticipants.Count == 0)
                        return;
                    else
                        character = SelectedParticipants[0] as Character;
                }
                if (character.IsActive)
                {
                    _characterActivation.RestoreFormerMovementOnDeactivate();
                    _characterActivator.DeactivateCharacter(character, this.Participants);
                    SelectNextCharacterInCrowdCycle();
                }

            };
        }

        private bool CanActivateGang()
        {
            return !IsPlayingAttack && SelectedParticipants.Count > 1;
        }

        private bool CanActivateCrowdAsGang()
        {
            return !IsPlayingAttack && SelectedParticipants != null && SelectedParticipants.Count == 1;
        }

        private void ToggleActivateGang()
        {
            if (!CanActivateGang())
                return;
            if (IsGangActive)
            {
                bool needToActivateAnotherGang = true;
                foreach (Character p in SelectedParticipants)
                {
                    if (p.IsActive)
                        needToActivateAnotherGang = false;
                }
                DeactivateGang();
                if (needToActivateAnotherGang)
                    ActivateSelectedCharactersAsGang();
            }
            else
            {
                if (this.ActiveCharacter != null)
                    DeactivateCharacter(this.ActiveCharacter as Character);
                ActivateSelectedCharactersAsGang();
            }

        }

        private void ActivateSelectedCharactersAsGang()
        {
            List<Character> gangMembers = new List<Character>();
            foreach (var c in this.SelectedParticipants)
            {
                Character character = c as Character;
                gangMembers.Add(character);
            }
            ActivateGang(gangMembers);
        }
        private void ToggleActivateCrowdAsGang()
        {
            if (!CanActivateCrowdAsGang())
                return;
            if (IsGangActive)
            {
                bool needToActivateAnotherGang = true;
                var targetedCharacter = GetCurrentTarget();
                foreach (Character p in Participants.Where(p => p.RosterCrowd == targetedCharacter.RosterCrowd))
                {
                    if (p.IsActive)
                        needToActivateAnotherGang = false;
                }
                DeactivateGang();
                if (needToActivateAnotherGang)
                    ActivateCrowdAsGang();
            }
            else
            {
                if (this.ActiveCharacter != null)
                    DeactivateCharacter(this.ActiveCharacter as Character);
                ActivateCrowdAsGang();
            }

        }

        private void ActivateCrowdAsGang()
        {
            var targetedCharacter = GetCurrentTarget();
            if (SelectedParticipants.Contains(targetedCharacter))
            {
                List<Character> gangMembers = new List<Character>();
                foreach (Character c in Participants.Where(p => p.RosterCrowd == targetedCharacter.RosterCrowd))
                {
                    gangMembers.Add(c);
                }
                ActivateGang(gangMembers);
            }
        }

        private void ActivateGang(List<Character> gangMembers)
        {
            Character targetedCharacter = GetCurrentTarget() as Character;
            _characterActivator.ActivateGang(gangMembers, targetedCharacter);
            this.IsGangActive = true;
        }

        private void DeactivateGang()
        {
            _characterActivator.DeactivateGang(this.Participants);
            this.IsGangActive = false;
        }

        private void InitiateGangMovementHandlers(Character gangLeader)
        {
            foreach (CharacterMovement cm in gangLeader.Movements)
            {
                cm.MovementActivatedForGangLeader -= this.GangLeader_OnMovementActivated;
                cm.MovementActivatedForGangLeader += this.GangLeader_OnMovementActivated;

                cm.MovementDeactivatedForGangLeader -= this.GangLeader_OnMovementDeactivated;
                cm.MovementDeactivatedForGangLeader += this.GangLeader_OnMovementDeactivated;
            }
        }

        private void DisableGangMovementHandlers(Character gangLeader)
        {
            foreach (CharacterMovement cm in gangLeader.Movements)
            {
                cm.MovementActivatedForGangLeader -= this.GangLeader_OnMovementActivated;

                cm.MovementDeactivatedForGangLeader -= this.GangLeader_OnMovementDeactivated;
            }
        }

        private void GangLeader_OnMovementActivated(object sender, CustomEventArgs<CharacterMovement> e)
        {
            if (this.IsGangActive)
            {
                CharacterMovement cm = e.Value;
                foreach (Character c in this.Participants.Where(p => (p as Character).IsActive && !(p as Character).IsGangLeader))
                {
                    cm.ActivateMovement(c);
                }
            }
        }

        private void GangLeader_OnMovementDeactivated(object sender, CustomEventArgs<CharacterMovement> e)
        {
            if (this.IsGangActive)
            {
                CharacterMovement cm = e.Value;
                foreach (Character c in this.Participants.Where(p => (p as Character).IsActive && !(p as Character).IsGangLeader))
                {
                    cm.DeactivateMovement(c);
                }
            }
        }

        #endregion

        #region Play Default Ability

        public void PlayDefaultAbility()
        {
            Action d = delegate ()
            {
                if (!IsPlayingAttack && SelectedParticipants != null && (SelectedParticipants.Count == 1 || (SelectedParticipants.Count > 1 && IsGangModeActive)))
                {
                    Character abilityPlayingCharacter = null;

                    if (this.ActiveCharacter == null)
                    {
                        abilityPlayingCharacter = SelectedParticipants[0] as Character;
                        //if(!abilityPlayingCharacter.IsActive)
                        //    ToggleActivateCrowdAsGang();
                    }
                    else
                        abilityPlayingCharacter = this.ActiveCharacter as Character;

                    RosterMouseDoubleClicked = false;

                    if (abilityPlayingCharacter != null)
                    {
                        var ability = abilityPlayingCharacter.DefaultAbilityToActivate;
                        if (ability != null)
                        {
                            if (IsGangModeActive)
                            {
                                foreach (Character character in Participants.Where(p => p.RosterCrowd == (abilityPlayingCharacter as CrowdMemberModel).RosterCrowd))
                                {
                                    //ability.Play(Target: character);
                                    this.eventAggregator.GetEvent<PlayAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(character, ability));
                                }
                            }
                            else
                            {
                                ////abilityPlayingCharacter.Target(false);
                                ////abilityPlayingCharacter.ActiveIdentity.RenderWithoutAnimation(target: abilityPlayingCharacter);
                                //ability.Play();
                                this.eventAggregator.GetEvent<PlayAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(null, ability));
                            }
                        }
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(d);
        }

        #endregion

        #region Play Movement

        private void PlayMovement(CharacterMovement characterMovement)
        {
            List<Character> movementTargets = new List<Character>();
            if (this.IsGangModeActive)
            {
                var gangCrowd = this.Participants.Where(p => p.RosterCrowd.IsGangMode);
                if (gangCrowd.Contains(characterMovement.Character as ICrowdMemberModel))
                {
                    foreach (Character c in gangCrowd.Where(gm => gm.RosterCrowd == (characterMovement.Character as CrowdMemberModel).RosterCrowd))
                    {
                        movementTargets.Add(c);
                    }
                }
                else
                    movementTargets.Add(characterMovement.Character);
            }
            else if (this.IsGangActive)
            {
                var gangLeader = this.Participants.FirstOrDefault(p => (p as Character).IsGangLeader);
                if (characterMovement.Character == gangLeader)
                {
                    foreach (Character c in this.Participants.Where(p => (p as Character).IsActive))
                        movementTargets.Add(c);
                }
                else
                    movementTargets.Add(characterMovement.Character);
            }
            else
                movementTargets.Add(characterMovement.Character);

            if (Participants.Any(p => (p as Character).Movements.Any(m => m.IsActive) && !movementTargets.Contains(p as Character)))
            {
                foreach (CharacterMovement cm in Participants.Where(p => (p as Character).Movements.Any(m => m.IsActive)
                 && !movementTargets.Contains(p as Character)).SelectMany(p => (p as Character).Movements.Where(m => m.IsActive)))
                {
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(cm);
                }
            }

            this.eventAggregator.GetEvent<PlayMovementConfirmedEvent>().Publish(new Tuple<CharacterMovement, List<Character>>(characterMovement, movementTargets));
        }

        private void RestartDistanceCountingForHCSActivatedCharacter(Tuple<CharacterMovement, List<Character>> tuple)
        {
            if(this.IsSequenceViewActive)
            {
                this.RestartDistanceCounting();
                if (this.CurrentDistanceCountingCharacter != null && tuple.Item1.DistanceLimit == float.MinValue)
                    this.CurrentDistanceCountingCharacter.CurrentDistanceLimit = hcsIntegrator.GetMovementDistanceLimit(tuple.Item1);
            }
        }

        #endregion

        #region Attacks
        int numRetryHover = 3;
        Character currentTarget = null;
        bool dontFireAttack = false;

        private void PlayAttackCycle()
        {
            CrowdMemberModel character = (CrowdMemberModel)GetHoveredCharacter();
            Vector3 mousePosition = new MouseElement().Position;
            this.CurrentDistanceCountingCharacter.UpdateDistanceCount(mousePosition);
            if (character == null && numRetryHover > 0)
            {
                numRetryHover--;
                Action d = delegate ()
                {
                    PlayAttackCycle();
                };
                AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 20);
                adex.ExecuteAsyncDelegate();
            }
            else if (this.currentAttack != null && this.AttackingCharacters.Count > 0)
            {
                numRetryHover = 3;
                Character latestTarget = GetCurrentTarget() as Character;
                if (character == null && latestTarget != currentTarget)
                {
                    currentTarget = null;
                    character = latestTarget as CrowdMemberModel;
                }
                if (!dontFireAttack && (this.AttackingCharacters.Contains(character) || character == null))
                {
                    if(this.IsSequenceViewActive && this.currentAttack.AttackInfo != null && this.currentAttack.AttackInfo.AttackType == AttackType.Area)
                    {
                        this.ConfigureAreaAttackThroughCombatSimulator(mousePosition);
                    }
                    else
                    {
                        AttackDirection direction = new AttackDirection(mousePosition);
                        if (!this.currentAttack.IsExecutionInProgressFor(this.currentAttackConfigKey))
                            this.currentAttack.AnimateAttack(direction, AttackingCharacters);
                    }
                    
                }
                else
                {
                    Action d = delegate ()
                    {
                        if (!dontFireAttack && this.IsSequenceViewActive && this.currentAttack.AttackInfo != null && this.currentAttack.AttackInfo.AttackType == AttackType.Area)
                        {
                            this.ConfigureAreaAttackThroughCombatSimulator(character);
                        }
                        else
                        {
                            if (Keyboard.Modifiers == ModifierKeys.Shift)
                            {
                                this.SelectedParticipants.Clear();
                                this.SelectedParticipants = new List<Character>();
                                this.SelectedParticipants.Add(character);
                                this.TargetCharacterForAttack(null);
                            }
                            else if (!dontFireAttack)
                            {
                                this.SelectedParticipants.Clear();
                                this.SelectedParticipants = new List<Character>();
                                this.SelectedParticipants.Add(character);
                                this.TargetAndExecuteAttack(null);
                            }
                        }
                    };
                    Dispatcher.Invoke(d);
                }
            }

        }

        private void InitializeAttackEventHandlers(Attack attack)
        {
            attack.AttackInitiated -= this.Ability_AttackInitiated;
            attack.AttackCompleted -= this.Ability_AttackCompleted;
            attack.AttackInitiated += this.Ability_AttackInitiated;
            attack.AttackCompleted += this.Ability_AttackCompleted;
        }

        private void InitializeAttackEventHandlers(Character character)
        {
            foreach (var ability in character.AnimatedAbilities)
            {
                var attack = ability as Attack;
                InitializeAttackEventHandlers(attack);
            }
        }

        private void Ability_AttackInitiated(object sender, EventArgs e)
        {
            Character targetCharacter = sender as Character;
            CustomEventArgs<Attack> customEventArgs = e as CustomEventArgs<Attack>;
            if (targetCharacter != null && customEventArgs != null)
            {
                _rosterGameState.IsPlayingAttack = true;
                // Change mouse pointer to bulls eye
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Dispatcher.Invoke(() => { Mouse.OverrideCursor = cursor; });
                // Inform Roster to update attacker
                this.eventAggregator.GetEvent<AttackInitiatedEvent>().Publish(new Tuple<Character, Attack>(targetCharacter, customEventArgs.Value));
            }
        }

        private void Ability_AttackCompleted(object sender, CustomEventArgs<Tuple<Attack, List<Character>, Guid>> e)
        {
            Attack attack = e.Value.Item1;
            List<Character> defenders = e.Value.Item2;
            Guid configKey = e.Value.Item3;
            attack.SetExecutionCompleteFor(configKey);
            this.ResetAttack(attack, defenders, configKey);
        }

        private void InitiateRosterCharacterAttack(Tuple<Character, Attack> attackInitiatedEventTuple)
        {
            this.targetCharacters = new List<Character>();
            Character attackingCharacter = attackInitiatedEventTuple.Item1;
            Attack attack = attackInitiatedEventTuple.Item2;
            this.currentAttackConfigKey = Guid.NewGuid();
            CrowdMemberModel rosterCharacter = this.Participants.FirstOrDefault(p => p.Name == attackingCharacter.Name) as CrowdMemberModel;
            if (rosterCharacter != null && attack != null)
            {
                this.IsPlayingAttack = true;
                if (attack.IsAreaEffect)
                {
                    this.IsPlayingAreaEffect = true;
                }
                Commands_RaiseCanExecuteChanged();
                OnPropertyChanged("ShowAttackContextMenu");
                targetObserver.TargetChanged += RosterTargetUpdated;
                this.currentAttack = attack;
                this.RetrieveAttackInfoFromCombatSimulator(this.currentAttack);
                Character attacker = this.AttackingCharacters.FirstOrDefault(ac => ac == this.currentAttack.Owner || ac.ActiveAbility == this.currentAttack);
                if (this.IsSweepAttackInProgress && this.attacksInProgress.Count == 0 && attacker != null)
                {
                    if(this.currentAttack.IsHandToHand)
                    {
                        List<Attack> handToHandAttacks = attacker.AnimatedAbilities.Where(a => a.IsAttack && (a as Attack).IsHandToHand).Cast<Attack>().ToList();
                        attacker.DisableAttacks(handToHandAttacks);
                    }
                    else
                    {
                        List<Attack> rangedAttacks = attacker.AnimatedAbilities.Where(a => a.IsAttack && (a as Attack).IsRanged).Cast<Attack>().ToList();
                        attacker.DisableAttacks(rangedAttacks);
                    }
                }
                //this.AttackingCharacter = attackingCharacter;
                this.AttackingCharacters.Clear();
                if (attackingCharacter.IsGangLeader)
                {
                    foreach (Character c in this.Participants.Where(p => (p as Character).IsActive && (p as Character).HasBeenSpawned))
                    {
                        this.AttackingCharacters.Add(c);
                        c.AddAttackConfiguration(this.currentAttack, new AttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None }, this.currentAttackConfigKey);
                    }
                }
                else if (this.IsGangModeActive && (attackingCharacter as CrowdMemberModel).RosterCrowd.IsGangMode)
                {
                    foreach (Character c in this.Participants.Where(p => p.RosterCrowd == (attackingCharacter as CrowdMemberModel).RosterCrowd && (p as Character).HasBeenSpawned))
                    {
                        this.AttackingCharacters.Add(c);
                        c.AddAttackConfiguration(this.currentAttack, new AttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None }, this.currentAttackConfigKey);
                    }
                }
                else
                {
                    this.AttackingCharacters.Add(attackingCharacter);
                    // Update character properties - icons in roster should show
                    rosterCharacter.AddAttackConfiguration(this.currentAttack, new AttackConfiguration { AttackMode = AttackMode.Attack, AttackEffectOption = AttackEffectOption.None }, this.currentAttackConfigKey);
                }

                this.RestartDistanceCounting();

                Dispatcher.Invoke(() => {
                    Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                    Mouse.OverrideCursor = cursor;
                });
            }
        }

        private void RetrieveAttackInfoFromCombatSimulator(Attack attack)
        {
            if(this.IsSequenceViewActive && attack.AttackInfo == null)
                attack.AttackInfo = this.hcsIntegrator.GetAttackInfo(attack.Name);
        }

        private void RetrieveAttackInfoFromCombatSimulator()
        {
            if (this.IsSequenceViewActive && this.ActiveCharacter != null)
            {
                foreach(var ability in (this.ActiveCharacter as Character).AnimatedAbilities.Where(a => a.IsAttack && (a as Attack).AttackInfo == null))
                {
                    Attack attack = ability as Attack;
                    attack.AttackInfo = this.hcsIntegrator.GetAttackInfo(attack.Name);
                }
            }
                
        }

        private void RetrieveMovementDistanceLimitInfoFromCombatSimulator()
        {
            if(this.IsSequenceViewActive && this.ActiveCharacter != null)
            {
                foreach(var characterMovement in (this.ActiveCharacter as Character).Movements.Where(m => m.DistanceLimit == float.MinValue))
                {
                    characterMovement.DistanceLimit = this.hcsIntegrator.GetMovementDistanceLimit(characterMovement);
                }
            }
        }

        private void DisableHTHAttacksOutOfDistanceLimit()
        {
            if (this.ActiveCharacter != null)
            {
                Character activeCharacter = this.ActiveCharacter as Character;
                bool isWithinRange = false;
                foreach (Character spawnedParticipant in this.Participants.Where(p => p != activeCharacter && (p as Character).HasBeenSpawned))
                {
                    float distance = Vector3.Distance(activeCharacter.CurrentPositionVector, spawnedParticipant.CurrentPositionVector) / 8f;
                    if (distance <= activeCharacter.MaxDistanceLimit / 2)
                    {
                        isWithinRange = true;
                        break;


                        if (!isWithinRange)
                        {
                            List<Attack> rangedAttacks = activeCharacter.AnimatedAbilities.Where(a => a.IsAttack && (a as Attack).IsRanged).Cast<Attack>().ToList();
                            activeCharacter.DisableAttacks(rangedAttacks);
                        }
                        else
                        {
                            List<Attack> hthAttacks = activeCharacter.AnimatedAbilities.Where(a => a.IsAttack && (a as Attack).IsHandToHand).Cast<Attack>().ToList();
                            hthAttacks.ForEach(a => a.IsEnabled = true);
                        }
                    }
                }
            }
        }
            

        private void RosterTargetUpdated(object sender, EventArgs e)
        {
            uint currentTargetPointer = targetObserver.CurrentTargetPointer;
            CrowdMemberModel currentTarget = (CrowdMemberModel)Participants.DefaultIfEmpty(null).Where(
                (p) =>
                {
                    Character c = p as Character;
                    return c.gamePlayer != null && c.gamePlayer.Pointer == currentTargetPointer;
                }).FirstOrDefault();
            Action action = delegate ()
            {
                if (currentTarget == null) // Cancel attack
                {
                    this.CancelAttack();
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }
        private void ConfirmAttacks()
        {
            if (IsPlayingAttack)
                this.eventAggregator.GetEvent<ConfirmAttacksEvent>().Publish(null);
            else if(IsSweepAttackInProgress && this.PopupService.IsOpen("AttackConfigurationView"))
                this.eventAggregator.GetEvent<ConfirmAttacksEvent>().Publish(null);
        }

        private void HandleCharacterOptionAddition(ICharacterOption option)
        {
            if (option is Attack)
                this.InitializeAttackEventHandlers(option as Attack);
        }

        private void LaunchAttacks(List<Tuple<Attack, List<Character>, Guid>> configuredAttacks)
        {
            targetObserver.TargetChanged -= RosterTargetUpdated;
            List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksToExecute = new List<Tuple<Attack, List<Character>, List<Character>, Guid>>();
            foreach (var tuple in configuredAttacks)
            {
                Guid configKey = tuple.Item3;
                Attack attack = tuple.Item1;
                List<Character> attackingCharacters = this.AttackingCharacters.ToList();
                List<Character> defendingCharacters = tuple.Item2;
                
                foreach (var defender in defendingCharacters)
                {
                    ////** Commenting out following as we need the fxs to be persisted across attacks
                    //defender.Deactivate(); // restore original costume
                }
                
                if (!attack.IsAreaEffect)
                {
                    foreach (var defender in defendingCharacters)
                    {
                        defender.AttackConfigurationMap[configKey].Item2.IsCenterTarget = !defender.AttackConfigurationMap[configKey].Item2.IsSecondaryTarget;
                    }
                }
                attacksToExecute.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(attack, attackingCharacters, defendingCharacters, configKey));
            }
            if (this.IsSequenceViewActive)
                this.hcsIntegrator.ConfirmAttack();
            ExecuteAttacks(attacksToExecute);
        }

        System.Threading.Timer attackSynchronizationTimer;
        public void ExecuteAttacks(List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksToExecute)
        {
            attackSynchronizationTimer = new System.Threading.Timer(attackSynchronizationTimer_Callback, attacksToExecute, Timeout.Infinite, Timeout.Infinite);
            attackSynchronizationTimer.Change(5, Timeout.Infinite);
        }

        private void attackSynchronizationTimer_Callback(object state)
        {
            List<Tuple<Attack, List<Character>, List<Character>, Guid>> attacksToExecute = state as List<Tuple<Attack, List<Character>, List<Character>, Guid>>;
            if (!attacksToExecute.Any(t => t.Item1.IsExecutionInProgressFor(t.Item4) && !t.Item1.IsExecutionCompleteFor(t.Item4)))
            {
                Tuple<Attack, List<Character>, List<Character>, Guid> attackTuple = attacksToExecute.Where(t => !t.Item1.IsExecutionInProgressFor(t.Item4) && !t.Item1.IsExecutionCompleteFor(t.Item4)).FirstOrDefault();
                if (attackTuple != null)
                {
                    Guid configKey = attackTuple.Item4;
                    Attack attack = attackTuple.Item1;
                    List<Character> attackers = attackTuple.Item2;
                    List<Character> defenders = attackTuple.Item3;
                    attack.AnimateAttackSequence(AttackingCharacters, defenders, configKey);
                }
            }

            if (attacksToExecute.All(t => t.Item1.IsExecutionCompleteFor(t.Item4)))
            {
                attackSynchronizationTimer.Change(Timeout.Infinite, Timeout.Infinite);
                this.eventAggregator.GetEvent<ResetSweepEvent>().Publish(null);
            }
            else
                attackSynchronizationTimer.Change(500, Timeout.Infinite);
        }

        private void CancelAttacks(List<Tuple<Attack, List<Character>, Guid>> attacksWithDefenders = null)
        {
            Action action = delegate ()
            {
                if (this.IsPlayingAttack || this.IsSweepAttackInProgress)
                {
                    _rosterGameState.IsPlayingAttack = false;
                    Commands_RaiseCanExecuteChanged();
                    if (this.IsSequenceViewActive)
                        this.hcsIntegrator.CancelAttack();
                    if(attacksWithDefenders == null)
                    {
                        foreach (var tuple in this.attacksInProgress.ToList())
                        {
                            ResetAttack(tuple.Item1, tuple.Item3, tuple.Item4, true);
                        }
                    }
                    else
                    {
                        foreach (var tuple in attacksWithDefenders)
                        {
                            ResetAttack(tuple.Item1, tuple.Item2, tuple.Item3, true);
                        }
                    }
                    if(this.IsSweepAttackInProgress)
                        this.eventAggregator.GetEvent<ResetSweepEvent>().Publish(null);
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }
        private void CancelAttack()
        {
            if(this.currentAttack != null)
            {
                this.CancelAttack(this.currentAttack, this.targetCharacters, this.currentAttackConfigKey);
            }
            else if (this.IsSweepAttackInProgress)
            {
                this.CancelAttacks();
            }
        }
        private void CancelAttack(Tuple<Attack, List<Character>, Guid> attackToCancel)
        {
            CancelAttack(attackToCancel.Item1, attackToCancel.Item2, attackToCancel.Item3);
        }
        private void CancelAttack(Attack attack, List<Character> defendingCharacters, Guid configKey)
        {
            if (this.IsSequenceViewActive && !IsSweepAttackInProgress)
                this.hcsIntegrator.CancelAttack();
            this.ResetAttack(attack, defendingCharacters, configKey, true);
        }

        private void CancelCurrentAttack()
        {
            this.IsPlayingAttack = false;
            this.IsPlayingAreaEffect = false;
            _rosterGameState.IsPlayingAttack = false;
            Mouse.OverrideCursor = Cursors.Arrow;
            if(this.currentAttack != null)
                this.currentAttack.Stop();
            this.currentAttack = null;
            this.currentAttackConfigKey = Guid.Empty;
            //this.moveAndAttackModeOn = false;
            this.RestartDistanceCounting();
            this.Commands_RaiseCanExecuteChanged();
            this.eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
            this.eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Publish(null);
            this.eventAggregator.GetEvent<CloseAutoFireAttackShotSelectionWidgetEvent>().Publish(null);
        }

        private void ResetAllAttacks()
        {
            Action d = delegate ()
            {
                this.IsPlayingAttack = false;
                this.IsPlayingAreaEffect = false;
                this.attacksInProgress.Clear();
                _rosterGameState.IsPlayingAttack = false;
                Mouse.OverrideCursor = Cursors.Arrow;
                this.currentAttack = null;
                this.currentAttackConfigKey = Guid.Empty;
                this.moveAndAttackModeOn = false;
                this.RestartDistanceCounting();
                this.Commands_RaiseCanExecuteChanged();
                this.eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
                this.eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Publish(null);
                this.eventAggregator.GetEvent<CloseAutoFireAttackShotSelectionWidgetEvent>().Publish(null);
            };
            Dispatcher.Invoke(d);
        }

        private void ResetAttack(Attack attack, List<Character> defenders, Guid configKey, bool killEffects = false)
        {
            targetObserver.TargetChanged -= RosterTargetUpdated;
            //this.currentAttack.AnimationElements.ToList().ForEach((x) => { if (!x.Persistent) x.Stop(); });
            attack.Stop(useMemoryTargeting: true);

            Action d = delegate ()
            {
                // Hide attack icon from attacking character
                if (this.AttackingCharacters.Count > 0)
                    this.AttackingCharacters.ForEach(ac =>
                    {
                        if(killEffects)
                            ac.RemoveAttackConfiguration(configKey);
                        else
                            ac.AttackConfigurationMap[configKey].Item2.AttackMode = AttackMode.None;
                        ac.RefreshAttackConfigurationParameters();
                        ac.ScanAndFixMemoryPointer();
                    });
                
                foreach (var defender in defenders)
                {
                    if(killEffects)
                        defender.RemoveAttackConfiguration(configKey);
                    else if(defender.AttackConfigurationMap.Any(m => m.Key == configKey))
                        defender.AttackConfigurationMap[configKey].Item2.AttackMode = AttackMode.None;
                    defender.RefreshAttackConfigurationParameters();
                    defender.ScanAndFixMemoryPointer();
                }
                var tuple = this.attacksInProgress.FirstOrDefault(a => a.Item1 == attack);
                if (tuple != null)
                    this.attacksInProgress.Remove(tuple);
                if (this.attacksInProgress.Count == 0)
                    this.ResetAllAttacks();
                else
                    CancelCurrentAttack();
            };
            Application.Current.Dispatcher.Invoke(d);
        }

        private void ResetCharacterState(object state)
        {
            if (state != null && this.Participants != null)
            {
                string charName = state.ToString();
                Character defendingCharacter = this.Participants.FirstOrDefault(p => p.Name == charName) as Character;
                if (defendingCharacter != null && defendingCharacter.AttackConfigurationMap != null)
                {
                    // If he is just stunned make him normal
                    if (defendingCharacter.AttackConfigurationMap.Any(ac => ac.Value.Item2.AttackEffectOption == AttackEffectOption.Stunned))
                    {
                        KeyBindsGenerator keyBindsGenerator = new KeyBindsGenerator();

                        if (defendingCharacter.ActiveIdentity.Type == IdentityType.Model && defendingCharacter.GhostShadow != null && defendingCharacter.GhostShadow.HasBeenSpawned)
                        {
                            defendingCharacter.GhostShadow.Target(!defendingCharacter.IsInViewForTargeting);
                            keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        }
                        defendingCharacter.Target(!defendingCharacter.IsInViewForTargeting);
                        keyBindsGenerator.GenerateKeyBindsForEvent(GameEvent.Move, "none");
                        keyBindsGenerator.CompleteEvent();
                    }
                    // Else make him stand up 
                    else if (_rosterGameState.DefaultAbilities != null && _rosterGameState.DefaultAbilities.Count > 0)
                    {
                        var globalStandUpAbility = _rosterGameState.DefaultAbilities.FirstOrDefault(a => a.Name == Constants.STANDUP_ABILITY_NAME);
                        if (globalStandUpAbility != null && globalStandUpAbility.AnimationElements != null && globalStandUpAbility.AnimationElements.Count > 0)
                        {
                            globalStandUpAbility.Play(false, defendingCharacter, false, !defendingCharacter.IsInViewForTargeting);
                        }
                    }
                    // Update icons in Roster
                    //defendingCharacter.AttackConfigurationsMap = new AttackConfiguration { AttackEffectOption = AttackEffectOption.None, AttackMode = AttackMode.None, AttackResults = null };
                    defendingCharacter.AttackConfigurationMap.Clear();
                    defendingCharacter.RefreshAttackConfigurationParameters();
                    this.isCharacterReset = true;
                }
            }
        }

        private void ConfigureAreaAttackThroughCombatSimulator(object attackCenter)
        {
            if(this.currentAttack.AttackInfo != null)
            {
                this.AttackingCharacters.ForEach(ac => ac.AttackConfigurationMap[this.currentAttackConfigKey].Item2.AttackCenterPosition = (attackCenter is Character) ? (attackCenter as Character).CurrentPositionVector : (Vector3)attackCenter);
                List<Character> targets = this.currentAttack.CalculateAreaAttackTargets(this.AttackingCharacters.First(), this.Participants.Where(p => (p as Character).HasBeenSpawned && !this.AttackingCharacters.Contains(p as Character)).Cast<Character>().ToList(), this.currentAttackConfigKey);
                if(targets.Count > 0)
                {
                    foreach (Character target in targets)
                    {
                        AddToAttackTarget(target);
                        if (target.AttackConfigurationMap.ContainsKey(this.currentAttackConfigKey))
                            target.AttackConfigurationMap[this.currentAttackConfigKey].Item2.IsCenterTarget = target == attackCenter;
                    }

                    if (this.currentAttack != null && !this.attacksInProgress.Any(ap => ap.Item4 == this.currentAttackConfigKey))
                    {
                        this.attacksInProgress.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(this.currentAttack, this.AttackingCharacters.ToList(), this.targetCharacters.ToList(), this.currentAttackConfigKey));
                    }
                    if (this.currentAttack.AttackInfo.TargetSelective)
                        this.eventAggregator.GetEvent<LoadAttackTargetsSelectionWidgetEvent>().Publish(targets);
                    else
                    {
                        if (this.IsSweepAttackInProgress)
                        {
                            this.CancelCurrentAttack();
                        }
                        else
                        {
                            ConfigureAttackThroughCombatSimulator();
                        }
                    }
                }
            }
        }
        private void ConfigureAutoFireAttack()
        {
            if (this.IsGangModeActive)
            {
                this.DistributeAutoFireShotsAmongGangMembers();
                this.eventAggregator.GetEvent<AutoFireAttackShotsAssignedEvent>().Publish(this.targetCharacters.ToList());
            }
            else
            {
                if(this.targetCharacters.Count > 0)
                    this.eventAggregator.GetEvent<LoadAutoFireAttackShotSelectionWidgetEvent>().Publish(new Tuple<AnimatedAbilities.Attack, List<Characters.Character>, Guid>(this.currentAttack, this.targetCharacters, this.currentAttackConfigKey));
            }
        }
        private void ConfigureAutoFireAttack(List<Character> targets)
        {
            this.targetCharacters = targets;
            this.ConfigureAttack();
        }
        private void ConfigureAreaAttackWithConfirmedTargets(List<Character> confirmedTargets)
        {
            this.targetCharacters = confirmedTargets;
            this.ConfigureAttackThroughCombatSimulator();
        }

        private void DistributeAutoFireShotsAmongGangMembers()
        {
            int maxShots = this.currentAttack.AttackInfo.AutoFireMaxShots;
            _attackCoordinator.DistributeAutoFireShots(this.targetCharacters, maxShots);
        }

        public void TargetCharacterForAttack(object state)
        {
            dontFireAttack = true;
            AddAttackTargets();
            if (this.IsPlayingAutoFire)
            {
                if(this.targetCharacters.Count == this.currentAttack.AttackInfo.AutoFireMaxShots)
                {
                    this.TargetAndExecuteAttack(null);
                }
            }
            Action d = delegate ()
            {
                dontFireAttack = false;
            };
            AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 500);
            adex.ExecuteAsyncDelegate();
        }

        public void TargetAndExecuteAttack(object state)
        {
            AddAttackTargets();
            if(this.targetCharacters.Count > 0 && !this.attacksInProgress.Any(t => t.Item4 == this.currentAttackConfigKey))
                this.attacksInProgress.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(this.currentAttack, this.AttackingCharacters.ToList(), this.targetCharacters.ToList(), this.currentAttackConfigKey));
            if(this.targetCharacters.Count > 1 && this.currentAttack.CanSpread && this.currentAttack.SpreadDistance == 0d)
            {
                var targetPositions = this.targetCharacters.Select(tc => tc.CurrentPositionVector).ToArray();
                double maxDist = Helper.CalculateMaximumDistanceBetweenTwoPointsInASetOfPoints(targetPositions);
                this.currentAttack.SpreadDistance = maxDist;
            }
            if(targetCharacters.Count > 0 && this.IsSequenceViewActive && !(this.currentAttack.CanSpread || this.currentAttack.IsAutoFire || this.currentAttack.IsAreaEffect))
            {
                // split targets into sweep attack
            }
            if (this.IsPlayingAutoFire)
            {
                ConfigureAutoFireAttack();
            }
            else
            {
                if(this.IsSweepAttackInProgress)
                {
                    if (this.currentAttack != null)
                    {
                        this.CancelCurrentAttack();
                    }
                }
                else
                    ConfigureAttack();
            }
        }

        public bool CanTargetAndExecuteCrowd()
        {
            return SelectedParticipants != null && SelectedParticipants.Count > 0 && (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd != null;
        }

        public void TargetAndExecuteCrowd()
        {
            CrowdModel selectedCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd as CrowdModel;
            AddToAttackTarget(selectedCrowd);
            if (this.targetCharacters.Count > 0 && !this.attacksInProgress.Any(t => t.Item4 == this.currentAttackConfigKey))
                this.attacksInProgress.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(this.currentAttack, this.AttackingCharacters.ToList(), this.targetCharacters.ToList(), this.currentAttackConfigKey));
            if (this.IsSweepAttackInProgress)
            {
                TargetAndExecuteAttack(null);
            }
            else if (this.IsPlayingAutoFire)
            {
                ConfigureAutoFireAttack();
            }
            else
                ConfigureAttack();
        }

        private void TargetAndExecuteSweepAttack(object state)
        {
            if (this.IsPlayingAttack)
            {
                AddAttackTargets();
                this.attacksInProgress.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(this.currentAttack, this.AttackingCharacters.ToList(), this.targetCharacters.ToList(), this.currentAttackConfigKey));
            }
            this.eventAggregator.GetEvent<ExecuteSweepAttackEvent>().Publish(null);
        }

        private void ExecuteSweepAttack(object state)
        {
            ConfigureAttack();
        }

        private void AddAttackTargets()
        {
            if (IsGangModeActive)
            {
                CrowdModel selectedCrowd = (SelectedParticipants[0] as CrowdMemberModel).RosterCrowd as CrowdModel;
                AddToAttackTarget(selectedCrowd);
            }
            else
            {
                foreach (Character participant in this.SelectedParticipants)
                {
                    AddToAttackTarget(participant);
                }
            }
        }

        private void AddToAttackTarget(CrowdModel crowd)
        {
            foreach (Character c in Participants.Where(p => p.RosterCrowd == crowd && (p as Character).HasBeenSpawned))
            {
                AddToAttackTarget(c);
            }
        }

        private void AddToAttackTarget(Character character)
        {
            if (!this.AttackingCharacters.Contains(character) && character.Name != Constants.COMBAT_EFFECTS_CHARACTER_NAME && character.Name != Constants.DEFAULT_CHARACTER_NAME)
            {
                bool canAddCharacterToTargets = true;
                if (this.currentAttack.IsHandToHand)
                {
                    if (!this.AttackingCharacters.All(ac => Vector3.Distance(ac.CurrentPositionVector, character.CurrentPositionVector) / 8 < ac.MaxDistanceLimit / 2))
                    {
                        canAddCharacterToTargets = false;
                    }
                }
                if (canAddCharacterToTargets)
                {
                    if (this.targetCharacters.FirstOrDefault(tc => tc.Name == character.Name) == null)
                    {
                        this.targetCharacters.Add(character);
                    }
                    //currentTarget.ChangeCostumeColor(new Framework.WPF.Extensions.ColorExtensions.RGB() { R = 0, G = 51, B = 255 });
                    AttackConfiguration attackConfig = new AttackConfiguration();
                    attackConfig.AttackMode = AttackMode.Defend;
                    attackConfig.AttackEffectOption = AttackEffectOption.None;
                    if (this.moveAndAttackModeOn)
                        attackConfig.MoveAttackerToTarget = true;
                    if (this.IsSequenceViewActive && this.currentAttack.IsHandToHand)
                    {
                        if (this.AttackingCharacters.Any(ac => Vector3.Distance(ac.CurrentPositionVector, character.CurrentPositionVector) / 8 > 1))
                        {
                            if(this.currentAttack.AttackInfo != null && this.currentAttack.AttackInfo.AttackType == AttackType.Area)
                            {
                                
                            }
                            else
                                attackConfig.MoveAttackerToTarget = true;
                        }
                    }
                    character.AddAttackConfiguration(this.currentAttack, attackConfig, this.currentAttackConfigKey); 
                }
            }
        }

        private void ConfigureAttack()
        {
            if (IsSequenceViewActive)
            {
                ConfigureAttackThroughCombatSimulator();
                Dispatcher.Invoke(() => {
                    Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                    Mouse.OverrideCursor = cursor;
                });
            }
            else
            {
                if (this.IsSweepAttackInProgress)
                {
                    List<Tuple<Attack, List<Character>, Guid>> attacksWithTargets = new List<Tuple<AnimatedAbilities.Attack, List<Characters.Character>, Guid>>();
                    foreach (var tuple in this.attacksInProgress)
                    {
                        Attack attack = tuple.Item1;
                        List<Character> attackers = tuple.Item2;
                        List<Character> defenders = tuple.Item3;
                        Guid configKey = (Guid)tuple.Item4;
                        foreach (var defender in defenders)
                        {
                            if (attackers.Count > 0 && (IsGangActive || attackers.All(ac => (ac as CrowdMemberModel).RosterCrowd.IsGangMode)))
                            {
                                defender.AttackConfigurationMap[configKey].Item2.AttackResults = new ObservableCollection<AttackResult>();
                                attackers.ForEach(ac => defender.AttackConfigurationMap[configKey].Item2.AttackResults.Add(new AttackResult { Attacker = ac, IsHit = false, AttackResultOption = AttackResultOption.Miss }));
                            }
                        }
                        attacksWithTargets.Add(new Tuple<Attack, List<Character>, Guid>(attack, defenders, configKey));
                    }
                    this.eventAggregator.GetEvent<ConfigureSweepAttackEvent>().Publish(attacksWithTargets);
                }
                else
                    LaunchAttackConfiguration();
            }
        }

        private void LaunchAttackConfiguration()
        {
            foreach (var currentTarget in targetCharacters)
            {
                if (this.AttackingCharacters.Count > 0 && (IsGangActive || AttackingCharacters.All(ac => (ac as CrowdMemberModel).RosterCrowd.IsGangMode)))
                {
                    currentTarget.AttackConfigurationMap[this.currentAttackConfigKey].Item2.AttackResults = new ObservableCollection<AttackResult>();
                    this.AttackingCharacters.ForEach(ac => currentTarget.AttackConfigurationMap[this.currentAttackConfigKey].Item2.AttackResults.Add(new AttackResult { Attacker = ac, IsHit = false, AttackResultOption = AttackResultOption.Miss }));
                }
            }
            if(targetCharacters.Count > 0)
            {
                this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<Attack, List<Character>, Guid>(this.currentAttack, this.targetCharacters, this.currentAttackConfigKey));
            }
        }

        private void ConfirmAttack(object state)
        {
            if (this.IsPlayingAttack)
            {
                if (this.IsSequenceViewActive)
                    this.hcsIntegrator.ConfirmAttack();
                if (PopupService.IsOpen("AttackConfigurationView") == true)
                {
                    this.eventAggregator.GetEvent<ConfirmAttacksEvent>().Publish(null);
                }
            }
        }

        #endregion

        
        #region HCS Integration

        private void HCSIntegrator_SequenceUpdated(object sender, CustomEventArgs<object> e)
        {
            if (this.IsSequenceViewActive)
            {
                object[] values = e.Value as object[];
                RefreshSequenceView(values);
            }
        }

        private void HCSIntegrator_ActiveCharacterUpdated(object sender, CustomEventArgs<object> e)
        {
            if (this.IsSequenceViewActive)
            {
                ActiveCharacterInfo activeCharacterInfo = e.Value as ActiveCharacterInfo;
                this.ActivateCharacterFromHCSActiveCharacterInfo(activeCharacterInfo);
            }
        }

        private void HCSIntegrator_AttackResultsUpdated(object sender, CustomEventArgs<object> e)
        {
            Dispatcher.Invoke(() =>
            {
                List<Character> targets = e.Value as List<Character>;
                if (this.IsPlayingAreaEffect)
                {
                    if (targets.All(t => !t.AttackConfigurationMap[this.currentAttackConfigKey].Item2.IsCenterTarget && t.AttackConfigurationMap[this.currentAttackConfigKey].Item2.MoveAttackerToTarget))
                        this.AttackingCharacters.ForEach(ac => ac.AttackConfigurationMap[this.currentAttackConfigKey].Item2.AttackCenterPosition = Vector3.Zero);
                }
                if(targets.Count > 0)
                    this.eventAggregator.GetEvent<AttackTargetUpdatedEvent>().Publish(new Tuple<Attack, List<Character>, Guid>(this.currentAttack, targets, currentAttackConfigKey));
            });
        }

        private void HCSIntegrator_SweepAttackResultsUpdated(object sender, CustomEventArgs<object> e)
        {
            Dispatcher.Invoke(() =>
                {
                    List<Tuple<Guid, List<Character>>> configuredAttacksFromHCS = e.Value as List<Tuple<Guid, List<Character>>>;
                    List<Tuple<Attack, List<Character>, Guid>> attacksWithTargets = new List<Tuple<AnimatedAbilities.Attack, List<Characters.Character>, Guid>>();
                    foreach (var tuple in configuredAttacksFromHCS)
                    {
                        Guid configKey = tuple.Item1;
                        List<Character> defenders = tuple.Item2;
                        List<Character> attackers = this.attacksInProgress.Where(ap => ap.Item4 == configKey).Select(ap => ap.Item2).First();
                        Attack attack = this.attacksInProgress.Where(ap => ap.Item4 == configKey).Select(ap => ap.Item1).First();
                        foreach (var defender in defenders)
                        {
                            if (attackers.Count > 0 && (IsGangActive || attackers.All(ac => (ac as CrowdMemberModel).RosterCrowd.IsGangMode)))
                            {
                                defender.AttackConfigurationMap[configKey].Item2.AttackResults = new ObservableCollection<AttackResult>();
                                attackers.ForEach(ac => defender.AttackConfigurationMap[configKey].Item2.AttackResults.Add(new AttackResult { Attacker = ac, IsHit = false, AttackResultOption = AttackResultOption.Miss }));
                            }
                            if (attack.IsHandToHand)
                            {
                                if (attackers.Any(ac => Vector3.Distance(ac.CurrentPositionVector, defender.CurrentPositionVector) / 8 > 1))
                                {
                                    defender.AttackConfigurationMap[configKey].Item2.MoveAttackerToTarget = true;
                                }
                            }
                        }
                        attacksWithTargets.Add(new Tuple<Attack, List<Character>, Guid>(attack, defenders, configKey));
                    }
                    this.eventAggregator.GetEvent<ConfigureSweepAttackEvent>().Publish(attacksWithTargets);
                }
            );
        }

        private void HCSIntegrator_EligibleCombatantsUpdated(object sender, CustomEventArgs<object> e)
        {
            Dispatcher.Invoke(() => 
            {
                if (e.Value != null)
                {
                    CombatantsCollection combatantsColl = e.Value as CombatantsCollection;
                    this.holdingCombatants = combatantsColl;
                    this.UpdateHoldingCharactersInSequence();
                    this.OrderSequence();
                    OnSequenceUpdated(this, null);
                }
            }); 
        }

        private void RefreshSequenceView(object sequenceInfo = null)
        {
            Dispatcher.Invoke(() => { this.BusyService.ShowBusy(); });
            Action d = delegate ()
            {
                if (sequenceInfo == null)
                    sequenceInfo = hcsIntegrator.GetLatestSequenceInfo();
                object[] values = sequenceInfo as object[];
                CombatantsCollection onDeckCombatants = values[0] as CombatantsCollection;
                Chronometer chronometer = values[1] as Chronometer;
                ActiveCharacterInfo activeCharacterInfo = values[2] as ActiveCharacterInfo;
                CombatantsCollection eligibleCombatants = values[3] as CombatantsCollection;
                this.SequenceParticipants = new ObservableCollection<HCSIntegration.Combatant>();
                int order = 0;
                if (onDeckCombatants != null)
                {
                    foreach (var combatant in onDeckCombatants.Combatants)
                    {
                        Character character = this.Participants.FirstOrDefault(p => p.Name == combatant.CharacterName) as Character;
                        if (character != null)
                        {
                            this.SequenceParticipants.Add(new Combatant { Phase = combatant.Phase, CharacterName = character.Name, CombatantCharacter = character, Order = ++order });
                        }
                        else
                        {
                            if (!this.SequenceParticipants.Any(sp => sp.Phase == Constants.NOT_FOUND_IN_ROSTER_GROUP_NAME && sp.CharacterName == combatant.CharacterName))
                                this.SequenceParticipants.Add(new Combatant { Phase = Constants.NOT_FOUND_IN_ROSTER_GROUP_NAME, CharacterName = combatant.CharacterName, CombatantCharacter = new CrowdMemberModel { Name = "", RosterCrowd = new CrowdModel { Name = "" } }, Order = Int32.MaxValue });
                        }
                    } 
                }
                
                if (chronometer != null)
                {
                    if (onDeckCombatants != null && onDeckCombatants.Combatants.Count > 0 && chronometer.CurrentPhase == onDeckCombatants.Combatants[0].Phase)
                    {
                        this.CurrentPhase = chronometer.CurrentPhase;
                    }
                }
                else if(onDeckCombatants != null && onDeckCombatants.Combatants.Count > 0)
                {
                    this.CurrentPhase = onDeckCombatants.Combatants[0].Phase;
                }
                var currCombatant = SequenceParticipants.FirstOrDefault(sp => sp.Phase == this.CurrentPhase);
                if(currCombatant != null)
                    currCombatant.IsActivePhase = true;
                if (activeCharacterInfo != null)
                {
                    this.ActivateCharacterFromHCSActiveCharacterInfo(activeCharacterInfo);
                }
                this.holdingCombatants = eligibleCombatants;
                UpdateHoldingCharactersInSequence();
                foreach (var rosterOnlyParticipant in this.Participants.Where(p => !this.SequenceParticipants.Any(sp => sp.CombatantCharacter == (p as Character))))
                {
                    this.SequenceParticipants.Add(new Combatant { Phase = Constants.NOT_FOUND_IN_SEQUENCE_GROUP_NAME, CharacterName = rosterOnlyParticipant.Name, CombatantCharacter = rosterOnlyParticipant as Character, Order = Int32.MaxValue });
                }
                OrderSequence();
                this.hcsIntegrator.InGameCharacters = this.Participants.Where(p => (p as Character).HasBeenSpawned).Cast<Character>().ToList();
                OnSequenceUpdated(this, null);
                this.BusyService.HideBusy();
            };
            AsyncDelegateExecuter adex = new Library.Utility.AsyncDelegateExecuter(d, 1000);
            adex.ExecuteAsyncDelegate();
        }

        private void UpdateHoldingCharactersInSequence()
        {
            if(this.holdingCombatants != null && this.holdingCombatants.Combatants != null && this.holdingCombatants.Combatants.Count > 0)
            {
                foreach (Combatant c in this.holdingCombatants.Combatants)
                {
                    Combatant holdingCombatant = this.SequenceParticipants.FirstOrDefault(sp => sp.CharacterName == c.CharacterName && sp.Phase == Constants.HOLDING_CHARACTERS_GROUP_NAME);
                    if(holdingCombatant == null)
                    {
                        Character character = this.Participants.FirstOrDefault(p => p.Name == c.CharacterName) as Character;
                        this.SequenceParticipants.Add(new Combatant { Phase = Constants.HOLDING_CHARACTERS_GROUP_NAME, CharacterName = c.CharacterName, CombatantCharacter = character, Order = Int32.MinValue });
                    }
                }
            }
            else
            {
                List<Combatant> previousHoldingCombatants = this.SequenceParticipants.Where(sp => sp.Phase == Constants.HOLDING_CHARACTERS_GROUP_NAME).ToList();
                foreach (Combatant c in previousHoldingCombatants)
                {
                    Combatant deletingCombatant = this.SequenceParticipants.First(sp => sp.CharacterName == c.CharacterName);
                    this.SequenceParticipants.Remove(deletingCombatant);
                }
            }
        }

        private void OrderSequence()
        {
            this.SequenceParticipants = new ObservableCollection<Combatant>(this.SequenceParticipants.OrderBy(sp => sp.Order));
        }

        private void ConfigureAttackThroughCombatSimulator()
        {
            if (this.IsSweepAttackInProgress)
            {
                List<Tuple<Attack, List<Character>, List<Character>, Guid>> adjustedAttacksInProgress = new List<Tuple<Attack, List<Character>, List<Character>, Guid>>();
                foreach(var tuple in this.attacksInProgress)
                {
                    Attack attack = tuple.Item1;
                    List<Character> attackers = tuple.Item2;
                    List<Character> defenders = tuple.Item3;
                    Guid configKey = tuple.Item4;
                    if (defenders.Count > 1 && !attack.IsAreaEffect && !attack.CanSpread && !attack.IsAutoFire)
                    {
                        attackers.ForEach(c => { c.RemoveAttackConfiguration(configKey); });
                        foreach (Character defender in defenders)
                        {
                            Guid newConfigKey = Guid.NewGuid();
                            attackers.ForEach(c => { c.AddAttackConfiguration(attack, new AttackConfiguration(), newConfigKey); });
                            defender.RemoveAttackConfiguration(configKey);
                            defender.AddAttackConfiguration(attack, new AnimatedAbilities.AttackConfiguration(), newConfigKey);
                            adjustedAttacksInProgress.Add(new Tuple<AnimatedAbilities.Attack, List<Characters.Character>, List<Characters.Character>, Guid>(attack, attackers, new List<Characters.Character> { defender }, newConfigKey));
                        }
                    }
                    else
                        adjustedAttacksInProgress.Add(new Tuple<Attack, List<Character>, List<Character>, Guid>(attack, attackers, defenders, configKey));
                }
                this.attacksInProgress = adjustedAttacksInProgress;
                this.hcsIntegrator.ConfigureAttacks(this.attacksInProgress, true);
            }
            else if (this.ActiveCharacter != null && this.AttackingCharacters.Contains(this.ActiveCharacter as Character) && this.targetCharacters.Count > 0)
            {
                this.hcsIntegrator.ConfigureAttacks(this.attacksInProgress, false);
            }
        }

        private void ActivateHeldCharacter()
        {
            if (this.IsPlayingAttack)
            {
                this.savedAttackState = new object[] { this.currentAttack, new List<Character>(this.targetCharacters), new List<Character>(this.AttackingCharacters) };
                this.CancelAttack(this.currentAttack, this.targetCharacters.ToList(), this.currentAttackConfigKey);
            }
            this.hcsIntegrator.ActivateHeldCharacter(this.SelectedParticipants[0] as Character);
        }

        private void OnMovementStopped(CharacterMovement characterMovement)
        {
            if (this.IsSequenceViewActive)
            {
                NotifyCombatSimulatorAboutMovementStop(characterMovement);
                DisableHTHAttacksOutOfDistanceLimit();
            }
        }

        private void NotifyCombatSimulatorAboutMovementStop(CharacterMovement characterMovement)
        {
            double distance = Math.Round(this.CurrentDistanceCountingCharacter.CurrentDistanceCount, 2);
            this.hcsIntegrator.NotifyStopMovement(characterMovement, distance);
            OnShowNotification(this, new CustomEventArgs<string> { Value = Messages.MOVEMENT_STOPPED_MESSAGE });
        }

        public bool IsCharacterHolding(Character character)
        {
            return this.holdingCombatants != null && this.holdingCombatants.Combatants != null && this.holdingCombatants.Combatants.Any(c => c.CharacterName == character.Name);
        }

        #endregion

        #region Desktop Context Menu Handlers

        private void desktopContextMenu_AbortMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.AbortAction();
        }
        void desktopContextMenu_SpawnMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.Spawn();
        }

        void desktopContextMenu_SavePositionMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.SavePosition();
        }

        void desktopContextMenu_ResetOrientationMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.ResetOrientation();
        }

        void desktopContextMenu_PlaceMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.Place();
        }

        private void desktopContextMenu_MoveTargetToCharacterMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            string destharacterName = e.Value as string;
            Character character = this.Participants.FirstOrDefault(p => p.Name == destharacterName) as Character;
            if (character != null)
            {
                Vector3 destination = new Vector3(character.Position.X, character.Position.Y, character.Position.Z);
                foreach (Character c in this.SelectedParticipants)
                {
                    c.MoveToLocation(destination);
                }
            }
        }

        void desktopContextMenu_MoveTargetToCameraMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.MoveTargetToCamera();
        }

        void desktopContextMenu_MoveCameraToTargetMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.TargetAndFollow(true);
        }

        void desktopContextMenu_ManueverWithCameraMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            this.ToggleManeuverWithCamera();
        }

        void desktopContextMenu_DefaultContextMenuDisplayed(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
        }

        void desktopContextMenu_CloneAndLinkMenuItemSelected(object sender, EventArgs e)
        {
            Character character = SelectedParticipants != null && SelectedParticipants.Count == 1 ? SelectedParticipants[0] as Character : null;
            this.eventAggregator.GetEvent<CloneLinkCrowdMemberEvent>().Publish(character as CrowdMemberModel);
        }

        void desktopContextMenu_ClearFromDesktopMenuItemSelected(object sender, EventArgs e)
        {
            this.ClearFromDesktop();
        }

        void desktopContextMenu_AttackTargetMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.TargetCharacterForAttack(null);
        }

        void desktopContextMenu_AttackTargetAndExecuteMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.TargetAndExecuteAttack(null);
        }

        void desktopContextMenu_AttackTargetAndExecuteCrowdMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
            this.TargetAndExecuteCrowd();
        }
        void desktopContextMenu_ExecuteSweepAttackMenuItemSelected(object sender, EventArgs e)
        {
            this.TargetAndExecuteSweepAttack(null);
        }
        private void desktopContextMenu_AttackContextMenuDisplayed(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                AddDesktopTargetToRosterSelection(character);
        }

        private void desktopContextMenu_ActivateMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                this.ToggleActivateCharacter(character);
        }

        private void desktopContextMenu_ActivateCrowdAsGangMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Character character = e.Value as Character;
            if (character != null)
                this.ToggleActivateCrowdAsGang();
        }

        private void desktopContextMenu_ActivateCharacterOptionMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Object[] parameters = e.Value as Object[];
            if (parameters != null && parameters.Length == 3)
            {
                Character character = parameters[0] as Character;
                string optionGroupName = parameters[1] as string;
                string optionName = parameters[2] as string;
                this.ToggleActivateCharacter(character, optionGroupName, optionName);
            }
        }

        private void desktopContextMenu_SpreadNumberMenuItemSelected(object sender, CustomEventArgs<Object> e)
        {
            Object[] parameters = e.Value as Object[];
            if(parameters != null && parameters.Length == 2)
            {
                Character target = parameters[0] as Character;
                int spreadNumber = (int)parameters[1];
                this.currentAttack.SpreadDistance = spreadNumber;
                this.TargetAndExecuteAttack(null);
            }
        }

        #endregion

        #region Desktop Key Handling
        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (_rosterGameState.CurrentActiveWindowName == Constants.ROSTER_EXPLORER || _rosterGameState.CurrentActiveWindowName == Constants.ACTIVE_CHARACTER_WIDGET)
            {
                if (inputKey == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.Place;
                }
                else if (inputKey == Key.S && Keyboard.Modifiers == (ModifierKeys.Control))
                {
                    return this.SavePosition;
                }
                else if (inputKey == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.Spawn();
                }
                else if (inputKey == Key.T && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ToggleTargeted;
                }
                else if (inputKey == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ToggleManeuverWithCamera;
                }
                else if (inputKey == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.TargetAndFollow;
                }
                else if (inputKey == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.EditCharacter;
                }
                else if (inputKey == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.MoveTargetToCamera;
                }
                else if (inputKey == Key.Y && Keyboard.Modifiers == (ModifierKeys.Control))
                {
                    return this.CycleCommandsThroughCrowd;
                }
                else if (inputKey == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ActivateCharacterOrGang;
                }
                else if (inputKey == Key.U && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ToggleGangMode;
                }
                else if (inputKey == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
                {
                    return this.ConfirmAttacks;
                }
                else if (inputKey == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ResetOrientation;
                }
                else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return this.ClearFromDesktop;
                }
                else if (inputKey == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    //Jeff fixed activating keystroke problem so works without activating a character
                    return ActivateDefaultMovementToActivate;
                }
                else if (inputKey == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.TeleportTargetToCamera(null);
                }
                else if (inputKey == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleTargetOnHover(null);
                }
                else if (inputKey == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleRelativePositioning(null);
                }
                else if (inputKey == Key.J && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleSpawnOnClick();
                }
                else if (inputKey == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleCloneAndSpawn();
                }
                else if (inputKey == Key.B && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    this.ToggleOverheadMode();
                }
            }

            if (this.ActiveCharacter != null)
            {
                if (inputKey == Key.F1)
                {
                    return this.PlayDefaultAbility;
                }
                else if (inputKey == Key.F2)
                {
                    return ActivateDefaultMovementToActivate;
                }
            }
            Character targetedCharacter = null;
            if (IsGangActive)
            {
                targetedCharacter = Participants.FirstOrDefault(p => (p as Character).IsGangLeader) as Character;
            }
            else
            {
                targetedCharacter = GetCurrentTarget() as Character;
            }
            if (Keyboard.Modifiers == (ModifierKeys.Alt | ModifierKeys.Shift) && targetedCharacter != null && targetedCharacter.AnimatedAbilities.Any(ab => ab.ActivateOnKey == vkCode))
            {
                var activeAbility = targetedCharacter.AnimatedAbilities.First(ab => ab.ActivateOnKey == vkCode);
                targetedCharacter.Target(false);
                targetedCharacter.ActiveIdentity.RenderWithoutAnimation(target: targetedCharacter);
                //activeAbility.Play();
                this.eventAggregator.GetEvent<PlayAnimatedAbilityEvent>().Publish(new Tuple<Character, AnimatedAbility>(null, activeAbility));
            }
            else if (targetedCharacter != null && Keyboard.Modifiers == ModifierKeys.Alt)
            {
                CharacterMovement cm = null;
                if (targetedCharacter.Movements.Any(m => m.ActivationKey == vkCode))
                {
                    cm = targetedCharacter.Movements.First(m => m.ActivationKey == vkCode);
                }
                else if (inputKey == Key.K)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Walk");
                }
                else if (inputKey == Key.U)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Run");
                }
                else if (inputKey == Key.S)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Swim");
                }
                else if (inputKey == Key.P)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Steampack");
                }
                else if (inputKey == Key.F)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Fly");
                }
                else if (inputKey == Key.B)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Beast");
                }
                else if (inputKey == Key.J)
                {
                    cm = targetedCharacter.Movements.FirstOrDefault(m => m.ActivationKey == System.Windows.Forms.Keys.None && m.Name == "Ninja");
                }
                else if (inputKey == Key.T)
                {
                    targetedCharacter.TeleportToCamera();
                }

                if (cm != null)
                {
                    if (!cm.IsActive)
                    {
                        this.eventAggregator.GetEvent<PlayMovementInitiatedEvent>().Publish(cm);
                    }
                    else
                        this.eventAggregator.GetEvent<StopMovementEvent>().Publish(cm);
                }
            }
            else if (inputKey == Key.Escape)
            {
                if (this.IsPlayingAttack)
                {
                    if(currentAttack != null)
                    {
                        foreach (var c in this.targetCharacters)
                        {
                            c.AttackConfigurationMap[this.currentAttackConfigKey] = new Tuple<Attack, AttackConfiguration>(this.currentAttack, new AttackConfiguration { AttackMode = AttackMode.None, AttackEffectOption = AttackEffectOption.None });
                        }
                        //this.eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
                        this.eventAggregator.GetEvent<CancelActiveAttackEvent>().Publish(new Tuple<Attack, List<Character>, Guid>(this.currentAttack, this.targetCharacters, this.currentAttackConfigKey));
                    }
                }
                else if (IsSweepAttackInProgress)
                {
                    this.CancelAttacks();
                }
                else if (CharacterIsMoving)
                {
                    this.eventAggregator.GetEvent<StopMovementEvent>().Publish(_rosterGameState.ActiveCharacterMovement);
                }
                else if (this.ActiveCharacter != null)
                {
                    if (this.IsGangActive)
                        DeactivateGang();
                    else
                        this.DeactivateCharacter(this.ActiveCharacter as Character);
                }
            }
            return null;
        }

        #endregion
    }
}
