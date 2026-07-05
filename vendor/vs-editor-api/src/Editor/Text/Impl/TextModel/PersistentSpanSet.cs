//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Implementation
{
    using Microsoft;
    using System;
    using System.Collections.Generic;

    sealed class PersistentSpanSet : IDisposable
    {
        internal FileNameKey FileKey;
        internal ITextDocument Document;
        internal readonly HashSet<PersistentSpan> Spans = new HashSet<PersistentSpan>();
        private readonly PersistentSpanFactory Factory;

        private ITextSnapshot _savedSnapshot = null;

        internal PersistentSpanSet(FileNameKey filePath, ITextDocument document, PersistentSpanFactory factory)
        {
            this.FileKey = filePath;
            this.Document = document;
            this.Factory = factory;

            if (document != null)
            {
                document.FileActionOccurred += this.OnFileActionOccurred;
            }
        }

        public void Dispose()
        {
            Assumes.True(this.Spans.Count == 0);

            if (this.Document != null)
            {
                this.Document.FileActionOccurred -= this.OnFileActionOccurred;
                this.Document = null;
            }
        }

        internal PersistentSpan Create(int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode)
        {
            PersistentSpan persistentSpan = new PersistentSpan(startLine, startIndex, endLine, endIndex, trackingMode, this);
            this.Spans.Add(persistentSpan);
            return persistentSpan;
        }

        internal PersistentSpan Create(Span span, SpanTrackingMode trackingMode)
        {
            var persistentSpan = new PersistentSpan(span, trackingMode, this);
            this.Spans.Add(persistentSpan);
            return persistentSpan;
        }

        internal PersistentSpan Create(ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode)
        {
            var start = PersistentSpan.LineIndexToSnapshotPoint(startLine, startIndex, snapshot);
            var end = PersistentSpan.LineIndexToSnapshotPoint(endLine, endIndex, snapshot);
            if (end < start)
            {
                end = start;
            }

            return this.Create(new SnapshotSpan(start, end), trackingMode);
        }

        internal PersistentSpan Create(SnapshotSpan span, SpanTrackingMode trackingMode)
        {
            var persistentSpan = new PersistentSpan(span, trackingMode, this);
            this.Spans.Add(persistentSpan);
            return persistentSpan;
        }

        internal void Delete(PersistentSpan span)
        {
            this.Factory.Delete(this, span);
        }

        internal void DocumentReopened(ITextDocument document)
        {
            Requires.NotNull(document, nameof(document));
            Assumes.Null(this.Document);

            this.Document = document;
            document.FileActionOccurred += this.OnFileActionOccurred;

            foreach (var s in this.Spans)
            {
                s.DocumentReopened();
            }
        }

        internal void DocumentClosed()
        {
            Assumes.NotNull(this.Document);

            this.FileKey = new FileNameKey(this.Document.FilePath);

            foreach (var s in this.Spans)
            {
                s.DocumentClosed(_savedSnapshot);
            }

            this.Document.FileActionOccurred -= this.OnFileActionOccurred;
            this.Document = null;
        }

        private void OnFileActionOccurred(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                _savedSnapshot = this.Document.TextBuffer.CurrentSnapshot;
            }
            else if (e.FileActionType == FileActionTypes.DocumentRenamed)
            {
                this.FileKey = new FileNameKey(this.Document.FilePath);
                this.Factory.DocumentRenamed(this);
            }
        }
    }
}
