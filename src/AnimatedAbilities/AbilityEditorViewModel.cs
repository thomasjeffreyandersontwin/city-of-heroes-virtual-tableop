using Framework.WPF.Behaviors;
using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared.Events;
using Module.Shared.Messages;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Reflection;
using System.IO;
using System.Collections.ObjectModel;
using Module.Shared;
using System.Windows.Data;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.HCSIntegration;

using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Library = Module.HeroVirtualTabletop.Library;
using Roster = Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
namespace HeroVTT.AnimatedAbilities
{
    public class AbilityEditorViewModel : BaseViewModel
    {

        #region Private Fields

        private EventAggregator eventAggregator;
        private IMessageBoxService messageBoxService;
        private IResourceRepository resourceRepository;
        private IDesktopKeyEventHandler desktopKeyEventHandler;
        private IHCSIntegrator hcsIntegrator;

        public bool isUpdatingCollection = false;
        public object lastAnimationElementsStateToUpdate = null;
        private Character currentSweeper = null;

        #endregion

        #region Events

        public event EventHandler<CustomEventArgs<bool>> AnimationAdded;
        public void OnAnimationAdded(object sender, CustomEventArgs<bool> e)
        {
            if (AnimationAdded != null)
            {
                AnimationAdded(sender, e);
            }
        }

        public event EventHandler AnimationElementDraggedFromGrid;
        public void OnAnimationElementDraggedFromGrid(object sender, EventArgs e)
        {
            if (AnimationElementDraggedFromGrid != null)
            {
                AnimationElementDraggedFromGrid(sender, e);
            }
        }

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

        public event EventHandler SelectionChanged;
        public void OnSelectionChanged(object sender, EventArgs e)
        {
            if (SelectionChanged != null)
            {
                SelectionChanged(sender, e);
            }
        }

        public event EventHandler<CustomEventArgs<ExpansionUpdateEvent>> ExpansionUpdateNeeded;
        public void OnExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            if (ExpansionUpdateNeeded != null)
                ExpansionUpdateNeeded(sender, e);
        }

        #endregion

        #region Public Properties
        private Character owner;
        public Character Owner
        {
            get
            {
                return owner;
            }
            set
            {
                owner = value;
                OnPropertyChanged("Owner");
            }
        }

        private Attack currentAttackAbility;
        public Attack CurrentAttackAbility
        {
            get
            {
                return currentAttackAbility;
            }
            set
            {
                currentAttackAbility = value;
                OnPropertyChanged("CurrentAttackAbility");
                this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            }
        }
        private ObservableCollection<System.Windows.Forms.Keys> availableKeys;
        public ObservableCollection<System.Windows.Forms.Keys> AvailableKeys
        {
            get
            {
                return availableKeys;
            }
            set
            {
                availableKeys = value;
                OnPropertyChanged("AvailableKeys");
            }
        }
        private AnimatedAbility currentAbility;
        public AnimatedAbility CurrentAbility
        {
            get
            {
                return currentAbility;
            }
            set
            {
                currentAbility = value;
                OnPropertyChanged("CurrentAbility");
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
                this.PasteAnimationCommand.RaiseCanExecuteChanged();
            }
        }
        private bool isShowingAblityEditor;
        public bool IsShowingAbilityEditor
        {
            get
            {
                return isShowingAblityEditor;
            }
            set
            {
                isShowingAblityEditor = value;
                if (value)
                    Helper.GlobalVariables_CurrentActiveWindowName = Constants.ABILITY_EDITOR;
                else
                    this.eventAggregator.GetEvent<PanelClosedEvent>().Publish(Constants.ABILITY_EDITOR);
                OnPropertyChanged("IsShowingAbilityEditor");
            }
        }

