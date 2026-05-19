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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Framework.WPF.Services.BusyService
{
    /// <summary>
    /// Interaction logic for BusyControl.xaml
    /// </summary>
    public partial class BusyControl : UserControl
    {
        #region Private Fields

        private Storyboard sbLoading;

        #endregion Private Fields

        #region Constructors

        public BusyControl()
        {
            InitializeComponent();

            sbLoading = (Storyboard)this.Resources["sbLoadingAnimation"];
            //Loaded += new RoutedEventHandler(BusyControl_Loaded);
        }

        #endregion Constructors

        #region Event Handlers

        //void BusyControl_Loaded(object sender, RoutedEventArgs e)
        //{
        //    sbLoading.Begin();
        //}

        #endregion Event Handlers

        public void BeginAnimation()
        {
            sbLoading.Begin();
        }

        public void StopAnimation()
        {
            sbLoading.Stop();
        }

        public void OverlayWithoutAnimation()
        {
            tbLoadingText.Visibility = Visibility.Collapsed;
            grdLoadingAnimation.Visibility = Visibility.Collapsed;
        }
    }
}
