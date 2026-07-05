using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.Text.Utilities;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Custom comparer for sorting lists of content types that preserves original order of unrelated content types.
    /// </summary>
    /// <remarks>Note that this class (as a typical comparer) is intended to be used as a long lived instance.
    /// It's expensive to create, but is immutable and very efficient when used.</remarks>
    internal class StableContentTypeComparer : IComparer<IEnumerable<string>>
    {
        private readonly Dictionary<string, int> _contentTypeRanks;

        /// <summary>
        /// Build a forest of all content types and associate a rank with each content type,
        /// indicating number of ancestors in the forest. E.g. any gets rank 0, text - 1,
        /// projection - 1, code - 2, inert - 0.
        /// </summary>
        public StableContentTypeComparer(IContentTypeRegistryService contentTypeRegistryService)
        {
            _contentTypeRanks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            contentTypeRegistryService = contentTypeRegistryService ?? throw new ArgumentNullException(nameof(contentTypeRegistryService));

            var unprocessedBaseTypes = new Dictionary<IContentType, int>();
            var derivedTypes = new Dictionary<IContentType, FrugalList<IContentType>>();

            foreach (IContentType curContentType in contentTypeRegistryService.ContentTypes)
            {
                unprocessedBaseTypes[curContentType] = curContentType.BaseTypes.Count();
                derivedTypes[curContentType] = new FrugalList<IContentType>();
            }

            foreach (IContentType curContentType in contentTypeRegistryService.ContentTypes)
            {
                foreach (IContentType baseContentType in curContentType.BaseTypes)
                {
                    FrugalList<IContentType> baseDerivedContentTypes = derivedTypes[baseContentType];
                    baseDerivedContentTypes.Add(curContentType);
                }
            }

            int groupRank = 0;
            while (unprocessedBaseTypes.Count > 0)
            {
                IEnumerable<IContentType> contentTypesWithoutBase = unprocessedBaseTypes.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).ToList();
                foreach (IContentType curContentTypeWithoutBase in contentTypesWithoutBase)
                {
                    _contentTypeRanks[curContentTypeWithoutBase.TypeName] = groupRank;

                    unprocessedBaseTypes.Remove(curContentTypeWithoutBase);
                    foreach (IContentType curDerivedType in derivedTypes[curContentTypeWithoutBase])
                    {
                        unprocessedBaseTypes[curDerivedType] = unprocessedBaseTypes[curDerivedType] - 1;
                    }
                }

                groupRank++;
            }
        }

        public int Compare(IEnumerable<string> x, IEnumerable<string> y)
        {
            if (x == null && y == null)
            {
                return 0;
            }

            if (x == null)
            {
                return -1;
            }

            if (y == null)
            {
                return 1;
            }

            int xRank = x.Select(ct => GetRank(ct)).Max();
            int yRank = y.Select(ct => GetRank(ct)).Max();

            return yRank.CompareTo(xRank);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int GetRank(string contentType)
        {
            _contentTypeRanks.TryGetValue(contentType, out int result);
            return result;
        }
    }
}

