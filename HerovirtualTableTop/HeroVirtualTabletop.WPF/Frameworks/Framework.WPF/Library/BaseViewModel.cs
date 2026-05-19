using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Framework.WPF.Library
{
    public abstract class BaseViewModel : DependencyObject, INotifyPropertyChanged
    {
        #region Private Fields

        private PropertyChangedEventHandler propertyChanged;

        #endregion Private Fields

        #region Constructor

        public BaseViewModel(IBusyService busyService, IUnityContainer container)
        {
            this.BusyService = busyService;
            this.Container = container;

            // save the dispatcher so that UI can be updated from another thread
            this.Dispatcher = Dispatcher.CurrentDispatcher;

            this.UIAction =
                ((uiAction) =>
                    this.Dispatcher.BeginInvoke(uiAction)
                 );
        }

        #endregion Constructor

        #region Protected Members

        protected IBusyService BusyService { get; set; }
        protected Dispatcher Dispatcher { get; set; }
        protected IUnityContainer Container { get; set; }
        protected Action<Action> UIAction { get; set; }     // to be used to execute some "action" on UI thread, rahter than using Dispatcher property directly

        /// <summary>
        /// This method is used to verify that calling thread is UI thread.
        /// May be used while setting or getting properties that are bound to UI.
        /// This makes it easy to catch these violations early.
        /// </summary>
        [Conditional("Debug")]
        protected void VerifyCalledOnUIThread()
        {
            Debug.Assert(Dispatcher.CurrentDispatcher == this.Dispatcher,
                        "Call must be made on UI thread.");
        }

        protected void NotifyPropertiesChanged(params string[] propertyNames)
        {
            foreach (string propertyName in propertyNames)
            {
                OnPropertyChanged(propertyName);
            }
        }

        #endregion Protected Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                VerifyCalledOnUIThread();
                propertyChanged += value;
            }
            remove
            {
                VerifyCalledOnUIThread();
                propertyChanged -= value;
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            VerifyCalledOnUIThread();
            if (propertyChanged != null)
                propertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