        private IAnimationElement selectedAnimationElement;
        public IAnimationElement SelectedAnimationElement
        {
            get
            {
                return selectedAnimationElement;
            }
            set
            {
                if (selectedAnimationElement != null)
                    (selectedAnimationElement as AnimationElement).PropertyChanged -= SelectedAnimationElement_PropertyChanged;
                if (value != null)
                    (value as AnimationElement).PropertyChanged += SelectedAnimationElement_PropertyChanged;
                selectedAnimationElement = value;
                OnPropertyChanged("SelectedAnimationElement");
                OnPropertyChanged("IsAnimationElementSelected");
                OnPropertyChanged("CanPlayWithNext");
                if (selectedAnimationElement != null)
                    SetSavedUIFilter(selectedAnimationElement);
                OnSelectionChanged(value, null);
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
                this.CutAnimationCommand.RaiseCanExecuteChanged();
                this.PasteAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private void SelectedAnimationElement_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Resource")
            {
                AnimationElement element = sender as AnimationElement;
                (sender as AnimationElement).DisplayName = GetDisplayNameFromResourceName(element.Resource);
                if (element.Resource!= null && element.Resource.Reference != null && element.Resource.Reference.IsAttack)
                {
                    if (this.CurrentAttackAbility != null)
                        this.CurrentAttackAbility.IsAttack = true;
                    this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
                }
                SaveAbility();
                DemoAnimation();
                SaveResources();
                SaveUISettingsForResouce(element, element.Resource);
                this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();

            }
        }

        private IAnimationElement selectedAnimationParent;
        public IAnimationElement SelectedAnimationParent
        {
            get
            {
                return selectedAnimationParent;
            }
            set
            {
                selectedAnimationParent = value;
                this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        private AnimationSequenceType sequenceType;

        public AnimationSequenceType SequenceType
        {
            get
            {
                return sequenceType;
            }
            set
            {
                sequenceType = value;
                OnPropertyChanged("SequenceType");
            }
        }


        private bool isSequenceAbilitySelected;
        public bool IsSequenceAbilitySelected
        {
            get
            {
                return isSequenceAbilitySelected;
            }
            set
            {
                isSequenceAbilitySelected = value;
                OnPropertyChanged("IsSequenceAbilitySelected");
            }
        }

        private bool isPauseElementSelected;
        public bool IsPauseElementSelected
        {
            get
            {
                return isPauseElementSelected;
            }
            set
            {
                isPauseElementSelected = value;
                OnPropertyChanged("IsPauseElementSelected");
            }
        }

        private bool isFxElementSelected;
        public bool IsFxElementSelected
        {
            get
            {
                return isFxElementSelected;
            }
            set
            {
                isFxElementSelected = value;
                OnPropertyChanged("IsFxElementSelected");
            }
        }

        private SequenceElement currentSequenceElement;
        public SequenceElement CurrentSequenceElement
        {
            get
            {
                return currentSequenceElement;
            }
            set
            {
                currentSequenceElement = value;
                OnPropertyChanged("CurrentSequenceElement");
            }
        }

        private PauseElement currentPauseElement;
        public PauseElement CurrentPauseElement
        {
            get
            {
                return currentPauseElement;
            }
            set
            {
                currentPauseElement = value;
                OnPropertyChanged("CurrentPauseElement");
                this.ConfigureUnitPauseCommand.RaiseCanExecuteChanged();
            }
        }
        private FXEffectElement currentFxElement;
        public FXEffectElement CurrentFxElement
        {
            get
            {
                return currentFxElement;
            }
            set
            {
                currentFxElement = value;
                OnPropertyChanged("CurrentFxElement");
                this.ToggleDirectionalFxCommand.RaiseCanExecuteChanged();
            }
        }
        private bool isReferenceAbilitySelected;
        public bool IsReferenceAbilitySelected
        {
            get
            {
                return isReferenceAbilitySelected;
            }
            set
            {
                isReferenceAbilitySelected = value;
                OnPropertyChanged("IsReferenceAbilitySelected");
            }
        }

        private ReferenceAbility currentReferenceElement;
        public ReferenceAbility CurrentReferenceElement
        {
            get
            {
                return currentReferenceElement;
            }
            set
            {
                currentReferenceElement = value;
                OnPropertyChanged("CurrentReferenceElement");
                this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            }
        }

        private IAnimationElement selectedAnimationElementRoot;
        public IAnimationElement SelectedAnimationElementRoot
        {
            get
            {
                return selectedAnimationElementRoot;
            }
            set
            {
                selectedAnimationElementRoot = value;
            }
        }
        public string OriginalName { get; set; }
        public string OriginalAnimationDisplayName { get; set; }
        private string editableAnimationDisplayName;
        public string EditableAnimationDisplayName
        {
            get
            {
                return editableAnimationDisplayName;
            }
            set
            {
                editableAnimationDisplayName = value;
                OnPropertyChanged("EditableAnimationDisplayName");
            }
        }
        public bool IsAnimationElementSelected
        {
            get
            {
                return this.SelectedAnimationElement != null;
            }
        }


        private ObservableCollection<AnimationResource> movResources;
        public ReadOnlyObservableCollection<AnimationResource> MOVResources { get; private set; }

        private CollectionViewSource movResourcesCVS;
        public CollectionViewSource MOVResourcesCVS
        {
            get
            {
                return movResourcesCVS;
            }
        }

        private ObservableCollection<AnimationResource> fxResources;
        public ReadOnlyObservableCollection<AnimationResource> FXResources { get; private set; }

        private CollectionViewSource fxResourcesCVS;
        public CollectionViewSource FXResourcesCVS
        {
            get
            {
                return fxResourcesCVS;
            }
        }

        private ObservableCollection<AnimationResource> soundResources;
        public ReadOnlyObservableCollection<AnimationResource> SoundResources { get; private set; }

        private CollectionViewSource soundResourcesCVS;
        public CollectionViewSource SoundResourcesCVS
        {
            get
            {
                return soundResourcesCVS;
            }
        }

        private CollectionViewSource referenceAbilitiesCVS;
        public CollectionViewSource ReferenceAbilitiesCVS
        {
            get
            {
                return referenceAbilitiesCVS;
            }
        }

        private CollectionViewSource identityResourcesCVS;
        public CollectionViewSource IdentityResourcesCVS
        {
            get
            {
                return identityResourcesCVS;
            }
        }

        private string filter;
        public string Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                if (SelectedAnimationElement != null)
                    if (value.Length > 2 || value.Length == 0)
                    {
                        switch (SelectedAnimationElement.Type)
                        {
                            case AnimationElementType.Movement:
                                Helper.SaveUISettings("Ability_MoveFilter", value);
                                if (movResourcesCVS != null && movResourcesCVS.View != null)
                                    movResourcesCVS.View.Refresh();
                                break;
                            case AnimationElementType.FX:
                                Helper.SaveUISettings("Ability_FxFilter", value);
                                if (fxResourcesCVS != null && fxResourcesCVS.View != null)
                                    fxResourcesCVS.View.Refresh();
                                break;
                            case AnimationElementType.Sound:
                                Helper.SaveUISettings("Ability_SoundFilter", value);
                                if (soundResourcesCVS != null && soundResourcesCVS.View != null)
                                    soundResourcesCVS.View.Refresh();
                                break;
                            case AnimationElementType.Reference:
                                Helper.SaveUISettings("Ability_ReferenceFilter", value);
                                if (referenceAbilitiesCVS != null && referenceAbilitiesCVS.View != null)
                                    referenceAbilitiesCVS.View.Refresh();
                                break;
                            case AnimationElementType.LoadIdentity:
                                Helper.SaveUISettings("Ability_IdentityFilter", value);
                                if (identityResourcesCVS != null && identityResourcesCVS.View != null)
                                    identityResourcesCVS.View.Refresh();
                                break;
                        }
                    }
                OnPropertyChanged("Filter");
            }
        }

        private void SetSavedUIFilter(IAnimationElement element)
        {
            switch (element.Type)
            {
                case AnimationElementType.Movement:
                    string moveFilter = Helper.GetUISettings("Ability_MoveFilter") as string;
                    if (!string.IsNullOrEmpty(moveFilter))
                        this.Filter = moveFilter;
                    else
                        this.Filter = string.Empty;
                    break;
                case AnimationElementType.FX:
                    string fxFilter = Helper.GetUISettings("Ability_FxFilter") as string;
                    if (!string.IsNullOrEmpty(fxFilter))
                        this.Filter = fxFilter;
                    else
                        this.Filter = string.Empty;
                    break;
                case AnimationElementType.Sound:
                    string soundFilter = Helper.GetUISettings("Ability_SoundFilter") as string;
                    if (!string.IsNullOrEmpty(soundFilter))
                        this.Filter = soundFilter;
                    else
                        this.Filter = string.Empty;
                    break;
                case AnimationElementType.Sequence:
                    break;
                case AnimationElementType.Pause:
                    break;
                case AnimationElementType.Reference:
                    string refFilter = Helper.GetUISettings("Ability_ReferenceFilter") as string;
                    if (!string.IsNullOrEmpty(refFilter))
                        this.Filter = refFilter;
                    else
                        this.Filter = string.Empty;
                    break;
            }
        }

        public bool CanPlayWithNext
        {
            get
            {
                bool canPlayWithNext = false;
                if (SelectedAnimationElement != null && currentAbility != null && currentAbility.AnimationElements != null)
                {
                    if (SelectedAnimationElement.Type == AnimationElementType.FX || SelectedAnimationElement.Type == AnimationElementType.Movement)
                    {
                        IAnimationElement next = GetNextAnimationElement(SelectedAnimationElement);
                        if (next != null && (next.Type == AnimationElementType.FX || next.Type == AnimationElementType.Movement))
                            canPlayWithNext = true;
                    }
                }
                return canPlayWithNext;
            }
        }

        private bool isAttackSelected = false;
        public bool IsAttackSelected
        {
            get
            {
                return isAttackSelected;
            }
            set
            {
                isAttackSelected = value;
                OnPropertyChanged("IsAttackSelected");
                this.ToggleAttackCommand.RaiseCanExecuteChanged();
            }
        }

        private bool isHitSelected;
        public bool IsHitSelected
        {
            get
            {
                return isHitSelected;
            }
            set
            {
                isHitSelected = value;
                OnPropertyChanged("IsHitSelected");
            }
        }
        public bool CanEditAbilityOptions
        {
            get
            {
                return !Helper.GlobalVariables_IsPlayingAttack;
            }
        }
        #endregion

        #region Commands


        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> LoadResourcesCommand { get; private set; }
        public DelegateCommand<object> EnterAbilityEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitAbilityRenameCommand { get; private set; }
        public DelegateCommand<object> CancelAbilityEditModeCommand { get; private set; }
        public DelegateCommand<object> AddAnimationElementCommand { get; private set; }
        public DelegateCommand<object> ChangePersistenceCommand { get; private set; }
        public DelegateCommand<object> SaveAbilityCommand { get; private set; }
        public DelegateCommand<object> RemoveAnimationCommand { get; private set; }
        public DelegateCommand<object> SaveSequenceCommand { get; private set; }
        public DelegateCommand<object> UpdateSelectedAnimationCommand { get; private set; }
        public DelegateCommand<object> SubmitAnimationElementRenameCommand { get; private set; }
        public DelegateCommand<object> EnterAnimationElementEditModeCommand { get; private set; }
        public DelegateCommand<object> CancelAnimationElementEditModeCommand { get; private set; }
        public DelegateCommand<object> DemoAnimatedAbilityCommand { get; private set; }
        public DelegateCommand<object> DemoAnimationCommand { get; private set; }
        public DelegateCommand<object> CloneAnimationCommand { get; private set; }
        public DelegateCommand<object> CutAnimationCommand { get; private set; }
        public DelegateCommand<object> LinkAnimationCommand { get; private set; }
        public DelegateCommand<object> PasteAnimationCommand { get; private set; }
        public DelegateCommand<object> UpdateReferenceTypeCommand { get; private set; }
        public DelegateCommand<object> ConfigureAttackAnimationCommand { get; private set; }
        public DelegateCommand<object> ConfigureUnitPauseCommand { get; private set; }
        public DelegateCommand<object> ChangePlayWithNextCommand { get; private set; }
        public DelegateCommand<object> ToggleAttackCommand { get; private set; }
        public DelegateCommand<object> ToggleDirectionalFxCommand { get; private set; }
        #endregion

        #region Constructor

        public AbilityEditorViewModel(IBusyService busyService, IUnityContainer container, IMessageBoxService messageBoxService, IResourceRepository resourceRepository, IDesktopKeyEventHandler keyEventHandler, IHCSIntegrator hcsIntegrator, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.resourceRepository = resourceRepository;
            this.eventAggregator = eventAggregator;
            this.messageBoxService = messageBoxService;
            this.desktopKeyEventHandler = keyEventHandler;
            this.hcsIntegrator = hcsIntegrator;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditAbilityEvent>().Subscribe(this.LoadAnimatedAbility);
            this.eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(this.LoadReferenceResource);
            //this.eventAggregator.GetEvent<FinishedIdentityCollectionRetrievalEvent>().Subscribe(this.LoadIdentityResource);
            this.eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(this.AttackInitiated);
            this.eventAggregator.GetEvent<ResetSweepEvent>().Subscribe(this.ResetSweep);
            this.eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(this.AttackEnded);
            this.eventAggregator.GetEvent<PlayAnimatedAbilityEvent>().Subscribe(this.PlayAnimatedAbility);
            this.eventAggregator.GetEvent<StopAnimatedAbilityEvent>().Subscribe(this.StopAnimatedAbility);
            // Unselect everything at the beginning
            this.InitializeAnimationElementSelections();
            this.InitializeDesktopKeyEventHandlers();
            
        }

        #endregion

        #region Initialization

        private void InitializeAnimationElementSelections()
        {
            this.SelectedAnimationElement = null;
            this.SelectedAnimationParent = null;
            this.IsSequenceAbilitySelected = false;
            this.IsReferenceAbilitySelected = false;
            this.IsPauseElementSelected = false;
            this.IsFxElementSelected = false;
            this.CurrentSequenceElement = null;
            this.CurrentReferenceElement = null;
            this.CurrentPauseElement = null;
            this.CurrentFxElement = null;
            this.CurrentAbility = null;
            this.CurrentAttackAbility = null;
            this.Owner = null;

            if (availableKeys == null)
            {
                availableKeys = new ObservableCollection<System.Windows.Forms.Keys>();
                foreach (var key in Enum.GetValues(typeof(System.Windows.Forms.Keys)).Cast<System.Windows.Forms.Keys>())
                {
                    //if (!IsAbilityKey(key))
                        availableKeys.Add(key);
                }
            }
        }

        public void InitializeDesktopKeyEventHandlers()
        {
            this.desktopKeyEventHandler.AddKeyEventHandler(this.RetrieveEventFromKeyInput);
        }

        private void InitializeCommands()
        {
            this.CloseEditorCommand = new DelegateCommand<object>(this.UnloadAbility);
            this.LoadResourcesCommand = new DelegateCommand<object>(this.LoadResources);
            this.SubmitAbilityRenameCommand = new DelegateCommand<object>(this.SubmitAbilityRename);
            this.SaveAbilityCommand = new DelegateCommand<object>(delegate(object state) { this.SaveAbility(); }, this.CanSaveAbility);
            this.SaveSequenceCommand = new DelegateCommand<object>(this.SaveSequence, this.CanSaveSequence);
            this.EnterAbilityEditModeCommand = new DelegateCommand<object>(this.EnterAbilityEditMode);
            this.CancelAbilityEditModeCommand = new DelegateCommand<object>(this.CancelAbilityEditMode);
            this.ChangePersistenceCommand = new DelegateCommand<object>(this.ChangePersistence);
            this.ChangePlayWithNextCommand = new DelegateCommand<object>(this.ChangePlayWithNext);
            this.AddAnimationElementCommand = new DelegateCommand<object>(delegate(object state) { EditingAnimationType = (AnimationElementType)state; this.AddAnimationElement(); }, this.CanAddAnimationElement);
            this.RemoveAnimationCommand = new DelegateCommand<object>(delegate(object state) { this.RemoveAnimation(); }, this.CanRemoveAnimation);
            this.UpdateSelectedAnimationCommand = new DelegateCommand<object>(this.UpdateSelectedAnimation);
            this.SubmitAnimationElementRenameCommand = new DelegateCommand<object>(this.SubmitAnimationElementRename);
            this.EnterAnimationElementEditModeCommand = new DelegateCommand<object>(this.EnterAnimationElementEditMode, this.CanEnterAnimationElementEditMode);
            this.CancelAnimationElementEditModeCommand = new DelegateCommand<object>(this.CancelAnimationElementEditMode);
            this.DemoAnimatedAbilityCommand = new DelegateCommand<object>(delegate(object state) { this.DemoAnimatedAbility(); }, this.CanDemoAnimation);
            this.DemoAnimationCommand = new DelegateCommand<object>(delegate(object state) { this.DemoAnimation(); }, this.CanDemoAnimation);
            this.CloneAnimationCommand = new DelegateCommand<object>(delegate(object state) { this.CloneAnimation(); }, this.CanCloneAnimation);
            this.CutAnimationCommand = new DelegateCommand<object>(delegate(object state) { this.CutAnimation(); }, this.CanCutAnimation);
            this.PasteAnimationCommand = new DelegateCommand<object>(delegate(object state) { this.PasteAnimation(); }, this.CanPasteAnimation);
            this.UpdateReferenceTypeCommand = new DelegateCommand<object>(this.UpdateReferenceType, this.CanUpdateReferenceType);
            this.ToggleAttackCommand = new DelegateCommand<object>(this.ToggleAttack, this.CanToggleAttack);
            this.ToggleDirectionalFxCommand = new DelegateCommand<object>(this.ToggleAttack, this.CanToggleAttack);
            this.ConfigureAttackAnimationCommand = new DelegateCommand<object>(delegate(object state) { DemoingType = (AnimationType)state; this.ConfigureAttackAnimation(); }, this.CanConfigureAttackAnimation);
            this.ConfigureUnitPauseCommand = new DelegateCommand<object>(delegate(object state) { this.ConfigureUnitPause(); }, this.CanConfigureUnitPause);
        }

        #endregion

        #region Methods

        #region Attack Consistency

        private void AttackInitiated(Tuple<Character, Attack> tuple)
        {
            this.UpdateCommandsAndControls();
        }

        private void AttackEnded(object state)
        {
            this.UpdateCommandsAndControls();
        }

        private void UpdateCommandsAndControls()
        {
            this.SaveAbilityCommand.RaiseCanExecuteChanged();
            this.SaveSequenceCommand.RaiseCanExecuteChanged();
            this.AddAnimationElementCommand.RaiseCanExecuteChanged();
            this.RemoveAnimationCommand.RaiseCanExecuteChanged();
            this.DemoAnimatedAbilityCommand.RaiseCanExecuteChanged();
            this.DemoAnimationCommand.RaiseCanExecuteChanged();
            this.CloneAnimationCommand.RaiseCanExecuteChanged();
            this.CutAnimationCommand.RaiseCanExecuteChanged();
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
            this.UpdateReferenceTypeCommand.RaiseCanExecuteChanged();
            this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            this.ConfigureUnitPauseCommand.RaiseCanExecuteChanged();
            OnPropertyChanged("CanEditAbilityOptions");
        }

        #endregion

        #region Update Selected Animation

        private void UpdateSelectedAnimation(object state)
        {
            if (state != null) // Update selection
            {
                if (!isUpdatingCollection)
                {
                    IAnimationElement parentAnimationElement;
                    Object selectedAnimationElement = Helper.GetCurrentSelectedAnimationInAnimationCollection(state, out parentAnimationElement);
                    if (selectedAnimationElement != null && selectedAnimationElement is IAnimationElement) // Only update if something is selected
                    {
                        this.SelectedAnimationParent = parentAnimationElement;
                        this.SelectedAnimationElement = selectedAnimationElement as IAnimationElement;
                        this.SetCurrentSequenceAnimation();
                        this.SetCurrentReferenceAbility();
                        this.SetCurrentPauseElement();
                        this.SetCurrentFxElement();
                    }
                    else if (selectedAnimationElement == null && (this.CurrentAbility == null || this.CurrentAbility.AnimationElements.Count == 0))
                    {
                        this.SelectedAnimationElement = null;
                        this.SelectedAnimationParent = null;
                        this.IsSequenceAbilitySelected = false;
                        this.IsReferenceAbilitySelected = false;
                        this.IsPauseElementSelected = false;
                        this.IsFxElementSelected = false;
                        this.CurrentSequenceElement = null;
                        this.CurrentReferenceElement = null;
                        this.CurrentPauseElement = null;
                        this.CurrentFxElement = null;
                    }
                }
                else
                    this.lastAnimationElementsStateToUpdate = state;
            }
            else // Unselect
            {
                this.SelectedAnimationElement = null;
                this.SelectedAnimationParent = null;
                this.IsSequenceAbilitySelected = false;
                this.IsReferenceAbilitySelected = false;
                this.IsPauseElementSelected = false;
                this.IsFxElementSelected = false;
                this.CurrentSequenceElement = null;
                this.CurrentReferenceElement = null;
                this.CurrentPauseElement = null;
                this.CurrentFxElement = null;
                OnAnimationAdded(null, null);
            }
        }

        private void LockModelAndMemberUpdate(bool isLocked)
        {
            this.isUpdatingCollection = isLocked;
            if (!isLocked)
                this.UpdateAnimationElementTree();
        }

        private void UpdateAnimationElementTree()
        {
            // Update character crowd if necessary
            if (this.lastAnimationElementsStateToUpdate != null)
            {
                this.UpdateSelectedAnimation(lastAnimationElementsStateToUpdate);
                this.lastAnimationElementsStateToUpdate = null;
            }
        }

        private void SetCurrentSequenceAnimation()
        {
            if (this.SelectedAnimationElement is SequenceElement)
            {
                this.IsSequenceAbilitySelected = true;
                this.CurrentSequenceElement = this.SelectedAnimationElement as SequenceElement;
            }
            else if (this.SelectedAnimationParent is SequenceElement)
            {
                this.IsSequenceAbilitySelected = true;
                this.CurrentSequenceElement = this.SelectedAnimationParent as SequenceElement;
            }
            else
            {
                this.IsSequenceAbilitySelected = false;
                this.CurrentSequenceElement = null;
            }
        }

        private void SetCurrentReferenceAbility()
        {
            if (this.SelectedAnimationElement is ReferenceAbility)
            {
                this.CurrentReferenceElement = this.SelectedAnimationElement as ReferenceAbility;
                this.IsReferenceAbilitySelected = true;
                //this.LoadReferenceResource();
            }
            else
            {
                this.CurrentReferenceElement = null;
                this.IsReferenceAbilitySelected = false;
            }
        }

        private void SetCurrentPauseElement()
        {
            if (this.SelectedAnimationElement is PauseElement)
            {
                this.CurrentPauseElement = this.SelectedAnimationElement as PauseElement;
                this.IsPauseElementSelected = true;
            }
            else
            {
                this.CurrentPauseElement = null;
                this.IsPauseElementSelected = false;
            }
        }
        private void SetCurrentFxElement()
        {
            if (this.SelectedAnimationElement is FXEffectElement)
            {
                this.CurrentFxElement = this.SelectedAnimationElement as FXEffectElement;
                this.IsFxElementSelected = true;
            }
            else
            {
                this.CurrentFxElement = null;
                this.IsFxElementSelected = false;
            }
        }
        #endregion

        #region Load Animated Ability
        private void LoadAnimatedAbility(Tuple<AnimatedAbility, Character> tuple)
        {
            this.InitializeAnimationElementSelections();
            this.IsShowingAbilityEditor = true;
            this.CurrentAttackAbility = tuple.Item1 as Attack;
            this.CurrentAbility = this.CurrentAttackAbility as AnimatedAbility;
            this.Owner = tuple.Item2 as Character;
            //if (this.CurrentAttackAbility.IsAttack)
            {
                this.IsAttackSelected = true;
                this.IsHitSelected = false;
            }
            LoadOwnerIdentitiesAsResources();
            referenceAbilitiesCVS = null;
            this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }
        private void UnloadAbility(object state = null)
        {
            this.CurrentAttackAbility = null;
            //this.Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            this.Owner = null;
            this.IsShowingAbilityEditor = false;
        }

        #endregion

        #region Rename
        private void EnterAbilityEditMode(object state)
        {
            this.OriginalName = CurrentAttackAbility.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelAbilityEditMode(object state)
        {
            CurrentAttackAbility.Name = this.OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitAbilityRename(object state)
        {
            if (this.OriginalName != null)
            {
                string updatedName = Helper.GetTextFromControlObject(state);

                bool duplicateName = false;
                if (updatedName != this.OriginalName)
                    duplicateName = this.Owner.AnimatedAbilities.ContainsKey(updatedName);

                if (!duplicateName)
                {
                    RenameAbility(updatedName);
                    OnEditModeLeave(state, null);
                    this.SaveAbility();
                    this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
                }
                else
                {
                    messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Ability", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.CancelAbilityEditMode(state);
                }
            }
        }

        private void RenameAbility(string updatedName)
        {
            if (this.OriginalName == updatedName)
            {
                OriginalName = null;
                return;
            }
            CurrentAttackAbility.Name = updatedName;
            Owner.AnimatedAbilities.UpdateKey(OriginalName, updatedName);

            if (CurrentAttackAbility.IsAttack)
            {
                CurrentAttackAbility.OnHitAnimation.Name = updatedName + " - OnHit";
            }

            OriginalName = null;
        }

        private bool CanEnterAnimationElementEditMode(object state)
        {
            return this.SelectedAnimationElement is PauseElement;
        }

        private void EnterAnimationElementEditMode(object state)
        {
            this.OriginalAnimationDisplayName = (this.SelectedAnimationElement as AnimationElement).DisplayName;
            if (this.SelectedAnimationElement is PauseElement)
                this.EditableAnimationDisplayName = (this.SelectedAnimationElement as PauseElement).Time.ToString();
            OnEditModeEnter(state, null);
        }

        private void CancelAnimationElementEditMode(object state)
        {
            (this.SelectedAnimationElement as AnimationElement).DisplayName = this.OriginalAnimationDisplayName;
            this.OriginalAnimationDisplayName = "";
            OnEditModeLeave(state, null);
        }
        private void SubmitAnimationElementRename(object state)
        {
            if (this.SelectedAnimationElement is PauseElement && this.OriginalAnimationDisplayName != "") // Original Display Name empty means we already cancelled the rename
            {
                string pausePeriod = Helper.GetTextFromControlObject(state);
                int period;
                if (!Int32.TryParse(pausePeriod, out period))
                    pausePeriod = "1";
                else
                    (this.SelectedAnimationElement as PauseElement).Time = period;

                (this.SelectedAnimationElement as PauseElement).DisplayName = "Pause " + pausePeriod.ToString();
                this.OriginalAnimationDisplayName = "";
                OnEditModeLeave(state, null);
                this.SaveAbility();
            }
        }
        #endregion

        #region Add Animation Element

        private bool CanAddAnimationElement(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void AddAnimationElement()
        {
            if (this.CurrentAbility == null)
                this.CurrentAbility = new AnimatedAbility("Ability");

            AnimationElementType animationType = EditingAnimationType;
            animationType = EditingAnimationType; // Poor fix. Will need to change this mechanism later.

            AnimationElement animationElement = this.GetAnimationElement(animationType);
            this.AddAnimationElement(animationElement);
            OnAnimationAdded(animationElement, null);
            ChangeFXBehaviorWhenIdentityElementPresent();
            this.SaveAbility();
            this.SelectedAnimationElement = animationElement;
        }

        private void AddAnimationElement(AnimationElement animationElement)
        {
            int order = GetAppropriateOrderForAnimationElement();
            if (!this.IsSequenceAbilitySelected)
                this.CurrentAbility.AddAnimationElement(animationElement, order);
            else
            {
                if (this.SelectedAnimationElement is SequenceElement)
                    (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(animationElement, order);
                else
                    (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(animationElement, order);
            }
            this.CloneAnimationCommand.RaiseCanExecuteChanged();
        }

        private AnimationElement GetAnimationElement(AnimationElementType abilityType)
        {
            AnimationElement animationElement = null;
            List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            string fullName = "";
            switch (abilityType)
            {
                case AnimationElementType.Movement:
                    animationElement = new MOVElement("", "");
                    AnimationResource moveResource = Helper.GetUISettings("Ability_MoveResource") as AnimationResource;
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.Movement, flattenedList);
                    if (moveResource == null)
                    {
                        animationElement.DisplayName = fullName;
                    }
                    else
                    {
                        animationElement.Resource = moveResource;
                        animationElement.DisplayName = GetDisplayNameFromResourceName(animationElement.Resource);
                    }
                    animationElement.Name = fullName;

                    break;
                case AnimationElementType.FX:
                    animationElement = new FXEffectElement("", "");
                    AnimationResource fxResource = Helper.GetUISettings("Ability_FxResource") as AnimationResource;
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.FX, flattenedList);
                    if (fxResource == null)
                    {
                        animationElement.DisplayName = fullName;
                    }
                    else
                    {
                        animationElement.Resource = fxResource;
                        animationElement.DisplayName = GetDisplayNameFromResourceName(animationElement.Resource);
                    }

                    animationElement.Name = fullName;
                    break;
                case AnimationElementType.Sound:
                    animationElement = new SoundElement("", "");
                    AnimationResource soundResource = Helper.GetUISettings("Ability_SoundResource") as AnimationResource;
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.Sound, flattenedList);
                    if (soundResource == null)
                    {
                        animationElement.DisplayName = fullName;
                    }
                    else
                    {
                        animationElement.Resource = soundResource;
                        animationElement.DisplayName = GetDisplayNameFromResourceName(animationElement.Resource);
                    }

                    animationElement.Name = fullName;
                    break;
                case AnimationElementType.Sequence:
                    animationElement = new SequenceElement("");
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.Sequence, flattenedList);
                    animationElement.Name = fullName;
                    (animationElement as SequenceElement).SequenceType = AnimationSequenceType.And;
                    animationElement.DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                    break;
                case AnimationElementType.Pause:
                    animationElement = new PauseElement("", 1);
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.Pause, flattenedList);
                    animationElement.Name = fullName;
                    animationElement.DisplayName = "Pause " + (animationElement as PauseElement).Time.ToString();
                    break;
                case AnimationElementType.Reference:
                    animationElement = new ReferenceAbility("", null);
                    AnimationResource refResource = Helper.GetUISettings("Ability_ReferenceResource") as AnimationResource;
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.Reference, flattenedList);
                    if (refResource == null)
                    {
                        animationElement.DisplayName = fullName;
                    }
                    else
                    {
                        animationElement.Resource = refResource;
                        animationElement.DisplayName = GetDisplayNameFromResourceName(animationElement.Resource);
                    }
                    animationElement.Name = fullName;
                    //this.LoadReferenceResource();
                    break;
                case AnimationElementType.LoadIdentity:
                    animationElement = new IdentityElement("", null);
                    AnimationResource identityResource = Helper.GetUISettings("Ability_IdentityResource") as AnimationResource;
                    fullName = AnimatedAbility.GetAppropriateAnimationName(AnimationElementType.LoadIdentity, flattenedList);
                    if (identityResource == null)
                    {
                        animationElement.DisplayName = fullName;
                    }
                    else
                    {
                        animationElement.Resource = identityResource;
                        animationElement.DisplayName = GetDisplayNameFromResourceName(animationElement.Resource);
                    }
                    animationElement.Name = fullName;
                    break;
            }

            return animationElement;
        }
        private int GetAppropriateOrderForAnimationElement()
        {
            int order = 0;
            if (this.SelectedAnimationParent == null)
            {
                if (this.SelectedAnimationElement == null)
                {
                    order = this.CurrentAbility.LastOrder + 1;
                }
                else if (this.SelectedAnimationElement is SequenceElement)
                {
                    order = (this.SelectedAnimationElement as SequenceElement).LastOrder + 1;
                }
                else
                {
                    order = this.SelectedAnimationElement.Order + 1;
                    //order = prevOrder + 1;
                    //foreach (var element in this.CurrentAbility.AnimationElements.Where(e => e.Order > prevOrder))
                    //    element.Order += 1;
                }
            }
            else
            {
                if (this.SelectedAnimationParent is SequenceElement)
                {
                    var seq = this.SelectedAnimationParent as SequenceElement;
                    if (this.SelectedAnimationElement != null)
                        order = this.SelectedAnimationElement.Order + 1;
                    else
                        order = seq.LastOrder + 1;
                }
            }
            return order;
        }


