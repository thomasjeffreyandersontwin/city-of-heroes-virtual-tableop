using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Framework.WPF.Library
{
    public class MultiSelectListBox : ListBox
    {
        public MultiSelectListBox()
        {
            SelectionChanged += MultiSelectListBox_SelectionChanged;
        }

        void MultiSelectListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItemsList = SelectedItems;
        }

        public IList SelectedItemsList
        {
            get { return (IList)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
           DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(MultiSelectListBox), new PropertyMetadata(null));

    }
}
