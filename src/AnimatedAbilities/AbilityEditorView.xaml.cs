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
using Framework.WPF.Extensions;
using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared.Events;
using Module.HeroVirtualTabletop.Library.Enumerations;
using Xceed.Wpf.Toolkit;
using Module.Shared;
using System.Windows.Controls.Primitives;

namespace HeroVTT.AnimatedAbilities { 
    /// <summary>
    /// Interaction logic for AbilityEditorView.xaml
    /// </summary>
    public partial class AbilityEditorView : UserControl
    {
        private AbilityEditorViewModel viewModel;

        public AbilityEditorView(AbilityEditorViewModel viewModel)
        {
            InitializeComponent();
            this.viewModel = viewModel;
            this.DataContext = this.viewModel;
            this.viewModel.EditModeEnter += viewModel_EditModeEnter;
            this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            this.viewModel.AnimationAdded += viewModel_AnimationAdded;
            this.viewModel.SelectionChanged += viewModel_SelectionChanged;
            this.viewModel.ExpansionUpdateNeeded += viewModel_ExpansionUpdateNeeded;
            this.viewModel.AnimationElementDraggedFromGrid += viewModel_AnimationElementDraggedFromGrid;
        }

        //private void UpdateDataGrid(IAnimationElement element)
        //{
        //    if (element == null)
        //    {
        //        dataGridAnimationResource.SetBinding(DataGrid.ItemsSourceProperty, new Binding());
        //        return;
        //    }
        //    Visibility colorsGridVisibility = Visibility.Collapsed;
        //    switch (element.Type)
        //    {
        //        case Library.Enumerations.AnimationType.Movement:
        //            dataGridAnimationResource.SetBinding(DataGrid.ItemsSourceProperty, new Binding("MOVResourcesCVS.View"));
        //            break;
        //        case Library.Enumerations.AnimationType.FX:
        //            dataGridAnimationResource.SetBinding(DataGrid.ItemsSourceProperty, new Binding("FXResourcesCVS.View"));
        //            colorsGridVisibility = Visibility.Visible;
        //            break;
        //        case Library.Enumerations.AnimationType.Sound:
        //            dataGridAnimationResource.SetBinding(DataGrid.ItemsSourceProperty, new Binding("SoundResourcesCVS.View"));
        //            break;
        //    }
        //    colorsGrid.Visibility = colorsGridVisibility;
        //}