        #endregion

        #region Update Fx Play on Top if Identity element present before

        private void ChangeFXBehaviorWhenIdentityElementPresent()
        {
            var animationElements = this.CurrentAbility.GetFlattenedAnimationList();

            foreach (FXEffectElement fxElement in animationElements.Where(e => e.Type == AnimationElementType.FX))
            {
                fxElement.PlayOnTopOfIdentityName = null;
                if (animationElements.Any(e => e.Type == AnimationElementType.LoadIdentity))
                {
                    int fxIndex = animationElements.IndexOf(fxElement);
                    var identityElement = animationElements.LastOrDefault(e => e.Type == AnimationElementType.LoadIdentity && animationElements.IndexOf(e) < fxIndex) as IdentityElement;
                    if (identityElement != null)
                    {
                        int identityIndex = animationElements.IndexOf(identityElement);
                        if (!animationElements.Any(e => e.Type == AnimationElementType.FX && animationElements.IndexOf(e) < fxIndex && animationElements.IndexOf(e) > identityIndex))
                        {
                            fxElement.PlayOnTopOfIdentityName = identityElement.Identity.Surface;
                        }
                    }
                }
            }
        }

        #endregion

        #region Change Persistence

        private void ChangePersistence(object state)
        {
            if (!IsAnimationElementSelected)
            {
                foreach (var element in CurrentAbility.AnimationElements)
                    element.Persistent = CurrentAbility.Persistent;
            }
            else
            {
                if (SelectedAnimationElement.Persistent && !CurrentAbility.Persistent)
                    CurrentAbility.Persistent = true;
            }
            SaveAbility();
        }

