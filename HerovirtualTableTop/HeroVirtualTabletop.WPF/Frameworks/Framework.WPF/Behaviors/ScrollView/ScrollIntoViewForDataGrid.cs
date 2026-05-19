using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interactivity;


namespace Framework.WPF.Behaviors
{
    public class ScrollIntoViewForDataGrid : Behavior<DataGrid>
    {
        /// <summary>
        ///  When Beahvior is attached
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();
            this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
        }

        /// <summary>
        /// On Selection Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AssociatedObject_SelectionChanged(object sender,
                                               SelectionChangedEventArgs e)
        {
            if (sender is DataGrid)
            {
                DataGrid dGrid = (sender as DataGrid);
                if (dGrid.SelectedItem != null)
                {
                    dGrid.Dispatcher.BeginInvoke(
                        (Action)(() =>
                        {
                            dGrid.UpdateLayout();
                            if (dGrid.SelectedItem !=
                            null)
                                dGrid.ScrollIntoView(
                                dGrid.SelectedItem);
                        }));
                }
            }
        }
        /// <summary>
        /// When behavior is detached
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            this.AssociatedObject.SelectionChanged -=
                AssociatedObject_SelectionChanged;

        }
    }
}