        private void viewModel_SelectionChanged(object sender, EventArgs e)
        {
            //this.UpdateDataGrid(sender as IAnimationElement);
            if (!string.IsNullOrEmpty(this.viewModel.Filter))
            {
                txtBoxAnimationResourceFilter.Focus();
                txtBoxAnimationResourceFilter.CaretIndex = txtBoxAnimationResourceFilter.Text.Length;
            }
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

        private void viewModel_AnimationAdded(object sender, CustomEventArgs<bool> e)
        {
            IAnimationElement modelToSelect = sender as IAnimationElement;
            if (sender == null) // Need to unselect
            {
                DependencyObject dObject = treeViewAnimations.GetItemFromSelectedObject(treeViewAnimations.SelectedItem);
                TreeViewItem tvi = dObject as TreeViewItem; // Got the selected treeviewitem
                if (tvi != null)
                {
                    tvi.IsSelected = false;
                    this.viewModel.SelectedAnimationElementRoot = null;
                }
            }
            else
            {
                bool itemFound = false;
                TextBox txtBox = null;
                treeViewAnimations.UpdateLayout();
                if (sender is IAnimationElement)
                {
                    if (this.viewModel.SelectedAnimationElement == null || !(this.viewModel.SelectedAnimationElement is SequenceElement || this.viewModel.SelectedAnimationParent is SequenceElement)) // A new animation has been added to the collection
                    {
                        for (int i = 0; i < treeViewAnimations.Items.Count; i++)
                        {
                            TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(treeViewAnimations.Items[i]) as TreeViewItem;
                            if (item != null)
                            {
                                var model = item.DataContext as IAnimationElement;
                                if (model.Name == modelToSelect.Name)
                                {
                                    item.IsSelected = true;
                                    itemFound = true;
                                    txtBox = FindTextBoxInTemplate(item);
                                    this.viewModel.SelectedAnimationElement = model as IAnimationElement;
                                    this.viewModel.SelectedAnimationParent = null;
                                    this.viewModel.SelectedAnimationElementRoot = null;
                                    break;
                                }
                            }
                        }
                    }
                }
                if (!itemFound && (this.viewModel.SelectedAnimationElement is SequenceElement || this.viewModel.SelectedAnimationParent is SequenceElement)) // Added somewhere in nested animation
                {
                    DependencyObject dObject = null;
                    if (this.viewModel.SelectedAnimationElementRoot != null && this.viewModel.SelectedAnimationParent != null)
                    {
                        if (this.viewModel.SelectedAnimationElement is SequenceElement) // Sequence within a sequence
                        {
                            TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(this.viewModel.SelectedAnimationElementRoot) as TreeViewItem;
                            dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedAnimationElement.Name);
                        }
                        else if (this.viewModel.SelectedAnimationElementRoot.Name == this.viewModel.SelectedAnimationParent.Name) // They are the same element
                            dObject = treeViewAnimations.GetItemFromSelectedObject(this.viewModel.SelectedAnimationParent);
                        else
                        {
                            TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(this.viewModel.SelectedAnimationElementRoot) as TreeViewItem;
                            dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedAnimationParent.Name);
                        }
                    }
                    else if (this.viewModel.SelectedAnimationElementRoot == null && this.viewModel.SelectedAnimationElement is SequenceElement)
                        dObject = treeViewAnimations.GetItemFromSelectedObject(this.viewModel.SelectedAnimationElement);
                    TreeViewItem tvi = dObject as TreeViewItem; // Got the selected treeviewitem
                    if (tvi != null)
                    {
                        IAnimationElement model = tvi.DataContext as IAnimationElement;
                        if (tvi.Items != null)
                        {
                            tvi.IsExpanded = true;
                            tvi.UpdateLayout();
                            for (int i = 0; i < tvi.Items.Count; i++)
                            {
                                TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                                if (item != null)
                                {
                                    model = item.DataContext as IAnimationElement;
                                    if (model.Name == modelToSelect.Name)
                                    {
                                        item.IsSelected = true;
                                        itemFound = true;
                                        item.UpdateLayout();
                                        txtBox = FindTextBoxInTemplate(item);
                                        this.viewModel.SelectedAnimationElement = model as IAnimationElement;
                                        this.viewModel.SelectedAnimationParent = tvi.DataContext as IAnimationElement;
                                        if (this.viewModel.SelectedAnimationElementRoot == null)
                                            this.viewModel.SelectedAnimationElementRoot = tvi.DataContext as IAnimationElement;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (txtBox != null && this.viewModel.SelectedAnimationElement is PauseElement)
                {
                    if (!(e != null && e.Value == false)) // to avoid renaming in case of cut-paste or drag-drop
                        this.viewModel.EnterAnimationElementEditModeCommand.Execute(txtBox);
                }
            }
            //this.UpdateDataGrid(modelToSelect);
        }

        private void viewModel_AnimationElementDraggedFromGrid(object sender, EventArgs e)
        {
            IAnimationElement modelToSelect = sender as IAnimationElement;
            TreeViewItem tvi = itemNodeParent; // Got the selected treeviewitem
            if (tvi != null)
            {
                IAnimationElement model = tvi.DataContext as IAnimationElement;
                if (tvi.Items != null)
                {
                    tvi.IsExpanded = true;
                    tvi.UpdateLayout();
                    for (int i = 0; i < tvi.Items.Count; i++)
                    {
                        TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
                        if (item != null)
                        {
                            model = item.DataContext as IAnimationElement;
                            if (model.Name == modelToSelect.Name)
                            {
                                item.IsSelected = true;
                                item.UpdateLayout();
                                this.viewModel.SelectedAnimationElement = model as IAnimationElement;
                                this.viewModel.SelectedAnimationParent = tvi.DataContext as IAnimationElement;
                                TreeViewItem rootItem = GetRootTreeViewItemParent(item);
                                if (rootItem != null)
                                {
                                    this.viewModel.SelectedAnimationElementRoot = rootItem.DataContext as IAnimationElement;
                                    if (this.viewModel.SelectedAnimationElementRoot is SequenceElement)
                                        this.viewModel.IsSequenceAbilitySelected = true;
                                    else
                                        this.viewModel.IsSequenceAbilitySelected = false;
                                }
                                else
                                    this.viewModel.SelectedAnimationElementRoot = null;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private TextBox FindTextBoxInTemplate(TreeViewItem item)
        {
            TextBox textBox = Helper.GetDescendantByType(item, typeof(TextBox)) as TextBox;
            return textBox;
        }
        private TreeViewItem FindTreeViewItemUnderTreeViewItemByModelName(TreeViewItem tvi, string modelName)
        {
            TreeViewItem treeViewItemRet = null;
            if (tvi != null)
            {
                IAnimationElement model = tvi.DataContext as IAnimationElement;
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
        private void treeViewAnimations_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                startPoint = e.GetPosition(null);
            }

            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                treeViewItem.Focus();
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                {
                    this.viewModel.SelectedAnimationElementRoot = item.DataContext as IAnimationElement;
                    if (this.viewModel.SelectedAnimationElementRoot is SequenceElement)
                        this.viewModel.IsSequenceAbilitySelected = true;
                    else
                        this.viewModel.IsSequenceAbilitySelected = false;
                }
                else
                    this.viewModel.SelectedAnimationElementRoot = null;
                //if (treeViewItem.DataContext is SequenceElement)
                {
                    treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
                    if (treeViewItem != null)
                        this.viewModel.SelectedAnimationParent = treeViewItem.DataContext as IAnimationElement;
                    else
                        this.viewModel.SelectedAnimationParent = null;

                }
                //else
                //    this.viewModel.SelectedAnimationParent = null;
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

        private TreeViewItem GetRootTreeViewItemParent(TreeViewItem treeViewItem)
        {
            DependencyObject dObject = VisualTreeHelper.GetParent(treeViewItem); // got the immediate parent
            treeViewItem = dObject as TreeViewItem; // now get first treeview item parent
            while (true)
            {
                dObject = VisualTreeHelper.GetParent(dObject);
                if (dObject is TreeViewItem)
                    treeViewItem = dObject as TreeViewItem;
                else if (dObject is TreeView)
                    break;
            }
            return treeViewItem;
        }

        private TreeViewItem GetNearestContainer(UIElement element)
        {
            // Walk up the element tree to the nearest tree view item.
            TreeViewItem container = element as TreeViewItem;
            while ((container == null) && (element != null))
            {
                element = VisualTreeHelper.GetParent(element) as UIElement;
                container = element as TreeViewItem;
            }
            return container;
        }

        private void treeViewAnimations_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

            if (treeViewItem != null)
            {
                TreeViewItem item = GetRootTreeViewItemParent(treeViewItem);
                if (item != null)
                {
                    this.viewModel.SelectedAnimationElementRoot = item.DataContext as IAnimationElement;
                    if (this.viewModel.SelectedAnimationElementRoot is SequenceElement)
                        this.viewModel.IsSequenceAbilitySelected = true;
                    else
                        this.viewModel.IsSequenceAbilitySelected = false;
                }
                else
                    this.viewModel.SelectedAnimationElementRoot = null;

                treeViewItem = GetImmediateTreeViewItemParent(treeViewItem);
                if (treeViewItem != null)
                    this.viewModel.SelectedAnimationParent = treeViewItem.DataContext as IAnimationElement;
                else
                    this.viewModel.SelectedAnimationParent = null;
            }
        }

        #region TreeView Expansion Management

        private void viewModel_ExpansionUpdateNeeded(object sender, CustomEventArgs<ExpansionUpdateEvent> e)
        {
            SequenceElement sequenceElement = sender as SequenceElement;
            DependencyObject dObject = null;
            ExpansionUpdateEvent updateEvent = e.Value;
            if (updateEvent == ExpansionUpdateEvent.Filter)
            {
                ExpandMatchedNode(sender); // Keeping way open for future filtering feature
            }
            else
            {
                if (this.viewModel.SelectedAnimationElementRoot != null && this.viewModel.SelectedAnimationParent != null)
                {
                    if (this.viewModel.SelectedAnimationElement is SequenceElement) // Sequence within a sequence
                    {
                        TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(this.viewModel.SelectedAnimationElementRoot) as TreeViewItem;
                        dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedAnimationElement.Name);
                    }
                    else if (this.viewModel.SelectedAnimationElementRoot.Name == this.viewModel.SelectedAnimationParent.Name) // They are the same element
                        dObject = treeViewAnimations.GetItemFromSelectedObject(this.viewModel.SelectedAnimationParent);
                    else
                    {
                        TreeViewItem item = treeViewAnimations.ItemContainerGenerator.ContainerFromItem(this.viewModel.SelectedAnimationElementRoot) as TreeViewItem;
                        dObject = FindTreeViewItemUnderTreeViewItemByModelName(item, this.viewModel.SelectedAnimationParent.Name);
                    }
                }
                else if (this.viewModel.SelectedAnimationElementRoot == null && this.viewModel.SelectedAnimationElement is SequenceElement)
                    dObject = treeViewAnimations.GetItemFromSelectedObject(this.viewModel.SelectedAnimationElement);
                TreeViewItem tvi = dObject as TreeViewItem;
                if (tvi != null)
                {
                    IAnimationElement model = tvi.DataContext as IAnimationElement;
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
            if (tvi != null)
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
            //SequenceElement sequenceElement = sender as SequenceElement;
            //if (sequenceElement.IsMatched)
            //{
            //    DependencyObject dObject = this.treeViewAnimations.GetItemFromSelectedObject(sequenceElement);
            //    TreeViewItem tvi = dObject as TreeViewItem;
            //    if (tvi != null)
            //    {
            //        tvi.IsExpanded = true;
            //        ExpandMatchedItems(tvi);
            //    }
            //}
        }
        /// <summary>
        /// This will recursively expand a matched item and its matched children
        /// </summary>
        /// <param name="tvi"></param>
        private void ExpandMatchedItems(TreeViewItem tvi)
        {
            //if (tvi != null)
            //{
            //    tvi.UpdateLayout();
            //    if (tvi.Items != null && tvi.Items.Count > 0)
            //    {
            //        for (int i = 0; i < tvi.Items.Count; i++)
            //        {
            //            TreeViewItem item = tvi.ItemContainerGenerator.ContainerFromItem(tvi.Items[i]) as TreeViewItem;
            //            if (item != null)
            //            {
            //                SequenceElement model = item.DataContext as SequenceElement;
            //                if (model != null && model.IsMatched)
            //                {
            //                    item.IsExpanded = true;
            //                    ExpandMatchedItems(item);
            //                }
            //                else
            //                    item.IsExpanded = false;
            //            }
            //        }
            //    }
            //}
        }
        #endregion

        #region Drag Drop

        bool isDragging = false;
        Point startPoint;


        private void StartDrag(TreeView tv, MouseEventArgs e)
        {
            isDragging = true;
            try
            {
                // Get the dragged ListViewItem
                TreeView treeView = tv as TreeView;
                TreeViewItem treeViewItem =
                    Helper.FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
                if (treeViewItem != null)
                {
                    // Find the data behind the ListViewItem
                    //AnimationElement elementBehind = (AnimationElement)treeView.ItemContainerGenerator.ItemFromContainer(treeViewItem);
                    AnimationElement element = (AnimationElement)treeView.SelectedItem;
                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject(Constants.ANIMATION_DRAG_KEY, element);
                    DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                isDragging = false;
            }
        }
        private void treeViewAnimations_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Action action = delegate()
            {
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
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
                    if (treeNode.DataContext is AnimationElement)
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

        private void treeViewAnimations_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(Constants.ANIMATION_DRAG_KEY) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
        }
        TreeViewItem itemNodeParent = null;
        private void treeViewAnimations_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.ANIMATION_DRAG_KEY))
            {
                TreeViewItem itemNode;
                FindDropTarget((TreeView)sender, out itemNode, e);

                if (itemNode != null)
                {
                    AnimationElement dropAnimationElement = (itemNode != null && itemNode.IsVisible ? itemNode.DataContext as AnimationElement : null);
                    AnimationElement dragAnimationElement = e.Data.GetData(Constants.ANIMATION_DRAG_KEY) as AnimationElement;
                    SequenceElement dropAnimationElementParent = null;
                    if (dropAnimationElement is SequenceElement)
                    {
                        itemNodeParent = itemNode;
                        dropAnimationElementParent = dropAnimationElement as SequenceElement;
                    }
                    else
                    {
                        itemNodeParent = GetImmediateTreeViewItemParent(itemNode);
                        dropAnimationElementParent = itemNodeParent != null ? itemNodeParent.DataContext as SequenceElement : null;
                    }
                    try
                    {
                        if (dragAnimationElement is AnimatedAbility)
                        {
                            // Drag drop of Reference Ability
                            this.viewModel.MoveReferenceAbilityToAnimationElements(dragAnimationElement as AnimatedAbility, dropAnimationElementParent, dropAnimationElement.Order);
                        }
                        else
                        {
                            // Drag drop of animations
                            this.viewModel.MoveSelectedAnimationElement(dropAnimationElementParent, dropAnimationElement.Order);
                        }
                    }
                    catch(Exception ex)
                    {

                    }
                }
            }
        }

        private void textBlockAnimationElement_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.ANIMATION_DRAG_KEY))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }

        private void textBlockAnimationElement_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.ANIMATION_DRAG_KEY))
            {
                e.Handled = true;
            }
        }

        private void textBlockAnimationElementChild_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.ANIMATION_DRAG_KEY))
            {
                e.Effects = DragDropEffects.Move;
                e.Handled = true;
            }
        }
        private void dataGridAbilityReferences_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                startPoint = e.GetPosition(null);
            }
        }