        #endregion

        #region Change Play with Next

        private void ChangePlayWithNext(object state)
        {
            // first check if play with next is selected for an FX
            if (SelectedAnimationElement != null && SelectedAnimationElement is FXEffectElement)
            {
                var currentFxOrder = SelectedAnimationElement.Order;
                if (currentFxOrder > 0)
                {
                    // check if there is a fx after the current one
                    var nextFxElement = this.CurrentAbility.AnimationElements.FirstOrDefault(a => a.Order > currentFxOrder && a is FXEffectElement);
                    if (nextFxElement != null)
                    {
                        // now check if this fx is indeed intended to be played on top of the current fx
                        var nextFxOrder = nextFxElement.Order;
                        // see if nothing comes between these two fxs except for a pause
                        var otherAnimationElementExists = this.CurrentAbility.AnimationElements.FirstOrDefault(a => !(a is PauseElement) && a.Order > currentFxOrder && a.Order < nextFxOrder) != null;
                        if (!otherAnimationElementExists)
                        {
                            FXEffectElement fxElement = nextFxElement as FXEffectElement;
                            fxElement.PlayOnTopOfPreviousFx = SelectedAnimationElement.PlayWithNext;
                        }
                    }
                }
            }
            this.SaveAbility();
        }

        #endregion

