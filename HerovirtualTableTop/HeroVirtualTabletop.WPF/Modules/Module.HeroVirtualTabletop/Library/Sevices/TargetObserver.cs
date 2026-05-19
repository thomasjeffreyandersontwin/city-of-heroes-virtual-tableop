using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Library.Events;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Prism.Events;
using System;
using System.ComponentModel;
using System.Threading;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Characters;

namespace Module.HeroVirtualTabletop.Library.Sevices
{
    public class TargetObserver : ITargetObserver
    {
        private IUnityContainer container;
        private EventAggregator eventAggregator;
        private BackgroundWorker bgWorker;
        private Character currentAttacker;
        private Attack currentAttack;

        public TargetObserver(IUnityContainer container, EventAggregator eventAggregator)
        {
            this.container = container;
            this.eventAggregator = eventAggregator;
            currentTargetPointer = new MemoryInstance().TargetPointer;
            bgWorker = new BackgroundWorker();
            bgWorker.WorkerReportsProgress = false;
            bgWorker.DoWork += listenForTargetChanged;
            bgWorker.RunWorkerCompleted += restart;
            bgWorker.RunWorkerAsync();
        }

        private void restart(object sender, RunWorkerCompletedEventArgs e)
        {
            bgWorker.RunWorkerAsync();
        }

        private void listenForTargetChanged(object sender, DoWorkEventArgs e)
        {
            uint actualTarget = new MemoryInstance().TargetPointer;
            while (actualTarget == currentTargetPointer)
            {
                Thread.Sleep(500);
                actualTarget = new MemoryInstance().TargetPointer;
            }
            currentTargetPointer = actualTarget;
            OnTargetChanged(currentTargetPointer, new EventArgs());
        }

        public event EventHandler TargetChanged;

        private void OnTargetChanged(object sender, EventArgs e)
        {
            if (TargetChanged != null)
                TargetChanged(sender, e);
        }

        private uint currentTargetPointer;
        public uint CurrentTargetPointer
        {
            get
            {
                return currentTargetPointer;
            }
        }
    }
}
