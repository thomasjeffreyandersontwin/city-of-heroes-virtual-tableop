using Framework.WPF.Library;
using Framework.WPF.Library.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Framework.WPF.Services.PopupService
{
    public interface IPopupService
    {
        void Register(string key, Type controlType);
        bool Unregister(string key);
        void ShowDialog(string key, BaseViewModel viewModel, bool isModal, Action<CancelEventArgs> winClosing = null, System.Windows.Media.SolidColorBrush background = null, System.Windows.Style customStyle = null, WindowStartupLocation location = WindowStartupLocation.CenterOwner, WindowLocation customLocation = WindowLocation.Ignore);
        void ShowDialog(string key, BaseViewModel viewModel, string title, bool isModal, Action<CancelEventArgs> winClosing = null, System.Windows.Media.SolidColorBrush background = null, System.Windows.Style customStyle = null, WindowStartupLocation location = WindowStartupLocation.CenterOwner, WindowLocation customLocation = WindowLocation.Ignore);
        void ShowDialog(string key, BaseViewModel viewModel, string title, Dictionary<string, object> ctrlPropertiesToSet,
            Dictionary<string, object> windowPropertiesToSet, bool isModal, Action<CancelEventArgs> winClosing = null, System.Windows.Media.SolidColorBrush background = null, System.Windows.Style customStyle = null, WindowStartupLocation location = WindowStartupLocation.CenterOwner, WindowLocation customLocation = WindowLocation.Ignore);
        void SavePosition(string primaryKey, params string[] secondaryKeys);
        object GetPosition(string primaryKey, params string[] secondaryKeys);
        void CloseDialog(string key);
        bool IsOpen(string key);
    }
}
