using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Roster;
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
using Module.HeroVirtualTabletop.Identities;
using Module.HeroVirtualTabletop.AnimatedAbilities;
using Module.HeroVirtualTabletop.Library.Converters;
using Module.HeroVirtualTabletop.Movements;

namespace Module.HeroVirtualTabletop.Crowds
{
    /// <summary>
    /// Interaction logic for CharacterCrowdMainView.xaml
    /// </summary>
    public partial class CharacterCrowdMainView : UserControl
    {
        private CharacterCrowdMainViewModel viewModel;
        public CharacterCrowdMainView(CharacterCrowdMainViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.ViewLoaded += viewModel_ViewLoaded;

            this.viewModel.LoadCharacterExplorer();
            this.viewModel.LoadRosterExplorer();
            this.viewModel.LoadCharacterEditor();
            this.viewModel.LoadIdentityEditor();
            this.viewModel.LoadAbilityEditor();
            this.viewModel.LoadMovementEditor();
            this.viewModel.LoadCrowdFromModelsView();
        }

        void viewModel_ViewLoaded(object sender, EventArgs e)
        {
            if (sender != null && sender is CharacterExplorerView)
                RenderCharacterExplorer(sender as CharacterExplorerView);
            else if (sender != null && sender is RosterExplorerView)
                RenderRosterExplorer(sender as RosterExplorerView);
            else if (sender != null && sender is CharacterEditorView)
                RenderCharacterEditor(sender as CharacterEditorView);
            else if (sender != null && sender is IdentityEditorView)
                RenderIdentityEditor(sender as IdentityEditorView);
            else if (sender != null && sender is AbilityEditorView)
                RenderAbilityEditor(sender as AbilityEditorView);
            else if (sender != null && sender is MovementEditorView)
                RenderMovementEditor(sender as MovementEditorView);
            else if (sender != null && sender is CrowdFromModelsView)
                RenderCrowdFromModelsView(sender as CrowdFromModelsView);
        }

        private void RenderCharacterExplorer(CharacterExplorerView charExplorerView)
        {
            this.charExplorerPanel.Children.Add(charExplorerView);
        }
        private void RenderRosterExplorer(RosterExplorerView rosterExplorerView)
        {
            this.rosterExplorerPanel.Children.Add(rosterExplorerView);
        }
        private void RenderCharacterEditor(CharacterEditorView charEditorView)
        {
            this.charEditorPanel.Children.Add(charEditorView);
        }
        private void RenderIdentityEditor(IdentityEditorView identityEditorView)
        {
            this.identityEditorPanel.Children.Add(identityEditorView);
        }
        private void RenderAbilityEditor(AbilityEditorView abilityEditorView)
        {
            this.abilityEditorPanel.Children.Add(abilityEditorView);
        }
        private void RenderMovementEditor(MovementEditorView movementEditorView)
        {
            this.movementEditorPanel.Children.Add(movementEditorView);
        }
        private void RenderCrowdFromModelsView(CrowdFromModelsView crowdFromModelsView)
        {
            this.crowdFromModelsPanel.Children.Add(crowdFromModelsView);
        }

        private void Expander_ExpansionChanged(object sender, RoutedEventArgs e)
        {
            Window window = null;
            DependencyObject dObj = sender as DependencyObject;
            while (true)
            {
                dObj = VisualTreeHelper.GetParent(dObj);
                if (dObj is Window)
                {
                    window = dObj as Window; break;
                }
            }
            if (window != null)
            {              
                window.SizeToContent = SizeToContent.Width;
            }  
            
        }
    }
}
