using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Prism.Commands;
using Microsoft.Practices.Unity;
using Characters = Module.HeroVirtualTabletop.Characters;
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

using Module.HeroVirtualTabletop.OptionGroups;
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Movements;
using Module.HeroVirtualTabletop.Desktop;
using Crowds = Module.HeroVirtualTabletop.Crowds;
using Library = Module.HeroVirtualTabletop.Library;
using Roster = Module.HeroVirtualTabletop.Roster;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Crowds;
namespace HeroVTT.AnimatedAbilities
{
    public class AttackTargetSelectionViewModel : BaseViewModel
    {
        private EventAggregator eventAggregator;
        private ObservableCollection<AttackTarget> attackTargets;
        public DelegateCommand ConfirmAttackTargetsCommand { get; private set; }
        public ObservableCollection<AttackTarget> AttackTargets
        {
            get
            {
                return attackTargets;
            }
            set
            {
                attackTargets = value;
                OnPropertyChanged("AttackTargets");
            }
        }
        public AttackTargetSelectionViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            this.eventAggregator.GetEvent<AttackTargetsSelectionRequiredEvent>().Subscribe(this.LoadAttackTargets);
            this.ConfirmAttackTargetsCommand = new DelegateCommand(ConfirmAttackTargets);
        }

        private void LoadAttackTargets(List<Character> defendingCharacters)
        {
            this.AttackTargets = new ObservableCollection<AttackTarget>();
            foreach(var defender in defendingCharacters)
            {
                this.AttackTargets.Add(new AttackTarget { Defender = defender, Targeted = true});
            }
            Dispatcher.Invoke(() => {
                Mouse.OverrideCursor = Cursors.Arrow;
            });
        }

        private void ConfirmAttackTargets()
        {
            List<Character> attackTargets = new List<Character>();
            foreach (var attackTarget in this.AttackTargets.Where(at => at.Targeted))
                attackTargets.Add(attackTarget.Defender);
            this.eventAggregator.GetEvent<AttackTargetsConfirmedEvent>().Publish(attackTargets);
            Dispatcher.Invoke(() => {
                Cursor cursor = new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("Module.HeroVirtualTabletop.Resources.Bullseye.cur"));
                Mouse.OverrideCursor = cursor;
            });
        }
    }
}
