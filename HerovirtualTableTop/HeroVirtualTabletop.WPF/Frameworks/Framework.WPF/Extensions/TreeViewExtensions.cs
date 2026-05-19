using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Framework.WPF.Extensions
{
    public static class TreeViewHelper
    {
        /// <summary>
        /// Returns the TreeViewItem of a data bound object.
        /// </summary>
        /// <param name="treeView">TreeView</param>
        /// <param name="obj">Data bound object</param>
        /// <returns>The TreeViewItem of the data bound object or null.</returns>
        public static TreeViewItem GetItemFromSelectedObject(this TreeView treeView, object obj)
        {
            try
            {
                DependencyObject dObject = GetContainerFormObject(treeView, obj);
                TreeViewItem tvi = dObject as TreeViewItem;
                while (tvi == null)
                {
                    dObject = VisualTreeHelper.GetParent(dObject);
                    tvi = dObject as TreeViewItem;
                }
                return tvi;
            }
            catch { }
            return null;
        }

        private static DependencyObject
        GetContainerFormObject(ItemsControl item, object obj)
        {
            DependencyObject dObject = null;
            dObject = item.ItemContainerGenerator.ContainerFromItem(obj);
            if (dObject == null)
            {
                if (item.Items.Count > 0)
                {
                    foreach (object childItem in item.Items)
                    {
                        ItemsControl childControl = item.ItemContainerGenerator.
                              ContainerFromItem(childItem) as ItemsControl;
                        if (childControl != null) // it is null if not visible, e.g. the items under a collapsed treeview node
                        {
                            dObject = GetContainerFormObject(childControl, obj);
                            if (dObject != null)
                            {
                                TreeViewItem ti = dObject as TreeViewItem;
                                if(ti!=null && (ti.IsFocused || ti.IsSelected))
                                    break;
                            }
                        }
                    }
                }
            }
            return dObject;
        }
    }
}
