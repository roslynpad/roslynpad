//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Helper class that improves performances of ITextImage by caching last leaf accessed for certain operations.
    /// </summary>
    internal class CachingTextImage : ITextImage
    {
        public readonly StringRebuilder Builder;
        private Tuple<int, StringRebuilder> _cache;

        public static ITextImage Create(StringRebuilder builder, ITextImageVersion version)
        {
            return new CachingTextImage(builder, version);
        }

        private CachingTextImage(StringRebuilder builder, ITextImageVersion version)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            this.Builder = builder;
            this.Version = version;
        }

        public ITextImageVersion Version { get; }

        public ITextImage GetSubText(Span span) { return CachingTextImage.Create(this.Builder.GetSubText(span), version: null); }

        public int Length => this.Builder.Length;
        public int LineCount => (this.Builder.LineBreakCount + 1);

        public string GetText(Span span)
        {
            Tuple<int, StringRebuilder> cache = this.UpdateCache(span.Start);

            int offsetStart = span.Start - cache.Item1;
            if (offsetStart + span.Length < cache.Item2.Length)
            {
                return cache.Item2.GetText(new Span(offsetStart, span.Length));
            }

            return this.Builder.GetText(span);
        }

        public char[] ToCharArray(int startIndex, int length) { return this.Builder.ToCharArray(startIndex, length); }
        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count) { this.Builder.CopyTo(sourceIndex, destination, destinationIndex, count); }

        public char this[int position]
        {
            get
            {
                Tuple<int, StringRebuilder> cache = this.UpdateCache(position);

                return cache.Item2[position - cache.Item1];
            }
        }

        public TextImageLine GetLineFromLineNumber(int lineNumber)
        {
            Span extent;
            int lineBreakLength;

            this.Builder.GetLineFromLineNumber(lineNumber, out extent, out lineBreakLength);
            return new TextImageLine(this, lineNumber, extent, lineBreakLength);
        }

        public TextImageLine GetLineFromPosition(int position)
        {
            return this.GetLineFromLineNumber(this.Builder.GetLineNumberFromPosition(position));
        }

        public int GetLineNumberFromPosition(int position) { return this.Builder.GetLineNumberFromPosition(position); }
        public void Write(System.IO.TextWriter writer, Span span) { this.Builder.Write(writer, span); }

        private Tuple<int, StringRebuilder> UpdateCache(int position)
        {
            Tuple<int, StringRebuilder> cache = _cache;
            if ((cache == null) || (position < cache.Item1) || (position >= (cache.Item1 + cache.Item2.Length)))
            {
                int offset;
                StringRebuilder leaf = this.Builder.GetLeaf(position, out offset);

                cache = new Tuple<int, StringRebuilder>(offset, leaf);

                //Since cache is a class, cachedLeaf should update atomically.
                _cache = cache;
            }

            return cache;
        }
    }
}
