using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Prism.Events;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System;
using Microsoft.Practices.Prism.Commands;
using Framework.WPF.Services.MessageBoxService;
using Module.Shared.Messages;
using Module.HeroVirtualTabletop.AnimatedAbilities;

using Module.HeroVirtualTabletop.OptionGroups;
using Characters = Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Desktop;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using Library = Module.HeroVirtualTabletop.Library;
using Roster = Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
namespace HeroVTT.Identities
{
    public class IdentityEditorViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IIdentityGameState _gameState;
        private Character _owner;
        private Identity _editedIdentity;
        private Visibility _visibility = Visibility.Collapsed;
        private string _filter;
        private ObservableCollection<string> _models;
        private ObservableCollection<string> _costumes;
        private CollectionViewSource _modelsCVS;
        private CollectionViewSource _costumesCVS;
        private CollectionViewSource _abilitiesCVS;

        public Character Owner
        {
            get { return _owner; }
            set
            {
                _owner = value;
                if (value != null)
                    _owner.PropertyChanged += Owner_PropertyChanged;
                OnPropertyChanged("Owner");
            }
        }

        public Identity EditedIdentity
        {
            get { return _editedIdentity; }
            set
            {
                if (_editedIdentity != null)
                    _editedIdentity.PropertyChanged -= EditedIdentity_PropertyChanged;
                _editedIdentity = value;
                if (_editedIdentity != null)
                    _editedIdentity.PropertyChanged += EditedIdentity_PropertyChanged;
                OnPropertyChanged("EditedIdentity");
            }
        }

        public Visibility Visibility
        {
            get { return _visibility; }
            set
            {
                _visibility = value;
                OnPropertyChanged("Visibility");
            }
        }

        public string Filter
        {
            get { return _filter; }
            set
            {
                _filter = value;
                ModelsCVS.View.Refresh();
                CostumesCVS.View.Refresh();
                if (AbilitiesCVS != null)
                    AbilitiesCVS.View.Refresh();
                OnPropertyChanged("Filter");
            }
        }

        public ObservableCollection<string> Models
        {
            get { return _models; }
            set { _models = value; OnPropertyChanged("Models"); }
        }

        public ObservableCollection<string> Costumes
        {
            get { return _costumes; }
            set { _costumes = value; OnPropertyChanged("Costumes"); }
        }

        public CollectionViewSource ModelsCVS { get { return _modelsCVS; } }
        public CollectionViewSource CostumesCVS { get { return _costumesCVS; } }
        public CollectionViewSource AbilitiesCVS { get { return _abilitiesCVS; } }

        public bool IsDefault
        {
            get { return EditedIdentity != null && EditedIdentity == Owner.DefaultIdentity; }
            set
            {
                if (value)
                    Owner.DefaultIdentity = EditedIdentity;
                else
                    Owner.DefaultIdentity = null;
                OnPropertyChanged("IsDefault");
            }
        }

        public bool CanEditIdentityOptions
        {
            get { return !_gameState.IsPlayingAttack; }
        }

        public string OriginalName { get; set; }

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

        public DelegateCommand<object> CloseEditorCommand { get; private set; }
        public DelegateCommand<object> LoadAbilitiesCommand { get; private set; }
        public DelegateCommand<object> EnterEditModeCommand { get; private set; }
        public DelegateCommand<object> SubmitIdentityRenameCommand { get; private set; }
        public DelegateCommand<object> CancelEditModeCommand { get; private set; }

        public IdentityEditorViewModel(
            IBusyService busyService,
            IUnityContainer container,
            IMessageBoxService messageBoxService,
            IIdentityGameState gameState,
            EventAggregator eventAggregator)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _messageBoxService = messageBoxService;
            _gameState = gameState;
            InitializeCommands();
            CreateModelsViewSource();
            CreateCostumesViewSource();

