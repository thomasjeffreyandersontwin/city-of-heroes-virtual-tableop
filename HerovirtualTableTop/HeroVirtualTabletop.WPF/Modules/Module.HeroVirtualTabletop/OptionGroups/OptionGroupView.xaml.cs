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

namespace Module.HeroVirtualTabletop.OptionGroups
{
    /// <summary>
    /// Interaction logic for OptionGroupView.xaml
    /// </summary>
    public partial class OptionGroupView : UserControl
    {
        private IOptionGroupViewModel viewModel;

        public OptionGroupView()
        {
            InitializeComponent();

            this.DataContextChanged += (sender, e) =>
            {
                this.viewModel = this.DataContext as IOptionGroupViewModel;
                this.viewModel.EditModeEnter += viewModel_EditModeEnter;
                this.viewModel.EditModeLeave += viewModel_EditModeLeave;
            };
            Style itemContainerStyle = this.optionListBox.ItemContainerStyle;
            if(itemContainerStyle != null && itemContainerStyle.Setters != null)
            {
                itemContainerStyle.Setters.Add(new Setter(ListBoxItem.AllowDropProperty, true));

                itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonDown)));
                itemContainerStyle.Setters.Add(new EventSetter(ListBoxItem.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonUp)));
            }
        }

        #region Rename
        private void viewModel_EditModeEnter(object sender, EventArgs e)
        {
            Border headborder = (Border)this.grpBoxOptionGroup.Template.FindName("Header", this.grpBoxOptionGroup);

            if(headborder != null)
            {
                ContentPresenter headContentPresenter = (ContentPresenter)headborder.Child;

                var dataTemplate = this.grpBoxOptionGroup.HeaderTemplate;
                TextBlock headerTextBlock = dataTemplate.FindName("textBlockName", headContentPresenter) as TextBlock;
                TextBox headerTextBox = dataTemplate.FindName("textBoxName", headContentPresenter) as TextBox;
                headerTextBox.Text = headerTextBlock.Text;
                headerTextBox.Visibility = Visibility.Visible;
                headerTextBox.Focus();
                headerTextBox.SelectAll();
            }
        }

        private void viewModel_EditModeLeave(object sender, EventArgs e)
        {
            TextBox txtBox = sender as TextBox;
            BindingExpression expression = txtBox.GetBindingExpression(TextBox.TextProperty);
            expression.UpdateSource();
            txtBox.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Drag Drop

        bool isDragging = false;
        Point startPoint;
        ListBoxItem dataItem = null;
        private void StartDrag(ListBoxItem listBoxItem, MouseEventArgs e)
        {
            isDragging = true;
            try
            {
                if (listBoxItem != null)
                {
                    // Find the data behind the ListBoxItem
                    ICharacterOption option = (ICharacterOption)listBoxItem.DataContext;
                    int sourceIndex = optionListBox.Items.IndexOf(option);
                   
                    // Initialize the drag & drop operation
                    DataObject dragData = new DataObject(Constants.OPTION_DRAG_KEY, new Tuple<IOptionGroupViewModel, int, ICharacterOption>(this.viewModel, sourceIndex, option));
                    DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                isDragging = false;
                dataItem = null;
            }
        }
        private void groupbox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Action action = delegate()
            {
                if (e.LeftButton == MouseButtonState.Pressed && !isDragging && dataItem != null && !Helper.GlobalVariables_IsPlayingAttack && !this.viewModel.IsReadOnlyMode)
                {
                    // Get the current mouse position
                    Point mousePos = e.GetPosition(null);
                    Vector diff = startPoint - mousePos;
                    if (
                    Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                    {
                        StartDrag(dataItem as ListBoxItem, e);
                    }
                }
            };
            Application.Current.Dispatcher.BeginInvoke(action);
        }

        private void FindDropTarget(GroupBox lb, out ListBoxItem listBoxItem, DragEventArgs dragEventArgs)
        {
            listBoxItem = null;

            DependencyObject k = VisualTreeHelper.HitTest(lb, dragEventArgs.GetPosition(lb)).VisualHit;

            while (k != null)
            {
                if (k is ListBoxItem)
                {
                    ListBoxItem lbItem = k as ListBoxItem;
                    if (lbItem.DataContext is ICharacterOption)
                    {
                        listBoxItem = lbItem;
                        break;
                    }
                }
                else if (k == lb)
                {
                    break;
                }

                k = VisualTreeHelper.GetParent(k);
            }
        }
        private bool expanderExpandedForDrop = false;
        private void grpBoxOptionGroup_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(Constants.OPTION_DRAG_KEY) || sender == e.Source)
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                Tuple<IOptionGroupViewModel, int, ICharacterOption> dragDropDataTuple = e.Data.GetData(Constants.OPTION_DRAG_KEY) as Tuple<IOptionGroupViewModel, int, ICharacterOption>;
                IOptionGroupViewModel sourceViewModel = dragDropDataTuple.Item1;
                if(sourceViewModel != this.viewModel && this.viewModel.OptionGroup.Type != OptionType.Mixed)
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            if(e.Effects != DragDropEffects.None && !this.ExpanderOptionGroup.IsExpanded)
            {
                this.ExpanderOptionGroup.IsExpanded = expanderExpandedForDrop = true;
            }
            e.Handled = true;
        }

        private void GroupBox_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if(expanderExpandedForDrop)
            {
                this.ExpanderOptionGroup.IsExpanded = expanderExpandedForDrop = false;
            }
        }

        private void GroupBox_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(Constants.OPTION_DRAG_KEY))
            {
                expanderExpandedForDrop = false;
                GroupBox groupBox = (GroupBox)sender;
                Tuple<IOptionGroupViewModel, int, ICharacterOption> dragDropDataTuple = e.Data.GetData(Constants.OPTION_DRAG_KEY) as Tuple<IOptionGroupViewModel, int, ICharacterOption>;
                if(dragDropDataTuple != null)
                {
                    IOptionGroupViewModel sourceViewModel = dragDropDataTuple.Item1;
                    int sourceIndex = dragDropDataTuple.Item2;
                    ICharacterOption option = dragDropDataTuple.Item3;
                    if (this.viewModel.OptionGroup.Type == OptionType.Mixed && sourceViewModel.OptionGroup.Type != OptionType.Mixed) // do a copy paste
                    {
                        sourceIndex = -1;
                    }
                    int targetIndex = 0;

                    ListBoxItem listBoxItem;
                    FindDropTarget(groupBox, out listBoxItem, e);
                    if (listBoxItem != null)
                    {
                        ICharacterOption target = listBoxItem.DataContext as ICharacterOption;
                        if (dragDropDataTuple != null && target != null)
                        {
                            targetIndex = optionListBox.Items.IndexOf(target);
                        }
                    }
                    else
                    {
                        targetIndex = optionListBox.Items != null ? optionListBox.Items.Count : 0; // append to last of current option group
                        if (sourceIndex >= 0 && this.viewModel == sourceViewModel) // an item will be removed from the current option group, so reduce target index by 1
                            targetIndex -= 1;
                    }
                    if(sourceViewModel != null && sourceIndex >= 0)
                    {
                        sourceViewModel.RemoveOption(sourceIndex);
                    }
                    this.viewModel.InsertOption(targetIndex, option);
                    this.viewModel.SaveOptionGroup();
                }
            }
        }

        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
            dataItem = sender as ListBoxItem;
        }

        private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            dataItem = null;
        }

        private void textBlockName_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Helper.GlobalVariables_OptionGroupDragStartPoint = e.GetPosition(null);
            Helper.GlobalVariables_DraggingOptionGroupName = this.viewModel.OptionGroup.Name;
        }

        #endregion     

        #region Adjustable Width

        public double OptionGroupWidth
        {
            get
            {
                return (double)GetValue(OptionGroupWidthProperty);
            }
            set 
            {
                SetValue(OptionGroupWidthProperty, value); 
            }
        }

        public static readonly DependencyProperty
            OptionGroupWidthProperty =
            DependencyProperty.Register("OptionGroupWidth",
            typeof(double), typeof(OptionGroupView),
            new PropertyMetadata(null));


        public double OptionListBoxWidth
        {
            get
            {
                return (double)GetValue(OptionListBoxWidthProperty);
            }
            set
            {
                SetValue(OptionListBoxWidthProperty, value);
            }
        }

        public static readonly DependencyProperty
            OptionListBoxWidthProperty =
            DependencyProperty.Register("OptionListBoxWidth",
            typeof(double), typeof(OptionGroupView),
            new PropertyMetadata(null));

        public int NumberOfOptionsPerRow
        {
            get
            {
                return (int)GetValue(NumberOfOptionsPerRowProperty);
            }
            set
            {
                SetValue(NumberOfOptionsPerRowProperty, value);
            }
        }

        public static readonly DependencyProperty
            NumberOfOptionsPerRowProperty =
            DependencyProperty.Register("NumberOfOptionsPerRow",
            typeof(int), typeof(OptionGroupView),
            new PropertyMetadata(null));
        #endregion
    }
}