        private delegate Point GetPosition(IInputElement element);

        private void dataGridAbilityReferences_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Action action = delegate()
            {
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging)
                {
                    // Get the current mouse position
                    Point mousePos = e.GetPosition(null);
                    Vector diff = startPoint - mousePos;
                    if (
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        StartDataGridDrag(sender as DataGrid, e);
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void StartDataGridDrag(DataGrid sender, MouseEventArgs e)
        {
            isDragging = true;
            try
            {
                int rowIndex = GetCurrentRowIndex(e.GetPosition);
                if (rowIndex < 0)
                    return;
                this.dataGridAbilityReferences.SelectedIndex = rowIndex;
                AnimationResource animationResource = dataGridAbilityReferences.Items[rowIndex] as AnimationResource;
                if (animationResource != null)
                {
                    DataObject dragData = new DataObject(Constants.ANIMATION_DRAG_KEY, animationResource.Reference);
                    //DragDrop.DoDragDrop(dataGridAbilityReferences, dragData, DragDropEffects.Move);
                    if (DragDrop.DoDragDrop(dataGridAbilityReferences, dragData, DragDropEffects.Move)
                                        != DragDropEffects.None)
                    {
                        dataGridAbilityReferences.SelectedItem = animationResource;
                    }
                }

            }
            catch (Exception ex)
            {

            }
            finally
            {
                isDragging = false;
            }
        }
        private bool GetMouseTargetRow(Visual theTarget, GetPosition position)
        {
            if (theTarget == null)
                return false;
            Rect rect = VisualTreeHelper.GetDescendantBounds(theTarget);
            Point point = position((IInputElement)theTarget);
            return rect.Contains(point);
        }
        private DataGridRow GetRowItem(int index)
        {
            if (dataGridAbilityReferences.ItemContainerGenerator.Status
                    != GeneratorStatus.ContainersGenerated)
                return null;
            return dataGridAbilityReferences.ItemContainerGenerator.ContainerFromIndex(index)
                                                            as DataGridRow;
        }
        private int GetCurrentRowIndex(GetPosition pos)
        {
            int curIndex = -1;
            for (int i = 0; i < dataGridAbilityReferences.Items.Count; i++)
            {
                DataGridRow itm = GetRowItem(i);
                if (GetMouseTargetRow(itm, pos))
                {
                    curIndex = i;
                    break;
                }
            }
            return curIndex;
        }
        #endregion

        private void btnFXAnimation_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
