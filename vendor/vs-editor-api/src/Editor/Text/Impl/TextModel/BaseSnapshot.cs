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
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Base class for all varieties of Text Snapshots.
    /// </summary>
    internal abstract class BaseSnapshot : ITextSnapshot, ITextSnapshot2
    {
        #region State and Construction
        protected readonly ITextVersion2 version;
        private readonly IContentType contentType;

        internal readonly StringRebuilder Content;
        internal readonly ITextImage cachingContent;

        protected BaseSnapshot(ITextVersion2 version, StringRebuilder content)
        {
            this.version = version;
            this.Content = content;
            this.cachingContent = CachingTextImage.Create(this.Content, version.ImageVersion);

            // we must extract the content type here, because the content type of the text buffer may change later.
            this.contentType = version.TextBuffer.ContentType;
        }
        #endregion

        #region ITextSnapshot implementations

        public ITextBuffer TextBuffer 
        {
            get { return this.TextBufferHelper; }
        }

        public IContentType ContentType
        {
            get { return this.contentType; }
        }

        public ITextVersion Version
        {
            get { return this.version; }
        }

        public string GetText(int startIndex, int length)
        {
            return GetText(new Span(startIndex, length));
        }

        public string GetText()
        {
            return GetText(new Span(0, this.Length));
        }

        #region Point and Span factories
        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            return this.version.CreateTrackingPoint(position, trackingMode);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return this.version.CreateTrackingPoint(position, trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            return this.version.CreateTrackingSpan(start, length, trackingMode);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return this.version.CreateTrackingSpan(start, length, trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            return this.version.CreateTrackingSpan(span, trackingMode, TrackingFidelityMode.Forward);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return this.version.CreateTrackingSpan(span, trackingMode, trackingFidelity);
        }
        #endregion
        #endregion

        #region ITextSnapshot2 implementations
        public void SaveToFile(string filePath, bool replaceFile, Encoding encoding)
        {
            FileUtilities.SaveSnapshot(this, replaceFile ? FileMode.Create : FileMode.CreateNew, encoding, filePath);
        }
        #endregion

        #region ITextSnapshot abstract methods
        protected abstract ITextBuffer TextBufferHelper { get; }

        public int Length
        {
            get { return this.cachingContent.Length; }
        }

        public int LineCount
        {
            get { return this.cachingContent.LineCount; }
        }

        public string GetText(Span span)
        {
            return this.cachingContent.GetText(span);
        }

        public void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            this.cachingContent.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        public char[] ToCharArray(int startIndex, int length)
        {
            return this.cachingContent.ToCharArray(startIndex, length);
        }

        public char this[int position]
        {
            get
            {
                return this.cachingContent[position];
            }
        }

        public ITextSnapshotLine GetLineFromLineNumber(int lineNumber)
        {
            TextImageLine lineSpan = this.cachingContent.GetLineFromLineNumber(lineNumber);

            return new TextSnapshotLine(this, lineSpan);
        }

        public ITextSnapshotLine GetLineFromPosition(int position)
        {
            int lineNumber = this.cachingContent.GetLineNumberFromPosition(position);
            return this.GetLineFromLineNumber(lineNumber);
        }

        public int GetLineNumberFromPosition(int position)
        {
            return this.cachingContent.GetLineNumberFromPosition(position);
        }

        public IEnumerable<ITextSnapshotLine> Lines
        {
            get
            {
                // this is a naive implementation
                int lineCount = this.cachingContent.LineCount;
                for (int line = 0; line < lineCount; ++line)
                {
                    yield return GetLineFromLineNumber(line);
                }
            }
        }

        public void Write(System.IO.TextWriter writer)
        {
            this.cachingContent.Write(writer, new Span(0, this.cachingContent.Length));
        }

        public void Write(System.IO.TextWriter writer, Span span)
        {
            this.cachingContent.Write(writer, span);
        }
        #endregion

        public ITextImage TextImage => this.cachingContent;

        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "version: {0} lines: {1} length: {2} \r\n content: {3}",
                Version.VersionNumber, LineCount, Length,
                Utilities.TextUtilities.Escape(this.GetText(0, Math.Min(40, this.Length))));
        }

#if _DEBUG
        internal string DebugOnly_AllText
        {
            get
            {
                return this.GetText(0, Math.Min(this.Length, 1024*1024));
            }
        }
#endif
    }
}
