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

namespace Module.HeroVirtualTabletop.AnimatedAbilities
{
    /// <summary>
    /// Interaction logic for AttackTargetSelectionView.xaml
    /// </summary>
    public partial class AttackTargetSelectionView : UserControl
    {
        private AttackTargetSelectionViewModel viewModel;
        public AttackTargetSelectionView(AttackTargetSelectionViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
        }
    }
}
