using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Framework.WPF.Extensions;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Module.Shared.Events;
using Module.HeroVirtualTabletop.Library.Enumerations;

namespace Module.HeroVirtualTabletop.Crowds
{
    /// <summary>
    /// Interaction logic for CharacterExplorerView.xaml
    /// </summary>
    public partial class CharacterExplorerView : UserControl
    {
        private CharacterExplorerViewModel viewModel;
        private CrowdModel selectedCrowdRoot;
        public CharacterExplorerView(CharacterExplorerViewModel viewModel)
        {
            InitializeComponent();

            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter += viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            this.viewModel.EditNeeded += viewModel_EditNeeded;
            this.viewModel.ExpansionUpdateNeeded+=viewModel_ExpansionUpdateNeeded;
            this.viewModel.FlattenNumberRequired += viewModel_FlattenNumberRequired;
            this.viewModel.FlattenNumberEntryFinished += viewModel_FlattenNumberEntryFinished;
        }
        private void CharacterExplorerView_Loaded(object sender, RoutedEventArgs e)
        {
            this.viewModel.LoadCrowdCollectionCommand.Execute(this);
        }
        private void viewModel_EditNeeded(object sender, CustomEventArgs<string> e)
        {
            ICrowdMemberModel modelToSelect = sender as ICrowdMemberModel;
            if(sender == null) // need to unselect
            {
                DependencyObject dObject = treeViewCrowd.GetItemFromSelectedObject(treeViewCrowd.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    this.selectedCrowdRoot = null;
                }
            }
            else
            {
                bool itemFound = false;
                TextBox txtBox = null;
                treeViewCrowd.UpdateLayout();
                if (this.viewModel.SelectedCrowdModel == null)
                {
                    var allCharsCrowd = this.viewModel.CrowdCollection[Constants.ALL_CHARACTER_CROWD_NAME];
                    if(allCharsCrowd != null)
                    {
                        TreeViewItem firstItem = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(allCharsCrowd) as TreeViewItem;
                        if (firstItem != null)
                        {
                            firstItem.IsSelected = true;
                            this.viewModel.SelectedCrowdModel = firstItem.DataContext as CrowdModel;
                        }
                    }
                }
                if (sender is CrowdModel)
                {
                    if (this.viewModel.SelectedCrowdModel == null || this.viewModel.SelectedCrowdModel.Name == Constants.ALL_CHARACTER_CROWD_NAME) // A new crowd has been added to the collection
                    {
                        for (int i = 0; i < treeViewCrowd.Items.Count; i++) 
                        {
                            TreeViewItem item = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(treeViewCrowd.Items[i]) as TreeViewItem;
                            if (item != null)
                            {
                                var model = item.DataContext as ICrowdMemberModel;
                                if (model.Name == modelToSelect.Name)
                                {
                                    item.IsSelected = true;
                                    itemFound = true;
                                    txtBox = FindTextBoxInTemplate(item);
                                    this.viewModel.SelectedCrowdModel = model as CrowdModel;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!itemFound)
                {
                    DependencyObject dObject = null;
                    if(e != null) // Something sent as event args
                    {
                        if(e.Value == "EditAfterDragDrop" && this.currentDropItemNodeParent != null)
                        {
                            dObject = currentDropItemNodeParent;
                        }
                    }
                    else
                    {
                        if (this.selectedCrowdRoot != null && this.viewModel.SelectedCrowdModel != null)
                        {
                            TreeViewItem item = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(this.selectedCrowdRoot) as TreeViewItem;
                            dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedCrowdModel.Name);
                        }
                        else
                            dObject = treeViewCrowd.GetItemFromSelectedObject(this.viewModel.SelectedCrowdModel);
                    }
                    TreeViewItem tvi = dObject as TreeViewItem; // got the selected treeviewitem
                    if (tvi != null)
                    {
                        ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                        if (tvi.Items != null)
                        {
                            tvi.IsExpanded = true;
                            tvi.UpdateLayout();
                            for (int i = 0; i < tvi.Items.Count; i++)
                            {
                                TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                                if (item != null)
                                {
                                    model = item.DataContext as ICrowdMemberModel;
                                    if (model.Name == modelToSelect.Name)
                                    {
                                        item.IsSelected = true;
                                        itemFound = true;                                       
                                        item.UpdateLayout();
                                        txtBox = FindTextBoxInTemplate(item);
                                        if(model is CrowdModel)
                                        {
                                            this.viewModel.SelectedCrowdModel = model as CrowdModel;
                                            this.viewModel.SelectedCrowdParent = tvi.DataContext as CrowdModel;
                                        }
                                        else
                                        {
                                            this.viewModel.SelectedCrowdMemberModel = model as CrowdMemberModel;
                                            this.viewModel.SelectedCrowdModel = tvi.DataContext as CrowdModel;
                                        }
                                        if(this.selectedCrowdRoot == null)
                                            this.selectedCrowdRoot = tvi.DataContext as CrowdModel;
                                        break;
                                    }
                                }
                            }
                        }
                    } 
                }
                if (txtBox != null)
                    this.viewModel.EnterEditModeCommand.Execute(txtBox);
            }
        }

        private TextBox FindTextBoxInTemplate(TreeViewItem item)
        {
            TextBox textBox = Helper.GetDescendantByType(item, typeof(TextBox)) as TextBox;
            return textBox;
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


        private void viewModel_FlattenNumberRequired(object sender, EventArgs e)
        {
            gridFlattenNumber.Visibility = Visibility.Visible;
            intUpDownFlattenNum.Focus();
        }

        private void viewModel_FlattenNumberEntryFinished(object sender, EventArgs e)
        {
            gridFlattenNumber.Visibility = Visibility.Collapsed;
        }

        private void treeViewCrowd_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);

            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);
            
            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                    this.selectedCrowdRoot = item.DataContext as CrowdModel;
                else
                    this.selectedCrowdRoot = null;
                if (treeViewItem.DataContext is CrowdModel)
                {
                    treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
                    if(treeViewItem != null)
                        this.viewModel.SelectedCrowdParent = treeViewItem.DataContext as CrowdModel;
                    else
                        this.viewModel.SelectedCrowdParent = null;
                    
                }
                else
                    this.viewModel.SelectedCrowdParent = null;
            }
        }
        private TreeViewItem VisualUpwardSearch(DependencyObject source)
        {
            while (source != null && !(source is TreeViewItem))
                source = VisualTreeHelper.GetParent(source);

            return source as TreeViewItem;
        }

