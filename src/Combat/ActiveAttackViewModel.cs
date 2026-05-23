using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Desktop;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Events;
using Module.Shared;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace HeroVTT.Combat
{
    public class ActiveAttackViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;
        private readonly IMessageBoxService _messageBoxService;
        private readonly IDesktopKeyEventHandler _desktopKeyEventHandler;
        private readonly ICombatGlobals _combatGlobals;
        private readonly AttackParameterApplier _parameterApplier;
        private readonly AttackSummaryBuilder _summaryBuilder;
        private readonly CombatKeyHandler _keyHandler;

        private Attack _activeAttack;
        public Attack ActiveAttack
        {
            get { return _activeAttack; }
            set { _activeAttack = value; OnPropertyChanged("ActiveAttack"); }
        }

        private List<Character> _defendingCharacters;
        public List<Character> DefendingCharacters
        {
            get { return _defendingCharacters; }
            set { _defendingCharacters = value; OnPropertyChanged("DefendingCharacters"); }
        }

        private ObservableCollection<DefenderActiveAttackConfiguration> _defenderConfigurations;
        public ObservableCollection<DefenderActiveAttackConfiguration> DefenderActiveAttackConfigurations
        {
            get { return _defenderConfigurations; }
            set { _defenderConfigurations = value; OnPropertyChanged("DefenderActiveAttackConfigurations"); }
        }

        private bool _moveAttackerToTarget;
        public bool MoveAttackerToTarget
        {
            get { return _moveAttackerToTarget; }
            set { _moveAttackerToTarget = value; OnPropertyChanged("MoveAttackerToTarget"); }
        }

        private string _attackSummaryText;
        public string AttackSummaryText
        {
            get { return _attackSummaryText; }
            set { _attackSummaryText = value; OnPropertyChanged("AttackSummaryText"); }
        }

        private bool _showAttackSummaryText;
        public bool ShowAttackSummaryText
        {
            get { return _showAttackSummaryText; }
            set { _showAttackSummaryText = value; OnPropertyChanged("ShowAttackSummaryText"); }
        }

        public Guid AttackConfigKey { get; set; }

        public DelegateCommand<object> CenterTargetChangedCommand { get; private set; }
        public DelegateCommand<object> SetActiveAttackCommand { get; private set; }
        public DelegateCommand<object> CancelActiveAttackCommand { get; private set; }
        public DelegateCommand<string> ActivatePanelCommand { get; private set; }
        public DelegateCommand<string> DeactivatePanelCommand { get; private set; }
        public DelegateCommand<object> AttackHitChangedCommand { get; private set; }

        public ActiveAttackViewModel(
            IBusyService busyService,
            IUnityContainer container,
            IMessageBoxService messageBoxService,
            IDesktopKeyEventHandler keyEventHandler,
            EventAggregator eventAggregator,
            ICombatGlobals combatGlobals)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _messageBoxService = messageBoxService;
            _desktopKeyEventHandler = keyEventHandler;
            _combatGlobals = combatGlobals;
            _parameterApplier = new AttackParameterApplier();
            _summaryBuilder = new AttackSummaryBuilder();
            _keyHandler = new CombatKeyHandler();

            SetActiveAttackCommand = new DelegateCommand<object>(o => SetActiveAttack());
            CancelActiveAttackCommand = new DelegateCommand<object>(CancelActiveAttack);
            CenterTargetChangedCommand = new DelegateCommand<object>(ChangeCenterTarget);
            ActivatePanelCommand = new DelegateCommand<string>(name => _combatGlobals.CurrentActiveWindowName = name);
            DeactivatePanelCommand = new DelegateCommand<string>(DeactivatePanel);
            AttackHitChangedCommand = new DelegateCommand<object>(ChangeAttackHit);

            _desktopKeyEventHandler.AddKeyEventHandler(RetrieveEventFromKeyInput);
        }

        public void RemoveDesktopKeyEventHandlers()
        {
            _desktopKeyEventHandler.RemoveKeyEventHandler(RetrieveEventFromKeyInput);
        }

        public void ConfigureActiveAttack(Tuple<List<Character>, Attack, Guid> tuple)
        {
            DefendingCharacters = tuple.Item1;
            ActiveAttack = tuple.Item2;
            AttackConfigKey = tuple.Item3;

            DefenderActiveAttackConfigurations = new ObservableCollection<DefenderActiveAttackConfiguration>();
            foreach (var defender in DefendingCharacters)
            {
                DefenderActiveAttackConfigurations.Add(new DefenderActiveAttackConfiguration
                {
                    Defender = defender,
                    ActiveAttackConfiguration = defender.AttackConfigurationMap[AttackConfigKey].Item2
                });
                if (defender.AttackConfigurationMap[AttackConfigKey].Item2.MoveAttackerToTarget)
                    MoveAttackerToTarget = true;
            }

            ShowAttackSummaryText = _combatGlobals.IntegrateWithHcs;
            if (ShowAttackSummaryText)
                AttackSummaryText = _summaryBuilder.Build(DefendingCharacters, AttackConfigKey);
        }

        public void SetActiveAttack()
        {
            foreach (Character ch in DefendingCharacters)
            {
                _parameterApplier.ApplyAttackParameters(ch, AttackConfigKey);
                ch.AttackConfigurationMap[AttackConfigKey].Item2.MoveAttackerToTarget = MoveAttackerToTarget;
            }
        }

        private void CancelActiveAttack(object state)
        {
            foreach (var c in DefendingCharacters)
                c.RemoveAttackConfiguration(AttackConfigKey);
        }

        private void ChangeCenterTarget(object state)
        {
            if (!ActiveAttack.IsAreaEffect)
                return;

            var character = state as Character;
            if (character == null || !character.AttackConfigurationMap[AttackConfigKey].Item2.IsCenterTarget)
                return;

            foreach (Character ch in DefendingCharacters.Where(dc => dc.Name != character.Name))
            {
                ch.AttackConfigurationMap[AttackConfigKey].Item2.IsCenterTarget = false;
                ch.RefreshAttackConfigurationParameters();
            }
            character.RefreshAttackConfigurationParameters();
        }

        private void ChangeAttackHit(object state)
        {
            var target = state as Character;
            if (target == null) return;

            var config = target.AttackConfigurationMap[AttackConfigKey].Item2;
            config.IsHit = config.AttackResults.Any(ar => ar.IsHit);
            target.RefreshAttackConfigurationParameters();
        }

        private void DeactivatePanel(string panelName)
        {
            if (_combatGlobals.CurrentActiveWindowName == panelName)
                _combatGlobals.CurrentActiveWindowName = "";
        }

        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, Key inputKey)
        {
            if (_combatGlobals.CurrentActiveWindowName != Constants.ACTIVE_ATTACK_WIDGET)
                return null;

            if (inputKey == Key.Enter && SetActiveAttackCommand.CanExecute(null))
                SetActiveAttackCommand.Execute(null);
            else if (inputKey == Key.Escape && CancelActiveAttackCommand.CanExecute(null))
                CancelActiveAttackCommand.Execute(null);
            else
                _keyHandler.HandleKeyInput(inputKey, DefendingCharacters, AttackConfigKey);

            return null;
        }
    }
}
