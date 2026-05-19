using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Crowds;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Module.HeroVirtualTabletop.Library
{
    /// <summary>
    /// Interaction logic for HeroVirtualTabletopMainView.xaml
    /// </summary>
    public partial class HeroVirtualTabletopMainView : UserControl
    {
        private HeroVirtualTabletopMainViewModel viewModel;
        private IUnityContainer container;
        public HeroVirtualTabletopMainView(HeroVirtualTabletopMainViewModel viewModel, IUnityContainer container)
        {
            InitializeComponent();

            this.container = container;
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
        }

        private void HeroVirtualTabletopMainView_Loaded(object sender, RoutedEventArgs e)
        {
            var characterCrowdMainView = this.container.Resolve<CharacterCrowdMainView>();
            this.containerGrid.Children.Add(characterCrowdMainView);
        }
    }
}