        private TreeViewItem GetContainerFromItem(ItemsControl parent, object item)
        {
            var found = parent.ItemContainerGenerator.ContainerFromItem(item);
            if (found == null)
            {
                for (int i = 0; i < parent.Items.Count; i++)
                {
                    var childContainer = parent.ItemContainerGenerator.ContainerFromIndex(i) as ItemsControl;
                    TreeViewItem childFound = null;
                    if (childContainer != null)
                    {
                        bool expanded = (childContainer as TreeViewItem).IsExpanded;
                        (childContainer as TreeViewItem).IsExpanded = true;
                        childFound = GetContainerFromItem(childContainer, item);
                        (childContainer as TreeViewItem).IsExpanded = childFound == null ? expanded : true;
                    }
                    if (childFound != null)
                    {
                        (childContainer as TreeViewItem).IsExpanded = true;
                        return childFound;
                    }
                        
                }
            }
            return found as TreeViewItem;
        }

        private TreeViewItem GetImmediateTreeViewItemParent(TreeViewItem treeViewItem)
        {
            DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
            treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
            while (treeViewItem == null)
            {
                dObject = VisualTreeHelper.GetParent(dObject);
                treeViewItem = dObject as TreeViewItem;
                if (dObject is TreeView)
                    break;
            }
            return treeViewItem;
        }

        private object GetCurrentSelectedParent(object selectedObject)
        {
            treeViewCrowd.UpdateLayout();
            DependencyObject dObject = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(selectedObject); // doesn't work if the node has never been loaded into view 
            TreeViewItem tvi = dObject as TreeViewItem;
            TreeViewItem tviParent = GetImmediateTreeViewItemParent(tvi);
            if (tviParent != null)
                return tvi.DataContext;
            return null;
        }

