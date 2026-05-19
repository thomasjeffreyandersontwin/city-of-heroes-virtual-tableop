using Module.HeroVirtualTabletop.Library.Utility;
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

namespace Module.HeroVirtualTabletop.Identities
{
    /// <summary>
    /// Interaction logic for IdentityEditor.xaml
    /// </summary>
    public partial class IdentityEditorView : UserControl
    {
        private IdentityEditorViewModel viewModel;
        public IdentityEditorView(IdentityEditorViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter += viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
        }

        private void viewModel_EditModeEnter(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            Grid grid = txtBox.Parent as Grid;
            TextBox textBox = grid.Children[1] as TextBox;
            textBox.Visibility = Visibility.Visible;
            textBox.Focus();
            textBox.SelectAll();
        }

        private void viewModel_EditModeLeave(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            txtBox.Visibility = Visibility.Hidden;
            BindingExpression expression = txtBox.GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource();
        }

        private void listBoxAnimations_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!((bool)e.NewValue))
            {
                if (listBoxAnimations.SelectedIndex >= 0)
                    Helper.SaveUISettings("Identity_AnimationOnLoadSelectedIndex", listBoxAnimations.SelectedIndex);
            }
            else
            {
                object selectedIndex = Helper.GetUISettings("Identity_AnimationOnLoadSelectedIndex");
                if(selectedIndex != null)
                    listBoxAnimations.ScrollIntoView(selectedIndex);
            }
        }
        private void listBoxCostumes_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!((bool)e.NewValue))
            {
                if (listBoxAnimations.SelectedIndex >= 0)
                    Helper.SaveUISettings("Identity_CostumesSelectedIndex", listBoxCostumes.SelectedIndex);
            }
            else
            {
                object selectedIndex = Helper.GetUISettings("Identity_CostumesSelectedIndex");
                if (selectedIndex != null)
                    listBoxCostumes.ScrollIntoView(selectedIndex);
            }
        }

        private void listBoxModels_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!((bool)e.NewValue))
            {
                if (listBoxAnimations.SelectedIndex >= 0)
                    Helper.SaveUISettings("Identity_ModelsSelectedIndex", listBoxModels.SelectedIndex);
            }
            else
            {
                object selectedIndex = Helper.GetUISettings("Identity_ModelsSelectedIndex");
                if (selectedIndex != null)
                    listBoxModels.ScrollIntoView(selectedIndex);
            }
        }
    }
}
