using Module.HeroVirtualTabletop.Library.Utility;
using Module.HeroVirtualTabletop.OptionGroups;
using Module.Shared;
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

namespace Module.HeroVirtualTabletop.Characters
{
    /// <summary>
    /// Interaction logic for CharacterEditorView.xaml
    /// </summary>
    public partial class CharacterEditorView : UserControl
    {
        private CharacterEditorViewModel viewModel;

        public CharacterEditorView(CharacterEditorViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = viewModel;
        }

        #region Drag Drop Option Group

        bool isDragging = false;
        private void StartDrag(string draggingOptionGroupName, MouseEventArgs e)
        {
            isDragging = true;
            try
            {
                if (draggingOptionGroupName != null)
                {
                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject(Constants.OPTION_GROUP_DRAG_KEY, draggingOptionGroupName);
                    DragDrop.DoDragDrop(this.listViewOptionGroup, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                isDragging = false;
                Helper.GlobalVariables_DraggingOptionGroupName = null;
            }
        }

        private void ListView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Action action = delegate()
            {
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging && Helper.GlobalVariables_DraggingOptionGroupName != null)
                {
                    // Get the current mouse position
                    Point mousePos = e.GetPosition(null);
                    Vector diff = Helper.GlobalVariables_OptionGroupDragStartPoint - mousePos;
                    if (
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        StartDrag(Helper.GlobalVariables_DraggingOptionGroupName, e);
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void ListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(Constants.OPTION_GROUP_DRAG_KEY) && !e.Data.GetDataPresent(Constants.OPTION_DRAG_KEY))
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void FindDropTarget(ListView listView, out ListViewItem listViewItem, DragEventArgs dragEventArgs)
        {
            listViewItem = null;

            DependencyObject k = VisualTreeHelper.HitTest(listView, dragEventArgs.GetPosition(listView)).VisualHit;

            while (k != null)
            {
                if (k is ListViewItem)
                {
                    ListViewItem lvItem = k as ListViewItem;
                    if (lvItem.DataContext is IOptionGroupViewModel)
                    {
                        listViewItem = lvItem;
                        break;
                    }
                }
                else if (k == listView)
                {
                    break;
                }

                k = VisualTreeHelper.GetParent(k);
            }
        }

        private void ListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.OPTION_GROUP_DRAG_KEY))
            {
                string draggingOPtionGroupName = e.Data.GetData(Constants.OPTION_GROUP_DRAG_KEY) as string;
                ListViewItem listViewItem;
                FindDropTarget(sender as ListView, out listViewItem, e);
                if(listViewItem != null)
                {
                    IOptionGroupViewModel targetViewModel = listViewItem.DataContext as IOptionGroupViewModel;
                    if(targetViewModel.OptionGroup.Name != draggingOPtionGroupName)
                    {
                        var sourceViewModel = this.viewModel.OptionGroups.FirstOrDefault(vm => vm.OptionGroup.Name == draggingOPtionGroupName);
                        int sourceIndex = this.viewModel.OptionGroups.IndexOf(sourceViewModel);
                        int targetIndex = this.viewModel.OptionGroups.IndexOf(targetViewModel);
                        this.viewModel.ReOrderOptionGroups(sourceIndex, targetIndex);
                    }
                }
            }
        }

        #endregion

        private void ListView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Helper.GlobalVariables_DraggingOptionGroupName = null;
        }

        private void SaveCharacter(object sender, RoutedEventArgs e)
        {
            this.viewModel.SaveCharacter(null);
        }
    }
}
