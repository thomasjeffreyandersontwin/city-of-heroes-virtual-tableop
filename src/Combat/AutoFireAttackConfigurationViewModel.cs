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
    public class AutoFireAttackConfigurationViewModel : BaseViewModel
    {
        private readonly EventAggregator _eventAggregator;

        private Attack _currentAttack;
        public Attack CurrentAttack
        {
            get { return _currentAttack; }
            set { _currentAttack = value; OnPropertyChanged("CurrentAttack"); }
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
            set
            {
                _defenderConfigurations = value;
                OnPropertyChanged("DefenderActiveAttackConfigurations");
                DistributeNumberOfShotsCommand.RaiseCanExecuteChanged();
            }
        }

        private Guid _attackConfigKey = Guid.Empty;

        public DelegateCommand ConfirmAutoFireAttackCommand { get; private set; }
        public DelegateCommand<object> DistributeNumberOfShotsCommand { get; private set; }

        public AutoFireAttackConfigurationViewModel(
            IBusyService busyService,
            IUnityContainer container,
            EventAggregator eventAggregator)
            : base(busyService, container)
        {
            _eventAggregator = eventAggregator;
            _eventAggregator.GetEvent<AssignAutoFireAttackShotsEvent>().Subscribe(LoadAttackTargets);
            ConfirmAutoFireAttackCommand = new DelegateCommand(ConfirmAutoFireAttack);
            DistributeNumberOfShotsCommand = new DelegateCommand<object>(DistributeShots, CanDistributeShots);
        }

        private void LoadAttackTargets(Tuple<Attack, List<Character>, Guid> tuple)
        {
            CurrentAttack = tuple.Item1;
            DefendingCharacters = new List<Character>(tuple.Item2);
            _attackConfigKey = tuple.Item3;

            DefenderActiveAttackConfigurations = new ObservableCollection<DefenderActiveAttackConfiguration>();
            foreach (var defender in DefendingCharacters)
            {
                DefenderActiveAttackConfigurations.Add(new DefenderActiveAttackConfiguration
                {
                    Defender = defender,
                    ActiveAttackConfiguration = defender.AttackConfigurationMap[_attackConfigKey].Item2
                });
            }
            DistributeShots((Character)null);

            Dispatcher.Invoke(() => { Mouse.OverrideCursor = Cursors.Arrow; });
        }

        private void ConfirmAutoFireAttack()
        {
            _eventAggregator.GetEvent<AutoFireAttackShotsAssignedEvent>().Publish(DefendingCharacters.ToList());
        }

        private bool CanDistributeShots(object state)
        {
            return DefendingCharacters.Count > 1;
        }

        private bool _isUpdating;
        private void DistributeShots(object state)
        {
            if (!_isUpdating)
                DistributeShots(state as Character);
        }

        private void DistributeShots(Character lastUpdated)
        {
            _isUpdating = true;
            int maxShots = CurrentAttack.AttackInfo.AutoFireMaxShots;
            int lastAssignment = lastUpdated != null
                ? lastUpdated.AttackConfigurationMap[_attackConfigKey].Item2.NumberOfShotsAssigned
                : 0;
            int remaining = maxShots - lastAssignment;

            foreach (var dc in DefenderActiveAttackConfigurations.Where(dc => dc.Defender != lastUpdated))
                dc.ActiveAttackConfiguration.NumberOfShotsAssigned = 0;

            for (int i = 0; i < DefenderActiveAttackConfigurations.Count; i++)
            {
                if (DefenderActiveAttackConfigurations[i].Defender != lastUpdated)
                {
                    if (remaining == 0) break;
                    DefenderActiveAttackConfigurations[i].ActiveAttackConfiguration.NumberOfShotsAssigned += 1;
                    remaining--;
                }
                if (i == DefenderActiveAttackConfigurations.Count - 1 && remaining > 0)
                    i = -1;
            }
            _isUpdating = false;
        }
    }
}
