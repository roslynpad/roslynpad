using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Implementation
{
    /// <summary>
    /// Lightweight stack-like view over a readonly ordered list of imports.
    /// </summary>
    internal class ImportBucket<T, TMetadata>
        where T : class
        where TMetadata : IContentTypeMetadata
    {
        private int _currentImportSetting;
        private readonly IReadOnlyList<Lazy<T, TMetadata>> _imports;

        public ImportBucket(IReadOnlyList<Lazy<T, TMetadata>> imports)
        {
            _imports = imports ?? throw new ArgumentNullException(nameof(imports));
        }

        public bool IsEmpty => _currentImportSetting >= _imports.Count;

        public Lazy<T, TMetadata> Peek()
        {
            if (!IsEmpty)
            {
                return _imports[_currentImportSetting];
            }

            throw new InvalidOperationException($"{nameof(ImportBucket<T, TMetadata>)} is empty.");
        }

        internal Lazy<T, TMetadata> Pop()
        {
            if (!IsEmpty)
            {
                return _imports[_currentImportSetting++];
            }

            throw new InvalidOperationException($"{nameof(ImportBucket<T, TMetadata>)} is empty.");
        }
    }
}
