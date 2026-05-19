using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Framework.WPF.Library
{
    public class SortableObservableCollection<T, TKey> : ObservableCollection<T>
    {
        private Func<T, IComparable>[] keySelectors;

        public SortableObservableCollection(params Func<T, IComparable>[] keySelectors)
            : base()
        {
            this.keySelectors = keySelectors;
        }

        public SortableObservableCollection(List<T> list, params Func<T, IComparable>[] keySelectors)
            : base(list)
        {
            this.keySelectors = keySelectors;
        }

        public SortableObservableCollection(IEnumerable<T> collection, params Func<T, IComparable>[] keySelectors)
            : base(collection)
        {
            this.keySelectors = keySelectors;
        }

        public void Sort(ListSortDirection sortOrder = ListSortDirection.Ascending, IComparer<T> comparer = null, params Func<T, IComparable>[] keySelectors)
        {
            if (keySelectors.Count() == 0)
            {
                keySelectors = this.keySelectors;
            }
            switch (sortOrder)
            {
                case ListSortDirection.Ascending:
                    {
                        if(comparer == null)
                        {
                            for (int i = keySelectors.Count() - 1; i >= 0; i--)
                                ApplySort(Items.OrderBy(keySelectors[i]));
                        }
                        else
                        {
                            ApplySort(Items.OrderBy(x => x, comparer));
                        }

                        break;
                    }
                case ListSortDirection.Descending:
                    {
                        if (comparer == null)
                        {
                            for (int i = keySelectors.Count() - 1; i >= 0; i--)
                                ApplySort(Items.OrderByDescending(keySelectors[i]));
                        }
                        else
                        {
                            ApplySort(Items.OrderByDescending(x => x, comparer));
                        }
                        break;
                    }
            }
        }

        private void ApplySort(IEnumerable<T> sortedItems)
        {
            var sortedList = sortedItems.ToList();
            // Replace backing list in one shot and fire a single Reset — O(n log n) vs O(n²) Move/IndexOf loop
            Items.Clear();
            foreach (var item in sortedList)
                Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