        private TreeViewItem GetRootTreeViewItemParent(TreeViewItem treeViewItem)
        {
            DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
            treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
            while (true)
            {
                dObject = VisualTreeHelper.GetParent(dObject);
                if(dObject is TreeViewItem)
                    treeViewItem = dObject as TreeViewItem;
                else if (dObject is TreeView)
                    break;
            }
            return treeViewItem;
        }

        private void treeViewCrowd_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                    this.selectedCrowdRoot = item.DataContext as CrowdModel;
                else
                    this.selectedCrowdRoot = null;
                if (treeViewItem.DataContext is CrowdModel)
                {
                    treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
                    if (treeViewItem != null)
                        this.viewModel.SelectedCrowdParent = treeViewItem.DataContext as CrowdModel;
                    else 
                        this.viewModel.SelectedCrowdParent = null;
                }
                else
                    this.viewModel.SelectedCrowdParent = null;
            }
        }

        private TreeViewItem FindTreeViewItemUnderTreeViewItemByModelName(TreeViewItem tvi, string modelName)
        {
            TreeViewItem treeViewItemRet = null;
            if (tvi != null)
            {
                ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                if (model.Name == modelName)
                {
                    return tvi;
                }
                else if (tvi.Items != null)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        var treeViewItem = FindTreeViewItemUnderTreeViewItemByModelName(item, modelName);
                        if (treeViewItem != null)
                        {
                            treeViewItemRet = treeViewItem;
                            break;
                        }
                    }
                }
            }
            return treeViewItemRet;
        }
        #region TreeView Expansion Management

        private void viewModel_ExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            CrowdModel crowdModel = sender as CrowdModel;
            DependencyObject dObject = null;
            ExpansionUpdateEvent updateEvent = e.Value;
            if(updateEvent == ExpansionUpdateEvent.Filter)
            {
                ExpandMatchedNode(sender);
            }
            else if(updateEvent == ExpansionUpdateEvent.DragDrop)
            {
                if (this.currentDropItemNode != null)
                    this.currentDropItemNode.IsExpanded = true;
            }
            else
            {
                if (this.selectedCrowdRoot != null && crowdModel != null)
                {
                    TreeViewItem item = treeViewCrowd.ItemContainerGenerator.ContainerFromItem(this.selectedCrowdRoot) as TreeViewItem;
                    dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, crowdModel.Name);
                    if (dObject == null)
                        dObject = treeViewCrowd.GetItemFromSelectedObject(crowdModel);
                }
                else
                    dObject = treeViewCrowd.GetItemFromSelectedObject(crowdModel);
                TreeViewItem tvi = dObject as TreeViewItem; 
                if (tvi != null)
                {
                    ICrowdMemberModel model = tvi.DataContext as ICrowdMemberModel;
                    if (tvi.Items != null && tvi.Items.Count > 0)
                    {
                        if (updateEvent != ExpansionUpdateEvent.Delete)
                            tvi.IsExpanded = true;
                        else
                            UpdateExpansions(tvi);
                    }
                    else
                        tvi.IsExpanded = false;
                }
            }
        }
        /// <summary>
        /// This will recursively make the nodes unexpanded if there are no children in it. Otherwise it will hold the current state
        /// </summary>
        /// <param name="tvi"></param>
        private void UpdateExpansions(TreeViewItem tvi)
        {
            if(tvi != null)
            {
                if (tvi.Items != null && tvi.Items.Count > 0)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        UpdateExpansions(item);
                    }
                }
                else
                    tvi.IsExpanded = false;
            }
        }

        /// <summary>
        /// This will expand a matched item and its matched children
        /// </summary>
        /// <param name="sender"></param>
        private void ExpandMatchedNode(object sender)
        {
            CrowdModel crowdModel = sender as CrowdModel;
            if(crowdModel.IsMatched)
            {
                DependencyObject dObject = treeViewCrowd.GetItemFromSelectedObject(crowdModel);
                TreeViewItem tvi = dObject as TreeViewItem; 
                if (tvi != null)
                {
                    tvi.IsExpanded = true;
                    ExpandMatchedItems(tvi);
                }
            }
        }
        /// <summary>
        /// This will recursively expand a matched item and its matched children
        /// </summary>
        /// <param name="tvi"></param>
        private void ExpandMatchedItems(TreeViewItem tvi)
        {
            if (tvi != null)
            {
                tvi.UpdateLayout();
                if (tvi.Items != null && tvi.Items.Count > 0)
                {
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        if (item != null)
                        {
                            CrowdModel model = item.DataContext as CrowdModel;
                            if (model != null && model.IsMatched)
                            {
                                item.IsExpanded = true;
                                ExpandMatchedItems(item);
                            }
                            else
                                item.IsExpanded = false; 
                        }
                    }
                }
            }
        }
        #endregion

        #region Drag Drop

        bool isDragging = false;
        Point startPoint;
        TreeViewItem currentDropItemNode;
        TreeViewItem currentDropItemNodeParent;

        private void StartDrag(TreeView tv, MouseEventArgs e)
        {
            isDragging = true;
            try
            {
                // Get the dragged ListViewItem
                TreeView treeView = tv as TreeView;
                TreeViewItem treeViewItem =
                    Helper.FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                if(treeViewItem != null)
                {
                    // Find the data behind the TreeViewItem
                    //AnimationElement elementBehind = (AnimationElement)treeView.ItemContainerGenerator.ItemFromContainer(treeViewItem);
                    var element = treeView.SelectedItem;
                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY, element);
                    DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Move);
                }
            
            }
            catch(Exception ex)
            {

            }
            finally
            {
                isDragging = false;
            }
        }
        private void treeViewCrowd_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Action action = delegate()
            {
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging && !Helper.GlobalVariables_IsPlayingAttack)
                {
                    // Get the current mouse position
                    Point mousePos = e.GetPosition(null);
                    Vector diff = startPoint - mousePos;
                    if (
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        StartDrag(sender as TreeView, e);
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void FindDropTarget(TreeView tv, out TreeViewItem itemNode, DragEventArgs dragEventArgs)
        {
            itemNode = null;

            DependencyObject k = VisualTreeHelper.HitTest(tv, dragEventArgs.GetPosition(tv)).VisualHit;

            while (k != null)
            {
                if (k is TreeViewItem)
                {
                    TreeViewItem treeNode = k as TreeViewItem;
                    if (treeNode.DataContext is ICrowdModel || treeNode.DataContext is ICrowdMemberModel)
                    {
                        itemNode = treeNode;
                        break;
                    }
                }
                else if (k == tv)
                {
                    break;
                }

                k = VisualTreeHelper.GetParent(k);
            }
        }

        private void treeViewCrowd_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void treeViewCrowd_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY))
            {
                
                FindDropTarget((TreeView)sender, out currentDropItemNode, e);
                
                if (currentDropItemNode != null)
                {
                    var dropMember = (currentDropItemNode != null && currentDropItemNode.IsVisible ? currentDropItemNode.DataContext : null);
                    var dragMember = e.Data.GetData(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY);
                    CrowdModel targetCrowd = null;
                    if(dropMember is CrowdModel)
                    {
                        targetCrowd = dropMember as CrowdModel;
                        currentDropItemNodeParent = currentDropItemNode;
                    }
                    else
                    {
                        currentDropItemNodeParent = GetImmediateTreeViewItemParent(currentDropItemNode);
                        targetCrowd = currentDropItemNodeParent != null ? currentDropItemNodeParent.DataContext as CrowdModel : null;
                    }
                    
                    this.viewModel.DragDropSelectedCrowdMember(targetCrowd);
                }
            }
        }

        private void textBlockCrowdMember_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void textBlockCrowdMember_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY))
            {
                e.Handled = true;
            }
        }

        private void textBlockCrowdMember_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.CROWD_MEMBER_DRAG_FROM_CHAR_XPLORER_KEY))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }
        #endregion
    }
}
