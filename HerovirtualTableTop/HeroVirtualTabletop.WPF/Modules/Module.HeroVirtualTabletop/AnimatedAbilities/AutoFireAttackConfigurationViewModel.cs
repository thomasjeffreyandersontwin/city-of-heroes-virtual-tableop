using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    public class AutoFireAttackConfigurationViewModel : BaseViewModel
    {
        private EventAggregator eventAggregator;

        private Attack currentAttack;
        public Attack CurrentAttack
        {
            get
            {
                return currentAttack;
            }
            set
            {
                currentAttack = value;
                OnPropertyChanged("CurrentAttack");
            }
        }

        private List<Character> defendingCharacters;
        public List<Character> DefendingCharacters
        {
            get
            {
                return defendingCharacters;
            }
            set
            {
                defendingCharacters = value;
                OnPropertyChanged("DefendingCharacters");
            }
        }

        private ObservableCollection<DefenderActiveAttackConfiguration> defenderActiveAttackConfigurations;
        public ObservableCollection<DefenderActiveAttackConfiguration> DefenderActiveAttackConfigurations
        {
            get
            {
                return defenderActiveAttackConfigurations;
            }
            set
            {
                defenderActiveAttackConfigurations = value;
                OnPropertyChanged("DefenderActiveAttackConfigurations");
                this.DistributeNumberOfShotsCommand.RaiseCanExecuteChanged();
            }
        }

        private Guid attackConfigKey = Guid.Empty;

        public DelegateCommand ConfirmAutoFireAttackCommand { get; private set; }
        public DelegateCommand<object> DistributeNumberOfShotsCommand { get; private set; }
        public AutoFireAttackConfigurationViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<AssignAutoFireAttackShotsEvent>().Subscribe(this.LoadAttackTargets);
            this.ConfirmAutoFireAttackCommand = new DelegateCommand(ConfirmAutoFireAttack);
            this.DistributeNumberOfShotsCommand = new DelegateCommand<object>(this.DistributeNumberOfShots, CanDistributeNumberOfShots);
        }

        private void LoadAttackTargets(Tuple<Attack, List<Character>, Guid> tuple)
        {
            this.CurrentAttack = tuple.Item1;
            this.DefendingCharacters = new List<Character>(tuple.Item2);
            this.attackConfigKey = tuple.Item3;
            this.DefenderActiveAttackConfigurations = new ObservableCollection<DefenderActiveAttackConfiguration>();
            foreach (var defender in this.DefendingCharacters)
            {
                this.DefenderActiveAttackConfigurations.Add(new DefenderActiveAttackConfiguration { Defender = defender, ActiveAttackConfiguration = defender.AttackConfigurationMap[attackConfigKey].Item2 });
            }
            DistributeNumberOfShots((Character)null);
            Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Arrow;
            });
        }

        private void ConfirmAutoFireAttack()
        {
            this.eventAggregator.GetEvent<AutoFireAttackShotsAssignedEvent>().Publish(this.DefendingCharacters.ToList());
        }

        private bool CanDistributeNumberOfShots(object state)
        {
            return this.DefendingCharacters.Count > 1;
        }
        private bool isUpdating = false;
        private void DistributeNumberOfShots(object state)
        {
            if (!isUpdating)
                DistributeNumberOfShots((Character)state);
        }
        private void DistributeNumberOfShots(Character lastUpdatedCharacter)
        {
            isUpdating = true;
            int maxNumberOfShots = this.CurrentAttack.AttackInfo.AutoFireMaxShots;
            int lastAssignment = 0;
            if (lastUpdatedCharacter != null)
                lastAssignment = lastUpdatedCharacter.AttackConfigurationMap[attackConfigKey].Item2.NumberOfShotsAssigned;
            int remainingNumberOfShots = maxNumberOfShots - lastAssignment;
            foreach (var dc in this.DefenderActiveAttackConfigurations.Where(dc => dc.Defender != lastUpdatedCharacter))
                dc.ActiveAttackConfiguration.NumberOfShotsAssigned = 0;
            for(int i = 0; i < this.DefenderActiveAttackConfigurations.Count; i++)
            {
                if (this.DefenderActiveAttackConfigurations[i].Defender != lastUpdatedCharacter)
                {
                    if (remainingNumberOfShots == 0)
                        break;
                    this.DefenderActiveAttackConfigurations[i].ActiveAttackConfiguration.NumberOfShotsAssigned += 1;
                    remainingNumberOfShots--;
                }
                if (i == DefenderActiveAttackConfigurations.Count - 1 && remainingNumberOfShots > 0)
                    i = -1;
            }
            isUpdating = false;
        }
    }
}
