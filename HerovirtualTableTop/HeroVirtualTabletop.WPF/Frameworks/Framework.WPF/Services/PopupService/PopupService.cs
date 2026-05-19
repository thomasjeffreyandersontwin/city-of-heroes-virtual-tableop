using Framework.WPF.Services.BusyService;
using Framework.WPF.Events;
using Framework.WPF.Library;
using Microsoft.Practices.Unity;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Framework.WPF.Library.Enumerations;

namespace Framework.WPF.Services.PopupService
{
    public class PopupService : IPopupService
    {
        private IUnityContainer container;
        private ImageSource icon;
        private readonly Dictionary<string, Type> registeredPopups;
        private readonly Dictionary<string, Window> openedPopups;
        private readonly Dictionary<string, Object> savedPositions;

        public PopupService(IUnityContainer container)
        {
            this.container = container;
            //this.icon = icon;
            registeredPopups = new Dictionary<string, Type>();
            openedPopups = new Dictionary<string, Window>();
            savedPositions = new Dictionary<string, object>();
        }

        public void Register(string key, Type controlType)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");
            if (controlType == null)
                throw new ArgumentNullException("controlType");
            if (!typeof(Control).IsAssignableFrom(controlType))
                throw new ArgumentException("controlType must be of type Control");

            registeredPopups.Add(key, controlType);
        }

        public bool Unregister(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            return registeredPopups.Remove(key);
        }

        public void ShowDialog(string key, BaseViewModel viewModel, bool isModal = true, Action<CancelEventArgs> winClosing = null, SolidColorBrush background = null, System.Windows.Style customStyle = null, WindowStartupLocation location = WindowStartupLocation.CenterOwner, WindowLocation customLocation = WindowLocation.Ignore)
        {
            this.ShowDialog(key, viewModel, null, isModal, winClosing, background, customStyle, location, customLocation);
        }

        public void ShowDialog(string key, BaseViewModel viewModel, string title, bool isModal = true, Action<CancelEventArgs> winClosing = null, SolidColorBrush background = null, System.Windows.Style customStyle = null, WindowStartupLocation location = WindowStartupLocation.CenterOwner, WindowLocation customLocation = WindowLocation.Ignore)
        {
            this.ShowDialog(key, viewModel, title, null, null, isModal, winClosing, background, customStyle, location, customLocation);
        }

        public void ShowDialog(string key, BaseViewModel viewModel, string title,
            Dictionary<string, object> ctrlPropertiesToSet,
            Dictionary<string, object> windowPropertiesToSet, bool isModal = true,
            Action<CancelEventArgs> winClosing = null, SolidColorBrush background = null, System.Windows.Style customStyle = null, WindowStartupLocation location = WindowStartupLocation.CenterOwner, WindowLocation customLocation = WindowLocation.Ignore)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            Type controlType;
            if (!registeredPopups.TryGetValue(key, out controlType))
                return;

            Window win = null;
            if (typeof(Window).IsAssignableFrom(controlType))
            {
                // Create instance of the window to be poppped up
                foreach (ConstructorInfo cnstrInfo in controlType.GetConstructors())
                {
                    ParameterInfo[] paramInfos = cnstrInfo.GetParameters();

                    if (paramInfos.Length == 1 && paramInfos[0].ParameterType.IsAssignableFrom(viewModel.GetType()))
                    {
                        win = (Window)cnstrInfo.Invoke(new object[] { viewModel });
                        break;
                    }
                    else if (paramInfos.Length == 0)
                    {
                        win = (Window)cnstrInfo.Invoke(null);
                        break;
                    }
                }

                if (win == null)
                    throw new Exception("Unable to find proper constructor of the popup.");
                //win = (Window)Activator.CreateInstance(controlType);
            }
            else
            {
                // Create instance of the user control to be poppped up
                Control ctrl = null;
                foreach (ConstructorInfo cnstrInfo in controlType.GetConstructors())
                {
                    ParameterInfo[] paramInfos = cnstrInfo.GetParameters();

                    if (paramInfos.Length == 1 && paramInfos[0].ParameterType.IsAssignableFrom(viewModel.GetType()))
                    {
                        ctrl = (Control)cnstrInfo.Invoke(new object[] { viewModel });
                        break;
                    }
                    else if (paramInfos.Length == 0)
                    {
                        ctrl = (Control)cnstrInfo.Invoke(null);
                        break;
                    }
                }

                if (ctrl == null)
                    throw new Exception("Unable to find proper constructor of the popup user control.");
                //Control ctrl = //(Control)this.container.Resolve(controlType); // (Control)Activator.CreateInstance(controlType);
                //    (Control)Activator.CreateInstance(controlType, viewModel);

                // Create window and add the user control to it
                win = new Window();
                win.Name = key;
                //if(this.icon != null)
                //    win.Icon = this.icon;

                System.Windows.Media.SolidColorBrush backgroundBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
                if (background != null)
                    backgroundBrush = background;
                var adornerDecorator = new System.Windows.Documents.AdornerDecorator();
                Grid layoutGrid = new Grid();
                layoutGrid.Name = "popupGrid";
                layoutGrid.Background = backgroundBrush;
                ctrl.Margin = new Thickness(5);
                layoutGrid.Children.Add(ctrl);
                adornerDecorator.Child = layoutGrid;
                win.Content = adornerDecorator;

                win.WindowStartupLocation = location;
                if (location == WindowStartupLocation.Manual)
                {
                    win.Loaded += (sender, e) =>
                    {
                        var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
                        switch (customLocation)
                        {
                            case WindowLocation.CenterLeft:
                                win.Left = 0;
                                win.Top = (desktopWorkingArea.Height - win.Height) / 2;
                                break;
                            case WindowLocation.BottomRight:
                                win.Left = desktopWorkingArea.Right - win.Width;
                                win.Top = desktopWorkingArea.Bottom - win.Height;
                                break;
                            case WindowLocation.Ignore:
                                break;
                            case WindowLocation.MouseCursor:
                                win.Left = System.Windows.Forms.Control.MousePosition.X;
                                win.Top = System.Windows.Forms.Control.MousePosition.Y;
                                break;
                        }
                    };
                }

                // Adjust popup window's sizing properties with the user control
                //if (double.IsNaN(ctrl.Width) || double.IsNaN(ctrl.Height))
                //{
                //    win.Width = 800; win.Height = 500; // use some predefined Constant relative to main window size
                //}
                //else
                {
                    win.SizeToContent = SizeToContent.WidthAndHeight;
                    win.MaxHeight = 700;
                    //win.ResizeMode = ResizeMode.CanResizeWithGrip; // To be able to resize even with windowstyle none and allowtransparency true
                }

                // Set any property of the control user wants to set by force
                if (ctrlPropertiesToSet != null)
                {
                    foreach (PropertyInfo pInfo in controlType.GetProperties())
                    {
                        if (ctrlPropertiesToSet.ContainsKey(pInfo.Name))
                        {
                            object objPropertyValue = ctrlPropertiesToSet[pInfo.Name];
                            if (objPropertyValue != null)
                                pInfo.SetValue(ctrl, objPropertyValue, null);
                        }
                    }
                }
            }

