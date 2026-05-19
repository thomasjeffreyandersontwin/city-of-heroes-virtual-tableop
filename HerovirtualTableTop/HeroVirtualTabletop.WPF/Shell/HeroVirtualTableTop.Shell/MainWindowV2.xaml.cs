using Microsoft.Practices.Unity;
using Module.Shared.Enumerations;
using Module.Shared.Error;
using Prism.Events;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ApplicationShell.Views
{
    /// <summary>
    /// Interaction logic for MainWindowV2.xaml
    /// </summary>
    public partial class MainWindowV2 : Window
    {
        private IUnityContainer container;
        private IRegionManager regionManager;
        private IEventAggregator eventAggregator;

        public MainWindowV2(IUnityContainer container, IRegionManager regionManager, IEventAggregator eventAggregator)
        {
            InitializeComponent();

            this.container = container;
            this.regionManager = regionManager;
            this.eventAggregator = eventAggregator;

            this.eventAggregator.GetEvent<Module.Shared.Events.CommonEvents.ErrorOccuredEvent>().Subscribe(ShowErrors, ThreadOption.UIThread); ;
            this.eventAggregator.GetEvent<Module.Shared.Events.CommonEvents.HideErrorEvent>().Subscribe(HideErrors, ThreadOption.UIThread);
        }

        /// <summary>   Listen for any errors and show the standard feedback screen to the user. </summary>
        /// <param name="value">    The value. </param>
        public void ShowErrors(object value)
        {
            ErrorItems errorItems = (ErrorItems)value;
            string message = string.Join("\n", errorItems.Errors.Select(e => e.FriendlyMessage));

            // If error is thrown but message string is empty then do not display any messagebox
            if (string.IsNullOrEmpty(message)) return;

            MessageBoxImage icon = MessageBoxImage.Information;
            if (errorItems.Errors.Any(ei => ei.ErrorDisplayType == ErrorDisplayType.Error))
                icon = MessageBoxImage.Error;
            else if (errorItems.Errors.Any(ei => ei.ErrorDisplayType == ErrorDisplayType.Warning))
                icon = MessageBoxImage.Warning;

            //IMessageBoxService messageBoxService = container.Resolve<IMessageBoxService>();
            //messageBoxService.ShowDialog(message, errorItems.Caption, MessageBoxButton.OK, icon);
        }

        private void HideErrors(ErrorItems errorItems)
        {

        }
    }
}
