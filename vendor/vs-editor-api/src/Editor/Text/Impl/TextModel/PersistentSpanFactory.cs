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
    using System.Collections.Generic;
    using System.Composition;

    [Export(typeof(IPersistentSpanFactory))]
    [Shared]
    public class PersistentSpanFactory : IPersistentSpanFactory
    {
        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        private readonly Dictionary<object, PersistentSpanSet> _spansOnDocuments = new Dictionary<object, PersistentSpanSet>();   //Used for lock
        private bool _eventsHooked;

        #region IPersistentSpanFactory members
        public bool CanCreate(ITextBuffer buffer)
        {
            Requires.NotNull(buffer, nameof(buffer));

            ITextDocument document;
            return this.TextDocumentFactoryService.TryGetTextDocument(buffer, out document);
        }

        public IPersistentSpan Create(SnapshotSpan span, SpanTrackingMode trackingMode)
        {
            Requires.NotNull(span.Snapshot, nameof(span.Snapshot));

            ITextDocument document;
            if (this.TextDocumentFactoryService.TryGetTextDocument(span.Snapshot.TextBuffer, out document))
            {
                lock (_spansOnDocuments)
                {
                    var spanSet = this.GetOrCreateSpanSet(null, document);
                    return spanSet.Create(span, trackingMode);
                }
            }

            return null;
        }

        public IPersistentSpan Create(ITextSnapshot snapshot, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode)
        {
            Requires.NotNull(snapshot, nameof(snapshot));
            Requires.Argument(startLine >= 0, nameof(startLine), "Must be non-negative.");
            Requires.Argument(startIndex >= 0, nameof(startIndex), "Must be non-negative.");
            Requires.Argument(endLine >= startLine, nameof(endLine), "Must be >= startLine.");
            Requires.Argument((endIndex >= 0) && ((startLine != endLine) || (endIndex >= startIndex)), nameof(endIndex), "Must be non-negative and (endLine,endIndex) may not be before (startLine,startIndex).");
            Requires.Range(((int)trackingMode >= (int)SpanTrackingMode.EdgeExclusive) || ((int)trackingMode <= (int)(SpanTrackingMode.EdgeNegative)), nameof(trackingMode));

            ITextDocument document;
            if (this.TextDocumentFactoryService.TryGetTextDocument(snapshot.TextBuffer, out document))
            {
                lock (_spansOnDocuments)
                {
                    var spanSet = this.GetOrCreateSpanSet(null, document);
                    return spanSet.Create(snapshot, startLine, startIndex, endLine, endIndex, trackingMode);
                }
            }

            return null;
        }

        public IPersistentSpan Create(string filePath, int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode)
        {
            Requires.NotNullOrEmpty(filePath, nameof(filePath));
            Requires.Argument(startLine >= 0, nameof(startLine), "Must be non-negative.");
            Requires.Argument(startIndex >= 0, nameof(startIndex), "Must be non-negative.");
            Requires.Argument(endLine >= startLine, nameof(endLine), "Must be >= startLine.");
            Requires.Argument((endIndex >= 0) && ((startLine != endLine) || (endIndex >= startIndex)), nameof(endIndex), "Must be non-negative and (endLine,endIndex) may not be before (startLine,startIndex).");
            Requires.Range(((int)trackingMode >= (int)SpanTrackingMode.EdgeExclusive) || ((int)trackingMode <= (int)(SpanTrackingMode.EdgeNegative)), nameof(trackingMode));

            var key = new FileNameKey(filePath);
            lock (_spansOnDocuments)
            {
                var spanSet = this.GetOrCreateSpanSet(key, null);
                return spanSet.Create(startLine, startIndex, endLine, endIndex, trackingMode);
            }
        }

        public IPersistentSpan Create(string filePath, Span span, SpanTrackingMode trackingMode)
        {
            Requires.NotNullOrEmpty(filePath, nameof(filePath));
            Requires.Range(((int)trackingMode >= (int)SpanTrackingMode.EdgeExclusive) || ((int)trackingMode <= (int)(SpanTrackingMode.EdgeNegative)), nameof(trackingMode));

            var key = new FileNameKey(filePath);
            lock (_spansOnDocuments)
            {
                var spanSet = this.GetOrCreateSpanSet(key, null);
                return spanSet.Create(span, trackingMode);
            }
        }
        #endregion

        internal bool IsEmpty { get { return _spansOnDocuments.Count == 0; } } //For unit tests

        private PersistentSpanSet GetOrCreateSpanSet(FileNameKey filePath, ITextDocument document)
        {
            object key = ((object)document) ?? filePath;
            if (!_spansOnDocuments.TryGetValue(key, out PersistentSpanSet spanSet))
            {
                if (!_eventsHooked)
                {
                    _eventsHooked = true;

                    this.TextDocumentFactoryService.TextDocumentCreated += OnTextDocumentCreated;
                    this.TextDocumentFactoryService.TextDocumentDisposed += OnTextDocumentDisposed;
                }

                spanSet = new PersistentSpanSet(filePath, document, this);
                _spansOnDocuments.Add(key, spanSet);
            }

            return spanSet;
        }

        private void OnTextDocumentCreated(object sender, TextDocumentEventArgs e)
        {
            var path = new FileNameKey(e.TextDocument.FilePath);
            lock (_spansOnDocuments)
            {
                if (_spansOnDocuments.TryGetValue(path, out PersistentSpanSet spanSet))
                {
                    spanSet.DocumentReopened(e.TextDocument);

                    _spansOnDocuments.Remove(path);
                    _spansOnDocuments.Add(e.TextDocument, spanSet);
                }
            }
        }

        private void OnTextDocumentDisposed(object sender, TextDocumentEventArgs e)
        {
            lock (_spansOnDocuments)
            {
                if (_spansOnDocuments.TryGetValue(e.TextDocument, out PersistentSpanSet spanSet))
                {
                    spanSet.DocumentClosed();
                    _spansOnDocuments.Remove(e.TextDocument);

                    if (_spansOnDocuments.TryGetValue(spanSet.FileKey, out PersistentSpanSet existingSpansOnPath))
                    {
                        // Handle (badly) the case where a document is renamed to an existing closed document & then closed.
                        // We should only end up in this case if we had spans on two open documents that were both renamed
                        // to the same file name & then closed.
                        foreach (var s in spanSet.Spans)
                        {
                            s.SetSpanSet(existingSpansOnPath);
                            existingSpansOnPath.Spans.Add(s);
                        }

                        spanSet.Spans.Clear();
                        spanSet.Dispose();
                    }
                    else
                    {
                        _spansOnDocuments.Add(spanSet.FileKey, spanSet);
                    }
                }
            }
        }

        internal void DocumentRenamed(PersistentSpanSet spanSet)
        {
            lock (_spansOnDocuments)
            {
                if (_spansOnDocuments.TryGetValue(spanSet.FileKey, out PersistentSpanSet existingSpansOnPath))
                {
                    // There were spans on a closed document with the same name as this one. Move all of those spans to this one
                    // and "open" them (note that this will probably do bad things to their positions but it is the best we
                    // can do).
                    foreach (var s in existingSpansOnPath.Spans)
                    {
                        s.SetSpanSet(spanSet);
                        spanSet.Spans.Add(s);

                        s.DocumentReopened();
                    }

                    existingSpansOnPath.Spans.Clear();
                    existingSpansOnPath.Dispose();
                }
            }
        }
        internal void Delete(PersistentSpanSet spanSet, PersistentSpan span)
        {
            lock (_spansOnDocuments)
            {
                if (spanSet.Spans.Remove(span) && (spanSet.Spans.Count == 0))
                {
                    _spansOnDocuments.Remove(((object)(spanSet.Document)) ?? spanSet.FileKey);
                    spanSet.Dispose();
                }
            }
        }
    }
}