            win.DataContext = viewModel;

            // set popup window title
            if (!string.IsNullOrEmpty(title))
                win.Title = title;

            
            win.ShowInTaskbar = false;

            if (!openedPopups.ContainsKey(key))
                openedPopups.Add(key, win);
            win.Tag = key;

            if (winClosing != null)
            {
                win.Closing += new CancelEventHandler
                (
                  (sender, e) =>
                  {
                      winClosing(e);
                  }
                );
            }

            win.Closed += new EventHandler(win_Closed);



            // Set any property of the window user wants to set by force
            if (windowPropertiesToSet != null)
            {
                foreach (PropertyInfo pInfo in win.GetType().GetProperties())
                {
                    if (windowPropertiesToSet.ContainsKey(pInfo.Name))
                    {
                        object objPropertyValue = windowPropertiesToSet[pInfo.Name];
                        if (objPropertyValue != null)
                            pInfo.SetValue(win, objPropertyValue, null);
                    }
                }
            }

            win.PreviewKeyDown += new System.Windows.Input.KeyEventHandler(win_PreviewKeyDown); // prevent "KeyDown" event from being fired when "busy" is being shown
            container.Resolve<IEventAggregator>().GetEvent<PopupOpened>().Publish(win);


            if (customStyle != null)
                win.Style = customStyle;

            if (isModal)
            {
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();
            }
            else
            {
                win.WindowStyle = WindowStyle.None; // Modeless window, assuming user will implement necessary window actions e.g. close, minimize, maximize etc. Need to fix this in future.
                win.Show();
            }
        }
        
        void win_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            IBusyService busyService = container.Resolve<IBusyService>();
            if (busyService.IsShowingBusy)
                e.Handled = true;
        }

        private void win_Closed(object sender, EventArgs e)
        {
            Window win = sender as Window;
            string key = (string)win.Tag;

            if (openedPopups.ContainsKey(key))
                openedPopups.Remove(key);
        }

        public void CloseDialog(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");

            Window win;
            if (!openedPopups.TryGetValue(key, out win))
                return;

            win.Close();
        }

        public bool IsOpen(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException("key");
            return openedPopups.ContainsKey(key);
        }

        public void SavePosition(string primaryKey, params string[] secondaryKeys)
        {
            if (string.IsNullOrEmpty(primaryKey))
                throw new ArgumentNullException("primaryKey");
            if (openedPopups.ContainsKey(primaryKey))
            {
                Window window = openedPopups[primaryKey];
                double left = window.Left;
                double top = window.Top;
                double[] positionArray = new double[] { left, top };
                string key = ConstructPopupKey(primaryKey, secondaryKeys);
                if (savedPositions.ContainsKey(key))
                    savedPositions[key] = positionArray;
                else
                    savedPositions.Add(key, positionArray);
            }
        }

        public object GetPosition(string primaryKey, params string[] secondaryKeys)
        {
            if (string.IsNullOrEmpty(primaryKey))
                throw new ArgumentNullException("primaryKey");
            string key = ConstructPopupKey(primaryKey, secondaryKeys);
            if (savedPositions.ContainsKey(key))
                return savedPositions[key];
            return null;
        }

        private string ConstructPopupKey(string primaryKey, params string[] secondaryKeys)
        {
            string key = primaryKey;
            if(secondaryKeys != null && secondaryKeys.Length > 0)
            {
                foreach (var secKey in secondaryKeys)
                    key += "|" + secKey;
            }

            return key;
        }
    }
}
