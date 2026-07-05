//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Diagnostics;
namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// Base class for a page that participates in an MRU list.
    /// </summary>
    internal class Page
    {
        private WeakReference<char[]> _uncompressedContents;
        private byte[] _compressedContents;

        public readonly int Length;
        public readonly PageManager Manager;

        public Page(PageManager manager, char[] contents, int length)
        {
            this.Manager = manager;
            this.Length = length;

            _uncompressedContents = new WeakReference<char[]>(contents);
            _compressedContents = Compressor.Compress(contents, length);
        }

        public char[] Expand()
        {
            char[] contents;
            if (!_uncompressedContents.TryGetTarget(out contents))
            {
                contents = new char[this.Length];
                Compressor.Decompress(_compressedContents, this.Length, contents);

                _uncompressedContents.SetTarget(contents);
            }

            this.Manager.UpdateMRU(this, contents);
            return contents;
        }
    }
}