            eventAggregator.GetEvent<EditIdentityEvent>().Subscribe(LoadIdentity);
            eventAggregator.GetEvent<FinishedAbilityCollectionRetrievalEvent>().Subscribe(CreateAbilitiesViewSource);
            eventAggregator.GetEvent<AttackInitiatedEvent>().Subscribe(_ => OnPropertyChanged("CanEditIdentityOptions"));
            eventAggregator.GetEvent<AttackExecutionsFinishedEvent>().Subscribe(_ => OnPropertyChanged("CanEditIdentityOptions"));
        }

        private void InitializeCommands()
        {
            CloseEditorCommand = new DelegateCommand<object>(UnloadIdentity);
            SubmitIdentityRenameCommand = new DelegateCommand<object>(SubmitIdentityRename);
            EnterEditModeCommand = new DelegateCommand<object>(EnterEditMode);
            CancelEditModeCommand = new DelegateCommand<object>(CancelEditMode);
            LoadAbilitiesCommand = new DelegateCommand<object>(
                _ => _eventAggregator.GetEvent<NeedAbilityCollectionRetrievalEvent>().Publish(null));
        }

        private void Owner_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DefaultIdentity")
                OnPropertyChanged("IsDefault");
        }

        private void LoadIdentity(Tuple<Identity, Character> data)
        {
            UnloadIdentity();
            Filter = null;
            Owner = data.Item2;
            EditedIdentity = data.Item1;
            Owner.AvailableIdentities.CollectionChanged += AvailableIdentities_CollectionChanged;
            Visibility = Visibility.Visible;
            LoadAbilitiesCommand.Execute(null);
            OnPropertyChanged("IsDefault");
        }

        private void UnloadIdentity(object state = null)
        {
            EditedIdentity = null;
            if (Owner != null)
                Owner.AvailableIdentities.CollectionChanged -= AvailableIdentities_CollectionChanged;
            Owner = null;
            Visibility = Visibility.Collapsed;
        }

        private void EnterEditMode(object state)
        {
            OriginalName = EditedIdentity.Name;
            OnEditModeEnter(state, null);
        }

        private void CancelEditMode(object state)
        {
            EditedIdentity.Name = OriginalName;
            OnEditModeLeave(state, null);
        }

        private void SubmitIdentityRename(object state)
        {
            IdentityCommands.TryRename(state, EditedIdentity, Owner, OriginalName, _messageBoxService,
                _eventAggregator, name => OriginalName = name,
                s => { OnEditModeLeave(s, null); }, s => CancelEditMode(s));
        }

        private void RenameIdentity(string updatedName)
        {
            IdentityCommands.ApplyRename(EditedIdentity, Owner, updatedName, OriginalName, _eventAggregator);
            OriginalName = null;
        }

        private void AvailableIdentities_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove
                && e.OldItems.Contains(EditedIdentity))
                UnloadIdentity();
        }

        private void CreateModelsViewSource()
        {
            _models = new ObservableCollection<string>(
                File.ReadAllLines(Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_MODELS_FILENAME))
                    .OrderBy(m => m, new StringValueComparer()));
            _modelsCVS = new CollectionViewSource { Source = Models };
            _modelsCVS.View.Filter += StringsCVS_Filter;
        }

        private void CreateCostumesViewSource()
        {
            _costumes = new ObservableCollection<string>(
                Directory.EnumerateFiles(
                    Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_COSTUMES_FOLDERNAME),
                    "*.costume")
                    .Select(file => Path.GetFileNameWithoutExtension(file))
                    .OrderBy(c => c, new StringValueComparer()));
            _costumesCVS = new CollectionViewSource { Source = Costumes };
            _costumesCVS.View.Filter += StringsCVS_Filter;
        }

        private void CreateAbilitiesViewSource(ObservableCollection<AnimatedAbility> abilities)
        {
            _abilitiesCVS = new CollectionViewSource();
            Attack none = new Attack("None", owner: Owner);
            abilities.Add(none);
            _abilitiesCVS.Source = new ObservableCollection<AnimatedAbility>(
                abilities.Where(an => an.Owner == Owner).OrderBy(a => a.Order));
            _abilitiesCVS.View.Filter += AbilitiesCVS_Filter;
            AnimatedAbility moveTo = EditedIdentity != null ? EditedIdentity.AnimationOnLoad : none;
            _abilitiesCVS.View.MoveCurrentTo(moveTo);
            OnPropertyChanged("AbilitiesCVS");
        }

        private bool AbilitiesCVS_Filter(object item)
        {
            if (string.IsNullOrWhiteSpace(Filter)) return true;
            string strItem = (item as AnimatedAbility).Name;
            if (EditedIdentity != null && EditedIdentity.AnimationOnLoad == item as AnimatedAbility) return true;
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(strItem);
        }

        private bool StringsCVS_Filter(object item)
        {
            if (string.IsNullOrWhiteSpace(Filter)) return true;
            string strItem = item as string;
            if (EditedIdentity != null && EditedIdentity.Surface == strItem) return true;
            return new Regex(Filter, RegexOptions.IgnoreCase).IsMatch(strItem);
        }

        private void EditedIdentity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Surface" || e.PropertyName == "AnimationOnLoad")
            {
                if (Owner.HasBeenSpawned && Owner.ActiveIdentity == EditedIdentity)
                {
                    Owner.Target(false);
                    Owner.ActiveIdentity.Render(Target: Owner);
                }
            }
            _eventAggregator.GetEvent<SaveCrowdEvent>().Publish(null);
        }
    }

    internal static class IdentityCommands
    {
        public static void TryRename(object state, Identity identity, Character owner,
            string originalName, IMessageBoxService messageBoxService,
            EventAggregator eventAggregator, Action<string> setOriginalName,
            Action<object> onSuccess, Action<object> onCancel)
        {
            if (originalName == null) return;
            string updatedName = Helper.GetTextFromControlObject(state);
            bool duplicateName = updatedName != originalName && owner.AvailableIdentities.ContainsKey(updatedName);
            if (!duplicateName)
            {
                ApplyRename(identity, owner, updatedName, originalName, eventAggregator);
                setOriginalName(null);
                onSuccess(state);
            }
            else
            {
                messageBoxService.ShowDialog(Messages.DUPLICATE_NAME_MESSAGE, "Rename Identity", MessageBoxButton.OK, MessageBoxImage.Error);
                onCancel(state);
            }
        }

        public static void ApplyRename(Identity identity, Character owner, string updatedName,
            string originalName, EventAggregator eventAggregator)
        {
            if (originalName == updatedName) return;
            identity.Name = updatedName;
            owner.AvailableIdentities.UpdateKey(originalName, updatedName);
            eventAggregator.GetEvent<NeedIdentityCollectionRetrievalEvent>().Publish(null);
        }
    }
}
