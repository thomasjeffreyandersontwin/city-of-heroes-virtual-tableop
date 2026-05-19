using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Library.Utility
{
    public class AsyncDelegateExecuter
    {
        private Timer timer;
        private int dueTime;
        private int period = Timeout.Infinite;
        private bool isRecurring;
        private Action delegateToExecute;

        public AsyncDelegateExecuter(Action delegateToExecute, int dueTime, bool isRecurring = false, int period = Timeout.Infinite)
        {
            this.delegateToExecute = delegateToExecute;
            this.isRecurring = isRecurring;
            this.dueTime = dueTime;
            if (isRecurring)
                this.period = period;
            timer = new Timer(Timer_Callback);
        }

        public void ExecuteAsyncDelegate()
        {
            timer.Change(dueTime, period);
        }

        private void Timer_Callback(object state)
        {
            var app = System.Windows.Application.Current;
            if (app != null && delegateToExecute != null)
                app.Dispatcher.BeginInvoke(delegateToExecute);
        }
    }
}

