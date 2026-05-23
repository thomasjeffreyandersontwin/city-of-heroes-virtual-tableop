using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;

namespace HeroVTT.Combat
{
    public class AttackTargetSelectionViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;

        private ObservableCollection<AttackTarget> _attackTargets;
        public ObservableCollection<AttackTarget> AttackTargets
        {
            get { return _attackTargets; }
            set { _attackTargets = value; OnPropertyChanged("AttackTargets"); }
        }

        public DelegateCommand ConfirmAttackTargetsCommand { get; private set; }

        public AttackTargetSelectionViewModel(
            IBusyService busyService,
            IUnityContainer container,
            EventAggregator eventAggregator)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AttackTargetsSelectionRequiredEvent>().Subscribe(LoadAttackTargets);
            ConfirmAttackTargetsCommand = new DelegateCommand(ConfirmAttackTargets);
        }

        private void LoadAttackTargets(List<Character> defendingCharacters)
        {
            AttackTargets = new ObservableCollection<AttackTarget>();
            foreach (var defender in defendingCharacters)
                AttackTargets.Add(new AttackTarget { Defender = defender, Targeted = true });

            Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Arrow; });
        }

        private void ConfirmAttackTargets()
        {
            var targets = AttackTargets.Where(at => at.Targeted).Select(at => at.Defender).ToList();
            _eventAggregator.GetEvent<AttackTargetsConfirmedEvent>().Publish(targets);

            Dispatcher.Invoke(() =>
            {
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Mouse.OverrideCursor = cursor;
            });
        }
    }
}
