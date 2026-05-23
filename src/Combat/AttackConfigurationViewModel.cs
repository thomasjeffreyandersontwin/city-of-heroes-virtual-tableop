using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Prism.Events;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Microsoft.Practices.Prism.Commands;
using System.Windows.Input;
using System.Reflection;
using Module.HeroVirtualTabletop.Desktop;
using Module.Shared;

namespace HeroVTT.Combat
{
    public class AttackConfigurationViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;
        private readonly IDesktopKeyEventHandler _desktopKeyEventHandler;
        private readonly ICombatGlobals _combatGlobals;
        private readonly AttackParameterApplier _parameterApplier;

        private static readonly List<Tuple<Module.HeroVirtualTabletop.AnimatedAbilities.Attack, List<Character>, Guid>> _confirmedAttacks =
            new List<Tuple<Module.HeroVirtualTabletop.AnimatedAbilities.Attack, List<Character>, Guid>>();

        private ObservableCollection<ActiveAttackViewModel> _attackConfigurations;
        public ObservableCollection<ActiveAttackViewModel> AttackConfigurations
        {
            get { return _attackConfigurations; }
            private set { _attackConfigurations = value; OnPropertyChanged("AttackConfigurations"); }
        }

        public DelegateCommand<object> ConfirmAttacksCommand { get; private set; }
        public DelegateCommand CancelAttacksCommand { get; private set; }

        public AttackConfigurationViewModel(
            IBusyService busyService,
            IUnityContainer container,
            IDesktopKeyEventHandler keyEventHandler,
            EventAggregator eventAggregator,
            ICombatGlobals combatGlobals)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _desktopKeyEventHandler = keyEventHandler;
            _combatGlobals = combatGlobals;
            _parameterApplier = new AttackParameterApplier();

            _eventAggregator.GetEvent<ConfigureAttacksEvent>().Subscribe(ConfigureAttacks);
            _eventAggregator.GetEvent<ConfirmAttacksEvent>().Subscribe(ConfirmAttacks);

            ConfirmAttacksCommand = new DelegateCommand<object>(ConfirmAttacks);
            CancelAttacksCommand = new DelegateCommand(CancelAttacks);
            _desktopKeyEventHandler.AddKeyEventHandler(RetrieveEventFromKeyInput);
        }

        public void ConfigureAttacks(List<Tuple<Module.HeroVirtualTabletop.AnimatedAbilities.Attack, List<Character>, Guid>> attacksWithDefenders)
        {
            if (attacksWithDefenders.Any(t => _confirmedAttacks.Any(c => c.Item3 == t.Item3) || t.Item3 == Guid.Empty))
            {
                _eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
                return;
            }

            if (AttackConfigurations != null)
                foreach (var cfg in AttackConfigurations)
                    cfg.RemoveDesktopKeyEventHandlers();

            AttackConfigurations = new ObservableCollection<ActiveAttackViewModel>();
            foreach (var tuple in attacksWithDefenders)
            {
                var vm = Container.Resolve<ActiveAttackViewModel>();
                vm.ConfigureActiveAttack(new Tuple<List<Character>, Module.HeroVirtualTabletop.AnimatedAbilities.Attack, Guid>(tuple.Item2, tuple.Item1, tuple.Item3));
                AttackConfigurations.Add(vm);
            }
        }

        private void ConfirmAttacks(object state)
        {
            var attacks = BuildAttackList();
            _confirmedAttacks.AddRange(attacks);

            Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
            Mouse.OverrideCursor = cursor;

            _eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
            _eventAggregator.GetEvent<LaunchAttacksEvent>().Publish(attacks);
        }

        private void CancelAttacks()
        {
            var attacks = BuildAttackList();
            _eventAggregator.GetEvent<CloseAttackConfigurationWidgetEvent>().Publish(null);
            _eventAggregator.GetEvent<CancelAttacksEvent>().Publish(attacks);
        }

        private List<Tuple<Module.HeroVirtualTabletop.AnimatedAbilities.Attack, List<Character>, Guid>> BuildAttackList()
        {
            var result = new List<Tuple<Module.HeroVirtualTabletop.AnimatedAbilities.Attack, List<Character>, Guid>>();
            foreach (var vm in AttackConfigurations)
            {
                vm.SetActiveAttack();
                result.Add(new Tuple<Module.HeroVirtualTabletop.AnimatedAbilities.Attack, List<Character>, Guid>(
                    vm.ActiveAttack, vm.DefendingCharacters.ToList(), vm.AttackConfigKey));
            }
            return result;
        }

        public EventMethod RetrieveEventFromKeyInput(System.Windows.Forms.Keys vkCode, System.Windows.Input.Key inputKey)
        {
            if (_combatGlobals.CurrentActiveWindowName != Constants.ACTIVE_ATTACK_WIDGET)
                return null;

            if (inputKey == Key.Enter && ConfirmAttacksCommand.CanExecute(null))
                ConfirmAttacksCommand.Execute(null);
            else if (inputKey == Key.Escape && CancelAttacksCommand.CanExecute())
                CancelAttacksCommand.Execute();

            return null;
        }
    }
}
