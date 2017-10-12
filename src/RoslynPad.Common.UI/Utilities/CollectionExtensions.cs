using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RoslynPad.UI.Utilities
{
    public static class ObservableCollection
    {
        public static void Sort<T>(this ObservableCollection<T> collection, Func<IEnumerable<T>, IOrderedEnumerable<T>> orderFunc)
        {
            var sortableList = orderFunc(collection).ToList();
            for (int i = 0; i < sortableList.Count; i++)
            {
                collection.Move(collection.IndexOf(sortableList[i]), i);
            }
        }
    }
}