        #region Save Ability

        private bool CanSaveAbility(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveResources()
        {
            this.resourceRepository.SaveMoveResources(this.movResources.ToList());
            this.resourceRepository.SaveFXResources(this.fxResources.ToList());
            this.resourceRepository.SaveSoundResources(this.soundResources.ToList());
        }

        private void SaveUISettingsForResouce(AnimationElement element, AnimationResource resource)
        {
            switch (element.Type)
            {
                case AnimationElementType.Movement:
                    Helper.SaveUISettings("Ability_MoveResource", resource);
                    break;
                case AnimationElementType.FX:
                    Helper.SaveUISettings("Ability_FxResource", resource);
                    break;
                case AnimationElementType.Sound:
                    Helper.SaveUISettings("Ability_SoundResource", resource);
                    break;
                case AnimationElementType.Reference:
                    Helper.SaveUISettings("Ability_ReferenceResource", resource);
                    break;
            }
        }

        private void SaveAbility()
        {
            this.eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
        }

        #endregion

        #region Load Resources
        private void LoadResources(object state)
        {
            List<AnimationResource> moveResourceCollection = this.resourceRepository.GetMoveResources();
            movResources = new ObservableCollection<AnimationResource>(moveResourceCollection.OrderBy(x => x, new AnimationResourceComparer()));
            MOVResources = new ReadOnlyObservableCollection<AnimationResource>(movResources);

            List<AnimationResource> fxResourceCollection = this.resourceRepository.GetFXResources();
            fxResources = new ObservableCollection<AnimationResource>(fxResourceCollection.OrderBy(x => x, new AnimationResourceComparer()));
            FXResources = new ReadOnlyObservableCollection<AnimationResource>(fxResources);

            List<AnimationResource> soundResourceCollection = this.resourceRepository.GetSoundResources();
            soundResources = new ObservableCollection<AnimationResource>(soundResourceCollection.OrderBy(x => x, new AnimationResourceComparer()));
            SoundResources = new ReadOnlyObservableCollection<AnimationResource>(soundResources);

            movResourcesCVS = new CollectionViewSource();
            movResourcesCVS.Source = MOVResources;
            MOVResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("MOVResourcesCVS");

            fxResourcesCVS = new CollectionViewSource();
            fxResourcesCVS.Source = FXResources;
            fxResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("FXResourcesCVS");

            soundResourcesCVS = new CollectionViewSource();
            soundResourcesCVS.Source = SoundResources;
            soundResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("SoundResourcesCVS");

            // publish events to retrieve reference resources and identities - commented out as we'd load them everytime
            //this.eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null);
            //this.eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Publish(null);
        }

        private void LoadReferenceResource(ObservableCollection<AnimatedAbility> abilityCollection)
        {
            if (referenceAbilitiesCVS == null)
            {
                referenceAbilitiesCVS = new CollectionViewSource();
                var refAbilityCollection = abilityCollection.Select((x) => { return new AnimationResource(x, x.Name); }).ToList();
                foreach(var attackObj in abilityCollection.Where(a => a.IsAttack))
                {
                    Attack attack = attackObj as Attack;
                    attack.OnHitAnimation.Name = attack.Name + " - OnHit";
                    if(attack.OnHitAnimation.Owner == null)
                    { 
                        attack.OnHitAnimation.Owner = attack.Owner;
                    }
                    refAbilityCollection.Add(new AnimationResource(attack.OnHitAnimation, attack.OnHitAnimation.Name));
                }
                refAbilityCollection = refAbilityCollection.Where(a => !(a.Reference != null && a.Reference.AnimationElements.Count == 1 && a.Reference.AnimationElements[0] is ReferenceAbility)).OrderBy(x => x, new ReferenceAbilityResourceComparer()).ToList();
                referenceAbilitiesCVS.Source = new ObservableCollection<AnimationResource>(refAbilityCollection);
                referenceAbilitiesCVS.View.Filter += ResourcesCVS_Filter;
            }
            else
            {
                var updatedAbilityResources = abilityCollection.Select((x) => { return new AnimationResource(x, x.Name); }).ToList();
                foreach (var attackObj in abilityCollection.Where(a => a.IsAttack))
                {
                    Attack attack = attackObj as Attack;
                    updatedAbilityResources.Add(new AnimationResource(attack.OnHitAnimation, attack.OnHitAnimation.Name));
                }
                var currentAbilityResources = referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>;
                var addedResources = updatedAbilityResources.Where(a => currentAbilityResources.Where(ca => ca.Name == a.Name && ca.Reference.Owner != null && a.Reference.Owner != null && ca.Reference.Owner.Name == a.Reference.Owner.Name).FirstOrDefault() == null);
                if (addedResources.Count() > 0)
                {
                    foreach (var addedResource in addedResources)
                    {
                        (referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>).Add(addedResource);
                    }
                }
                else
                {
                    var deletedResources = new List<AnimationResource>(currentAbilityResources.Where(ca => updatedAbilityResources.Where(a => (a.Name == ca.Name || ca.Name == (a.Name + " - OnHit")) && ca.Reference.Owner != null && a.Reference.Owner != null && ca.Reference.Owner.Name == a.Reference.Owner.Name).FirstOrDefault() == null));
                    if (deletedResources.Count() > 0)
                    {
                        foreach (var deletedResource in deletedResources)
                        {
                            var resourceToDelete = (referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>).First(ar => ar.Name == deletedResource.Name && ar.Reference.Owner != null && deletedResource.Reference.Owner != null && ar.Reference.Owner.Name == deletedResource.Reference.Owner.Name);
                            (referenceAbilitiesCVS.Source as ObservableCollection<AnimationResource>).Remove(resourceToDelete);
                        }
                    }
                }
            }
            OnPropertyChanged("ReferenceAbilitiesCVS");
        }

        private void LoadIdentityResource(ObservableCollection<Identity> identities)
        {
            if (identityResourcesCVS == null)
            {
                identityResourcesCVS = new CollectionViewSource();
                var identityCollection = identities.Select((x) => { return new AnimationResource(x, x.Name); }).ToList();

                identityCollection = identityCollection.OrderBy(x => x, new IdentityResourceComparer()).ToList();
                identityResourcesCVS.Source = new ObservableCollection<AnimationResource>(identityCollection);
                identityResourcesCVS.View.Filter += ResourcesCVS_Filter;
            }
            else
            {
                var updatedAbilityResources = identities.Select((x) => { return new AnimationResource(x, x.Name); }).ToList();
                var currentAbilityResources = identityResourcesCVS.Source as ObservableCollection<AnimationResource>;
                var addedResources = updatedAbilityResources.Where(a => currentAbilityResources.Where(ca => ca.Name == a.Name && ca.Identity != null && a.Identity != null 
                && ca.Identity.Name == a.Identity.Name).FirstOrDefault() == null);
                if (addedResources.Count() > 0)
                {
                    foreach (var addedResource in addedResources)
                    {
                        (identityResourcesCVS.Source as ObservableCollection<AnimationResource>).Add(addedResource);
                    }
                }
                else
                {
                    var deletedResources = new List<AnimationResource>(currentAbilityResources.Where(ca => updatedAbilityResources.Where(a => (a.Name == ca.Name) 
                    && ca.Identity != null && a.Identity != null && ca.Identity.Name == a.Identity.Name).FirstOrDefault() == null));
                    if (deletedResources.Count() > 0)
                    {
                        foreach (var deletedResource in deletedResources)
                        {
                            var resourceToDelete = (identityResourcesCVS.Source as ObservableCollection<AnimationResource>).First(ar => ar.Name == deletedResource.Name 
                            && ar.Identity != null && deletedResource.Identity != null && ar.Identity.Name == deletedResource.Identity.Name);
                            (identityResourcesCVS.Source as ObservableCollection<AnimationResource>).Remove(resourceToDelete);
                        }
                    }
                }
            }
            OnPropertyChanged("IdentityResourcesCVS");
        }

        private void LoadOwnerIdentitiesAsResources()
        {
            List<AnimationResource> identityCollection = this.Owner.AvailableIdentities.Select((x) => { return new AnimationResource(x, x.Name); }).ToList();
            identityCollection = identityCollection.OrderBy(x => x, new IdentityResourceComparer()).ToList();
            identityResourcesCVS = new CollectionViewSource();
            identityResourcesCVS.Source = new ObservableCollection<AnimationResource>(identityCollection);
            identityResourcesCVS.View.Filter += ResourcesCVS_Filter;
            OnPropertyChanged("IdentityResourcesCVS");
        }

        private bool ResourcesCVS_Filter(object item)
        {
            AnimationResource animationRes = item as AnimationResource;
            if (animationRes.Reference == this.CurrentAbility)
                return false;
            if (animationRes.Reference != null)
            {
                AnimatedAbility reference = animationRes.Reference;
                if (reference != null && reference.AnimationElements != null && reference.AnimationElements.Count > 0)
                {
                    if (reference.AnimationElements.Contains(this.CurrentAbility))
                        return false;
                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(reference.AnimationElements.ToList());
                    if (elementList.Where((an) => { return an.Type == AnimationElementType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                        return false;
                }
                ////Check if the referenced ability contains the parent ability
                //if (reference.AnimationElements.Contains(this.CurrentAbility))
                //    return false;
                ////Check if inside the referenced ability there's any reference to the current ability
                //if (reference.AnimationElements.Where((an) => { return an.Type == AnimationType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                //    return false;
            }
            if (string.IsNullOrWhiteSpace(Filter))
            {
                return true;
            }


            if (SelectedAnimationElement != null && SelectedAnimationElement.Resource == animationRes)
            {
                return true;
            }
            // Replace non-alphanumeric characters with empty string
            Regex rgx = new Regex("[^a-zA-Z0-9 ]");
            string filter = rgx.Replace(Filter, "");

            bool caseReferences = false;
            if (animationRes.Reference != null && animationRes.Reference.Owner != null)
            {
                caseReferences = new Regex(filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Name) || new Regex(filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Reference.Owner.Name);
            }
            return new Regex(filter, RegexOptions.IgnoreCase).IsMatch(animationRes.TagLine) || new Regex(filter, RegexOptions.IgnoreCase).IsMatch(animationRes.Name) || caseReferences;
        }

        #endregion

        #region Save Sequence

        private bool CanSaveSequence(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void SaveSequence(object state)
        {
            if (this.CurrentSequenceElement != null)
            {
                this.CurrentSequenceElement.DisplayName = "Sequence: " + this.CurrentSequenceElement.SequenceType.ToString();
            }
            this.SaveAbility();
        }
        #endregion

        #region Remove Animation

        private bool CanRemoveAnimation(object state)
        {
            return this.SelectedAnimationElement != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void RemoveAnimation()
        {
            this.LockModelAndMemberUpdate(true);
            if (this.SelectedAnimationParent != null)
            {
                this.DeleteAnimationElementFromParentElementByName(this.SelectedAnimationParent, this.SelectedAnimationElement.Name);
            }
            else
            {
                this.CurrentAbility.RemoveAnimationElement(this.SelectedAnimationElement.Name);
            }
            this.SaveAbility();
            this.ChangeFXBehaviorWhenIdentityElementPresent();
            this.LockModelAndMemberUpdate(false);
        }

        private void DeleteAnimationElementFromParentElementByName(IAnimationElement parent, string nameOfDeletingAnimation)
        {
            SequenceElement parentSequenceElement = parent as SequenceElement;
            if (parentSequenceElement != null && parentSequenceElement.AnimationElements.Count > 0)
            {
                //var anim = parentSequenceElement.AnimationElements.Where(a => a.Name == nameOfDeletingAnimation).FirstOrDefault();
                parentSequenceElement.RemoveAnimationElement(nameOfDeletingAnimation);
                if (parentSequenceElement.AnimationElements.Count == 0 && this.SelectedAnimationElementRoot != null
                    && parentSequenceElement.Name == this.SelectedAnimationElementRoot.Name)
                {
                    this.SelectedAnimationElementRoot = null;
                }
            }
            OnExpansionUpdateNeeded(parent, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Delete });
        }

        #endregion

        #region Utility Methods

        private List<AnimationElement> GetFlattenedAnimationList(List<AnimationElement> animationElementList)
        {
            List<AnimationElement> _list = new List<AnimationElement>();
            foreach (AnimationElement animationElement in animationElementList)
            {
                if (animationElement is SequenceElement)
                {
                    SequenceElement sequenceElement = (animationElement as SequenceElement);
                    if (sequenceElement.AnimationElements != null && sequenceElement.AnimationElements.Count > 0)
                        _list.AddRange(GetFlattenedAnimationList(sequenceElement.AnimationElements.ToList()));
                }
                _list.Add(animationElement);
            }
            return _list;
        }

        private string GetDisplayNameFromResourceName(string resourceName)
        {
            return Path.GetFileNameWithoutExtension(resourceName);
        }

        private IAnimationElement GetNextAnimationElement(IAnimationElement selectedAnimationElement)
        {
            if (selectedAnimationElement == null || currentAbility == null || currentAbility.AnimationElements == null)
                return null;

            IAnimationElement nextElement = null;
            if (SelectedAnimationParent != null && SelectedAnimationParent is SequenceElement)
            {
                SequenceElement seqElement = this.SelectedAnimationParent as SequenceElement;
                if(seqElement.SequenceType == AnimationSequenceType.And)
                {
                    if (selectedAnimationElement.Order != seqElement.LastOrder)
                        nextElement = seqElement.AnimationElements.Where(x => { return x.Order == selectedAnimationElement.Order + 1; }).FirstOrDefault();
                }
            }
            else
            {
                if (selectedAnimationElement.Order != currentAbility.LastOrder)
                    nextElement = currentAbility.AnimationElements.Where(x => { return x.Order == selectedAnimationElement.Order + 1; }).FirstOrDefault();
            }
            return nextElement;
        }
        #endregion

        #region Demo Animation

        private bool CanDemoAnimation(object state)
        {
            return !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void DemoAnimatedAbility()
        {
            Action d = delegate ()
            {
                IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                WindowsUtilities.SetForegroundWindow(winHandle);
                Character currentTarget = GetCurrentTarget();
                currentTarget.Target(false);
                currentTarget.ActiveIdentity.RenderWithoutAnimation(target: currentTarget);
                this.PlayAnimatedAbility(currentTarget, this.CurrentAbility);
                //this.CurrentAbility.Play(Target: currentTarget);
            };
            AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 5);
            adex.ExecuteAsyncDelegate();
        }

        private void DemoAnimation()
        {
            Action d = delegate ()
            {
                IntPtr winHandle = WindowsUtilities.FindWindow("CrypticWindow", null);
                WindowsUtilities.SetForegroundWindow(winHandle);
                Character currentTarget = GetCurrentTarget();
                if (this.SelectedAnimationElement != null)
                {
                    this.SelectedAnimationElement.Play(Target: currentTarget);
                }
            };
            AsyncDelegateExecuter adex = new AsyncDelegateExecuter(d, 5);
            adex.ExecuteAsyncDelegate();
        }

        private Character GetCurrentTarget()
        {
            Character currentTarget = null;
            if (!this.CurrentAbility.PlayOnTargeted)
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
            return currentTarget;
        }

        private void SpawnAndTargetOwnerCharacter()
        {
            if (this.Owner != null) // Will be null if the editor isn't loaded yet
            {
                if (!this.Owner.HasBeenSpawned)
                {
                    Crowds.CrowdMemberModel member = this.Owner as Crowds.CrowdMemberModel;
                    if (member.RosterCrowd == null)
                        this.eventAggregator.GetEvent<AddToRosterThruCharExplorerEvent>().Publish(new Tuple<Crowds.CrowdMemberModel, Crowds.CrowdModel>(member, member.RosterCrowd as Crowds.CrowdModel));
                    member.Spawn(false);
                }
                this.Owner.Target(false);
            }
        }

        private void PlayAnimatedAbility(Tuple<Character, AnimatedAbility> tuple)
        {
            Character target = tuple.Item1;
            AnimatedAbility ability = tuple.Item2;
            PlayAnimatedAbility(target, ability);
        }

        private void PlayAnimatedAbility(Character targetCharacter = null, AnimatedAbility ability = null)
        {
            Character target = targetCharacter ?? ability.Owner;
            if (target != null && ability != null)
            {
                if (Helper.GlobalVariables_IntegrateWithHCS && !ability.IsAttack && ability.Name != Constants.SWEEP_ABILITY_NAME)
                    this.hcsIntegrator.PlaySimpleAbility(target, ability);
                ability.Play(Target: target);
                if (ability.Name == Constants.SWEEP_ABILITY_NAME)
                {
                    target.DisableSimpleAbilities(new List<AnimatedAbility> { Helper.GlobalDefaultSweepAbility });
                    List<Attack> attacksToExclude = target.AnimatedAbilities.Where(aa => aa.IsAttack && !(aa as Attack).IsAutoFire && !(aa as Attack).IsAreaEffect).Cast<Attack>().ToList();
                    target.DisableAttacks(attacksToExclude);
                    this.currentSweeper = target; 
                }
            }
        }

        private void StopAnimatedAbility(Tuple<Character, AnimatedAbility> tuple)
        {
            Character target = tuple.Item1;
            AnimatedAbility ability = tuple.Item2;
            if (ability.Name == Constants.SWEEP_ABILITY_NAME)
            {
                this.eventAggregator.GetEvent<ExecuteSweepAttackEvent>().Publish(this.currentSweeper);
            }
            else
                StopAnimatedAbility(target, ability);
        }

        private void StopAnimatedAbility(Character target = null, AnimatedAbility ability = null)
        {
            Character Target = target ?? ability.Owner;
            if (Target != null && ability != null)
            {
                if (Helper.GlobalVariables_IntegrateWithHCS && !ability.IsAttack && ability.Persistent && ability.IsActive)
                    this.hcsIntegrator.PlaySimpleAbility(Target, ability);
                ability.Stop(Target: Target);
            }
        }
        #endregion

        #region Reset Sweep

        private void ResetSweep(object state)
        {
            if(this.currentSweeper != null)
            {
                var sweepAbility = Helper.GlobalDefaultSweepAbility;
                 if(sweepAbility != null && sweepAbility.IsActive)
                {
                    StopAnimatedAbility(this.currentSweeper, sweepAbility);
                    this.currentSweeper.RefreshAbilitiesActivationEligibility();
                    this.currentSweeper = null;
                }
            }
        }

        #endregion

        #region Clone Animation

        private bool CanCloneAnimation(object state)
        {
            return (this.SelectedAnimationElement != null || (this.SelectedAnimationElement == null && this.CurrentAbility != null && this.CurrentAbility.AnimationElements != null && this.CurrentAbility.AnimationElements.Count > 0)) && !Helper.GlobalVariables_IsPlayingAttack;
        }
        private void CloneAnimation()
        {
            if (this.SelectedAnimationElement != null)
                Helper.GlobalClipboardObject = this.SelectedAnimationElement; // any animation element
            else
                Helper.GlobalClipboardObject = this.CurrentAbility; // Sequence element
            Helper.GlobalClipboardAction = ClipboardAction.Clone;
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Cut Animation
        private bool CanCutAnimation(object state)
        {
            return (this.SelectedAnimationElement != null) && !Helper.GlobalVariables_IsPlayingAttack;
        }
        private void CutAnimation()
        {
            if (this.SelectedAnimationParent != null)
            {
                Helper.GlobalClipboardObject = this.SelectedAnimationElement;
                Helper.GlobalClipboardObjectParent = this.SelectedAnimationParent;
            }
            else
            {
                Helper.GlobalClipboardObject = this.SelectedAnimationElement;
                Helper.GlobalClipboardObjectParent = this.CurrentAbility;
            }
            Helper.GlobalClipboardAction = ClipboardAction.Cut;
            this.PasteAnimationCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region Paste Animation
        private bool CanPasteAnimation(object state)
        {
            bool canPaste = false;
            if (!Helper.GlobalVariables_IsPlayingAttack)
            {
                switch (Helper.GlobalClipboardAction)
                {
                    case ClipboardAction.Clone:
                        if (Helper.GlobalClipboardObject != null)
                        {
                            if (Helper.GlobalClipboardObject is SequenceElement)
                            {
                                SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                                List<AnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
                                if (!(elementList.Where((an) => { return an.Type == AnimationElementType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; })))
                                    canPaste = true;
                            }
                            else if (Helper.GlobalClipboardObject is ReferenceAbility)
                            {
                                ReferenceAbility refAbility = Helper.GlobalClipboardObject as ReferenceAbility;
                                if (refAbility.Reference == this.CurrentAbility)
                                    canPaste = false;
                                else if (refAbility.Reference != null && refAbility.Reference.AnimationElements != null && refAbility.Reference.AnimationElements.Count > 0)
                                {
                                    bool refexists = false;
                                    if (refAbility.Reference.AnimationElements.Contains(this.CurrentAbility))
                                        refexists = true;
                                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
                                    if (elementList.Where((an) => { return an.Type == AnimationElementType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                                        refexists = true;
                                    if (!refexists)
                                        canPaste = true;
                                }
                                else
                                    canPaste = true;
                            }
                            else
                                canPaste = true;
                        }
                        break;
                    case ClipboardAction.Cut:
                        if (Helper.GlobalClipboardObject != null && Helper.GlobalClipboardObjectParent != null)
                        {
                            if (Helper.GlobalClipboardObject is SequenceElement)
                            {
                                SequenceElement seqElement = Helper.GlobalClipboardObject as SequenceElement;
                                List<AnimationElement> elementList = this.GetFlattenedAnimationList(seqElement.AnimationElements.ToList());
                                if (!(elementList.Where((an) => { return an.Type == AnimationElementType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; })))
                                    canPaste = true;
                            }
                            else if (Helper.GlobalClipboardObject is ReferenceAbility)
                            {
                                ReferenceAbility refAbility = Helper.GlobalClipboardObject as ReferenceAbility;
                                if (refAbility.Reference == this.CurrentAbility)
                                    canPaste = false;
                                else if (refAbility.Reference != null && refAbility.Reference.AnimationElements != null && refAbility.Reference.AnimationElements.Count > 0)
                                {
                                    bool refexists = false;
                                    if (refAbility.Reference.AnimationElements.Contains(this.CurrentAbility))
                                        refexists = true;
                                    List<AnimationElement> elementList = this.GetFlattenedAnimationList(refAbility.Reference.AnimationElements.ToList());
                                    if (elementList.Where((an) => { return an.Type == AnimationElementType.Reference; }).Any((an) => { return an.Resource.Reference == this.CurrentAbility; }))
                                        refexists = true;
                                    if (!refexists)
                                        canPaste = true;
                                }
                                else
                                    canPaste = true;
                            }
                            else
                                canPaste = true;
                        }
                        break;
                }
            }
            return canPaste;
        }
        private void PasteAnimation()
        {
            // Lock animation Tree from updating
            this.LockModelAndMemberUpdate(true);
            List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
            switch (Helper.GlobalClipboardAction)
            {
                case ClipboardAction.Clone:
                    {
                        IAnimationElement animationElement = (Helper.GlobalClipboardObject as AnimationElement).Clone() as IAnimationElement;
                        animationElement.Name = AnimatedAbility.GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, null);
                        this.SaveAbility();
                        break;
                    }
                case ClipboardAction.Cut:
                    {
                        SequenceElement parentSequenceElement = Helper.GlobalClipboardObjectParent as SequenceElement;
                        AnimationElement animationElement = Helper.GlobalClipboardObject as AnimationElement;
                        parentSequenceElement.RemoveAnimationElement(animationElement);
                        animationElement.Name = AnimatedAbility.GetAppropriateAnimationName(animationElement.Type, flattenedList);
                        if (animationElement is SequenceElement && string.IsNullOrEmpty((animationElement as AnimationElement).DisplayName))
                            (animationElement as AnimationElement).DisplayName = "Sequence: " + (animationElement as SequenceElement).SequenceType.ToString();
                        this.AddAnimationElement(animationElement as AnimationElement);
                        OnAnimationAdded(animationElement, new CustomEventArgs<bool>() { Value = false });
                        this.SaveAbility();
                        break;
                    }
            }
            if (SelectedAnimationElement != null)
            {
                OnExpansionUpdateNeeded(this.SelectedAnimationElement, new CustomEventArgs<ExpansionUpdateEvent> { Value = ExpansionUpdateEvent.Paste });
            }
            // UnLock character crowd Tree from updating
            this.LockModelAndMemberUpdate(false);
            this.ChangeFXBehaviorWhenIdentityElementPresent();
            Helper.GlobalClipboardObject = null;
            Helper.GlobalClipboardObjectParent = null;
        }

        #endregion

        #region Reference Ability
        private bool CanUpdateReferenceType(object state)
        {
            return this.CurrentReferenceElement != null && this.CurrentReferenceElement.Reference != null && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void UpdateReferenceType(object state)
        {
            if (this.CurrentReferenceElement != null)
            {
                if (this.CurrentReferenceElement.ReferenceType == ReferenceType.Copy)
                {
                    this.LockModelAndMemberUpdate(true);
                    List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                    SequenceElement sequenceElementClone = (this.CurrentReferenceElement.Reference).Clone() as SequenceElement;
                    SequenceElement sequenceElement = this.GetAnimationElement(AnimationElementType.Sequence) as SequenceElement;
                    foreach(var animationElement in sequenceElementClone.AnimationElements)
                    {
                        sequenceElement.AddAnimationElement(animationElement);
                    }
                    int order = this.CurrentReferenceElement.Order;
                    //sequenceElement.Name = AnimatedAbility.GetAppropriateAnimationName(sequenceElement.Type, flattenedList);
                    if (sequenceElement is SequenceElement && string.IsNullOrEmpty((sequenceElement as AnimationElement).DisplayName))
                        (sequenceElement as AnimationElement).DisplayName = "Sequence: " + (sequenceElement as SequenceElement).SequenceType.ToString();
                    this.RemoveAnimation();
                    if (this.SelectedAnimationElement is SequenceElement)
                        (this.SelectedAnimationElement as SequenceElement).AddAnimationElement(sequenceElement, order);
                    else if (this.SelectedAnimationParent is SequenceElement)
                        (this.SelectedAnimationParent as SequenceElement).AddAnimationElement(sequenceElement, order);
                    else
                        this.CurrentAbility.AddAnimationElement(sequenceElement, order);
                    OnAnimationAdded(sequenceElement, null);
                    this.SaveAbility();
                    this.CurrentReferenceElement = null;
                    this.IsReferenceAbilitySelected = false;
                    this.LockModelAndMemberUpdate(false);
                }
            }
        }

        #endregion

        #region Unit Pause

        private bool CanConfigureUnitPause(object state)
        {
            return Helper.GlobalVariables_IsPlayingAttack == false;
        }

        private void ConfigureUnitPause()
        {
            if (this.CurrentPauseElement != null)
            {
                if (this.CurrentPauseElement.IsUnitPause)
                    this.CurrentPauseElement.DisplayName = "Pause 1";
                else
                    this.CurrentPauseElement.DisplayName = "Pause " + this.CurrentPauseElement.Time.ToString();
            }
            this.SaveAbility();
        }

        #endregion

        #region Toggle Attack

        private bool CanToggleAttack(object state)
        {
            return this.IsAttackSelected && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void ToggleAttack(object state)
        {
            this.SaveAbility();
            this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Toggle Directional FX

        private bool CanToggleDirectionalFx(object state)
        {
            return this.IsFxElementSelected && !Helper.GlobalVariables_IsPlayingAttack;
        }

        private void ToggleDirectionalFx(object state)
        {
            this.SaveAbility();
        }

        #endregion

        #region Attack/Defend Animations

        private bool CanConfigureAttackAnimation(object state)
        {
            return this.CurrentAttackAbility != null && this.CurrentAttackAbility.IsAttack && !Helper.GlobalVariables_IsPlayingAttack;
            //bool canConfigureAttack = false;
            //if (!Helper.GlobalVariables_IsPlayingAttack)
            //{
            //    if (DemoingType == AnimationType.Standard)
            //        canConfigureAttack = true;
            //    else
            //        canConfigureAttack = this.CurrentAttackAbility != null && this.CurrentAttackAbility.IsAttack;
            //}

            //return canConfigureAttack;
        }

        private void ConfigureAttackAnimation()
        {
            //if (DemoingType == AnimationType.Standard)
            //{
            //    this.IsAttackSelected = true;
            //    if (this.CurrentAttackAbility.IsAttack)
            //    {
            //        this.IsAttackSelected = true;
            //    }
            //    else
            //    {
            //        this.IsAttackSelected = false;
            //    }
            //    this.IsHitSelected = false;
            //    this.CurrentAbility = this.CurrentAttackAbility;
            //}
            //else
            if (DemoingType == AnimationType.Standard || DemoingType == AnimationType.Attack)
            {
                this.IsAttackSelected = true;
                this.IsHitSelected = false;
                this.CurrentAbility = this.CurrentAttackAbility;
            }
            else if (DemoingType == AnimationType.OnHit)
            {
                this.IsAttackSelected = false;
                this.IsHitSelected = true;
                this.CurrentAbility = this.CurrentAttackAbility.OnHitAnimation;
                this.CurrentAttackAbility.OnHitAnimation.Name = this.CurrentAttackAbility.Name + " - OnHit";
            }
            this.SaveAbility();
            this.ConfigureAttackAnimationCommand.RaiseCanExecuteChanged();
            this.ToggleAttackCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region Move Animation Element
        /// <summary>
        /// This method is used for Drag drop inside the treeview and performs a cut paste under the hood
        /// </summary>
        /// <param name="sourceElement"></param>
        /// <param name="targetElement"></param>
        public void MoveSelectedAnimationElement(SequenceElement targetElementParent, int order)
        {
            AnimationElement sourceElement = null;
            SequenceElement sourceElementParent = null;
            if (this.SelectedAnimationParent != null)
            {
                sourceElement = this.SelectedAnimationElement as AnimationElement;
                sourceElementParent = this.SelectedAnimationParent as SequenceElement;
            }
            else
            {
                sourceElement = this.SelectedAnimationElement as AnimationElement;
                sourceElementParent = this.CurrentAbility;
            }
            SequenceElement destinationElementParent = targetElementParent ?? this.CurrentAbility;
            if (sourceElement != null && sourceElementParent != null)
            {
                List<AnimationElement> flattenedList = GetFlattenedAnimationList(CurrentAbility.AnimationElements.ToList());
                sourceElementParent.RemoveAnimationElement(sourceElement);
                sourceElement.Name = AnimatedAbility.GetAppropriateAnimationName(sourceElement.Type, flattenedList);
                if (sourceElement is SequenceElement && string.IsNullOrEmpty((sourceElement as AnimationElement).DisplayName))
                    (sourceElement as AnimationElement).DisplayName = "Sequence: " + (sourceElement as SequenceElement).SequenceType.ToString();
                destinationElementParent.AddAnimationElement(sourceElement as AnimationElement, order);
                OnAnimationAdded(sourceElement, new CustomEventArgs<bool>() { Value = false });
                this.SaveAbility();
                this.ChangeFXBehaviorWhenIdentityElementPresent();
            }
        }
        /// <summary>
        /// Drag drop between refernce ability grid and animations treview
        /// </summary>
        /// <param name="referenceAbility"></param>
        /// <param name="targetElementParent"></param>
        /// <param name="order"></param>
        public void MoveReferenceAbilityToAnimationElements(AnimatedAbility referenceAbility, SequenceElement targetElementParent, int order)
        {
            SequenceElement destinationElementParent = targetElementParent ?? this.CurrentAbility;
            if (referenceAbility != null)
            {
                AnimationElement animationElement = this.GetAnimationElement(AnimationElementType.Reference);
                (animationElement as ReferenceAbility).Reference = referenceAbility;
                animationElement.DisplayName = GetDisplayNameFromResourceName(referenceAbility.Name);
                destinationElementParent.AddAnimationElement(animationElement, order);
                OnAnimationElementDraggedFromGrid(animationElement, null);
                SaveAbility();
                this.CloneAnimationCommand.RaiseCanExecuteChanged();
            }
        }

        #endregion

        #region Keyboard Hook

        AnimationElementType EditingAnimationType = AnimationElementType.FX;
        AnimationType DemoingType = AnimationType.Standard;
        internal EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (Helper.GlobalVariables_CurrentActiveWindowName == Constants.ABILITY_EDITOR)
            {
                if (inputKey == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.Movement;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.FX;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.Sound;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.Q && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.Sequence;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.R && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.Reference;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.P && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.Pause;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.I && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.LoadIdentity;
                    return AddAnimationElement;
                }
                else if (inputKey == Key.Enter && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return DemoAnimatedAbility;
                }
                else if (inputKey == Key.C && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return CloneAnimation;
                }
                else if (inputKey == Key.X && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return CutAnimation;
                }
                else if (inputKey == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    EditingAnimationType = AnimationElementType.FX;
                    return PasteAnimation;
                }
                else if ((inputKey == Key.OemMinus || inputKey == Key.Subtract || inputKey == Key.Delete) && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    return RemoveAnimation;
                }
                else if (inputKey == Key.Enter && Keyboard.Modifiers == (ModifierKeys.Shift | ModifierKeys.Control))
                {
                    return DemoAnimation;
                }
                else if (inputKey == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    DemoingType = AnimationType.Attack;
                    return ConfigureAttackAnimation;

                }
                else if (inputKey == Key.H && Keyboard.Modifiers == ModifierKeys.Control)
                {
                    DemoingType = AnimationType.OnHit;
                    return ConfigureAttackAnimation;
                } 
            }
            return null;
        }


        #endregion

        #region Misc

        private bool IsAbilityKey(System.Windows.Forms.Keys key)
        {
            return key == System.Windows.Forms.Keys.F
                || key == System.Windows.Forms.Keys.Q
                || key == System.Windows.Forms.Keys.S
                || key == System.Windows.Forms.Keys.R
                || key == System.Windows.Forms.Keys.X
                || key == System.Windows.Forms.Keys.C
                || key == System.Windows.Forms.Keys.V
                || key == System.Windows.Forms.Keys.A
                || key == System.Windows.Forms.Keys.H
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

        #endregion
    }
}
