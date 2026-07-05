//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Text.Implementation
{
    internal class PageManager
    {
        // .Item1 == topemost item in the real MRU (.Item2). It is called out as a special case because updating the
        // MRU to put the topmost item at the top of the MRU is a very hot path & has showed up in perf traces.
        private Tuple<Page, List<Tuple<Page, char[]>>> _mru;
        private readonly int _maxPages;

        public PageManager()
        {
            _maxPages = TextModelOptions.CompressedStorageMaxLoadedPages;
            _mru = Tuple.Create((Page)null, new List<Tuple<Page, char[]>>(_maxPages));
        }

        public void UpdateMRU(Page page, char[] contents)
        {
            var oldMRU = Volatile.Read(ref _mru);
            while (true)
            {
                if (oldMRU.Item1 == page)
                {
                    // This is the very hot path so return immediately if the new page is already topmost.
                    return;
                }

                int index = oldMRU.Item2.Count - 1; // Intentionally skip checking the topmost item (we know, due to the check above, that it isn't page).
                while (--index >= 0)
                {
                    if (oldMRU.Item2[index].Item1 == page)
                    {
                        break;
                    }
                }

                var newMRUList = new List<Tuple<Page, char[]>>(_maxPages);
                newMRUList.AddRange(oldMRU.Item2);
                if (index >= 0)
                    newMRUList.RemoveAt(index);
                else if (newMRUList.Count >= _maxPages)
                    newMRUList.RemoveAt(0);

                newMRUList.Add(Tuple.Create(page, contents));

                var newMRU = Tuple.Create(page, newMRUList);

                var result = Interlocked.CompareExchange(ref _mru, newMRU, oldMRU);
                if (result == oldMRU)
                    return;

                oldMRU = result;
            }
        }
    }
}
