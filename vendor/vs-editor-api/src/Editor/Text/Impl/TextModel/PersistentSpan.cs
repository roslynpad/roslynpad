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

    internal sealed class PersistentSpan : IPersistentSpan
    {
        #region members
        public PersistentSpanSet SpanSet;

        private ITrackingSpan _span;            //null for spans on closed documents or disposed spans
        private int _startLine;                 //these parameters are valid whether or not the document is open (but _start*,_end* may be stale).
        private int _startIndex;
        private int _endLine;
        private int _endIndex;
        private ITextVersion _originalVersion = null;
        private Span _originalSpan;             // This is either the span when this was created or when the document was reopened.
                                                // It is default(Span) if either we were created (on an unopened document) with line/column indices or after the document was closed.
        private bool _useLineIndex;

        private readonly SpanTrackingMode _trackingMode;
        #endregion

        internal PersistentSpan(SnapshotSpan span, SpanTrackingMode trackingMode, PersistentSpanSet spanSet)
        {
            _span = span.Snapshot.CreateTrackingSpan(span, trackingMode);

            _originalVersion = span.Snapshot.Version;
            _originalSpan = span;

            PersistentSpan.SnapshotPointToLineIndex(span.Start, out _startLine, out _startIndex);
            PersistentSpan.SnapshotPointToLineIndex(span.End, out _endLine, out _endIndex);

            _trackingMode = trackingMode;
            this.SpanSet = spanSet;
        }

        internal PersistentSpan(int startLine, int startIndex, int endLine, int endIndex, SpanTrackingMode trackingMode, PersistentSpanSet spanSet)
        {
            _useLineIndex = true;
            _startLine = startLine;
            _startIndex = startIndex;
            _endLine = endLine;
            _endIndex = endIndex;

            _trackingMode = trackingMode;
            this.SpanSet = spanSet;
        }

        internal PersistentSpan(Span span, SpanTrackingMode trackingMode, PersistentSpanSet spanSet)
        {
            _useLineIndex = false;
            _originalSpan = span;

            _trackingMode = trackingMode;
            this.SpanSet = spanSet;
        }

        #region IPersistentSpan members
        public bool IsDocumentOpen { get { return this.SpanSet.Document != null; } }

        public ITextDocument Document { get { return this.SpanSet.Document; } }

        public ITrackingSpan Span { get { return _span; } }

        public string FilePath
        {
            get
            {
                if (this.SpanSet == null)
                    throw new ObjectDisposedException("PersistentSpan");

                return (this.SpanSet.Document != null) ? this.SpanSet.Document.FilePath : this.SpanSet.FileKey.ToString();
            }
        }

        public bool TryGetStartLineIndex(out int startLine, out int startIndex)
        {
            if (this.SpanSet == null)
                throw new ObjectDisposedException("PersistentSpan");

            if (_span != null)
            {
                SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);
                PersistentSpan.SnapshotPointToLineIndex(span.Start, out startLine, out startIndex);
                return true;
            }
            else if (_useLineIndex)
            {
                startLine = _startLine;
                startIndex = _startIndex;
                return true;
            }

            startLine = startIndex = 0;
            return false;
        }

        public bool TryGetEndLineIndex(out int endLine, out int endIndex)
        {
            if (this.SpanSet == null)
                throw new ObjectDisposedException("PersistentSpan");

            if (_span != null)
            {
                SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);
                PersistentSpan.SnapshotPointToLineIndex(span.End, out endLine, out endIndex);
                return true;
            }
            else if (_useLineIndex)
            {
                endLine = _endLine;
                endIndex = _endIndex;
                return true;
            }

            endLine = endIndex = 0;
            return false;
        }

        public bool TryGetSpan(out Span span)
        {
            if (this.SpanSet == null)
                throw new ObjectDisposedException("PersistentSpan");

            if (_span != null)
            {
                span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);
                return true;
            }
            else if (!_useLineIndex)
            {
                span = _originalSpan;
                return true;
            }

            span = new Span();
            return false;
        }
        #endregion

        #region IDisposable members
        public void Dispose()
        {
            if (this.SpanSet != null)
            {
                this.SpanSet.Delete(this);
                this.SpanSet = null;
                _originalVersion = null;
                _span = null;
            }
        }
        #endregion

        internal void SetSpanSet(PersistentSpanSet spanSet)
        {
            if (this.SpanSet == null)
                throw new ObjectDisposedException("PersistentSpan");

            this.SpanSet = spanSet;
        }

        internal void DocumentClosed(ITextSnapshot savedSnapshot)
        {
            Assumes.NotNull(_originalVersion);

            if ((savedSnapshot != null) && (savedSnapshot.Version.VersionNumber > _originalVersion.VersionNumber))
            {
                // The document was saved and we want to line/column indices in the saved snapshot (& not the current snapshot)
                var savedSpan = new SnapshotSpan(savedSnapshot, Tracking.TrackSpanForwardInTime(_trackingMode, _originalSpan, _originalVersion, savedSnapshot.Version));

                PersistentSpan.SnapshotPointToLineIndex(savedSpan.Start, out _startLine, out _startIndex);
                PersistentSpan.SnapshotPointToLineIndex(savedSpan.End, out _endLine, out _endIndex);
            }
            else
            {
                // The document was never saved (or was saved before we created) so continue to use the old line/column indices.
                // Since those are set when either the span is created (against an open document) or when the document is reopened,
                // they don't need to be changed.
            }

            //We set this to false when the document is closed because we have an accurate line/index and that is more stable
            //than a simple offset.
            _useLineIndex = true;
            _originalSpan = default(Span);
            _originalVersion = null;
            _span = null;
        }

        internal void DocumentReopened()
        {
            ITextSnapshot snapshot = this.SpanSet.Document.TextBuffer.CurrentSnapshot;

            SnapshotPoint start;
            SnapshotPoint end;
            if (_useLineIndex)
            {
                start = PersistentSpan.LineIndexToSnapshotPoint(_startLine, _startIndex, snapshot);
                end = PersistentSpan.LineIndexToSnapshotPoint(_endLine, _endIndex, snapshot);

                if (end < start)
                {
                    //Guard against the case where _start & _end are something like (100,2) & (101, 1).
                    //Those points would pass the argument validation (since _endLine > _startLine) but
                    //would cause problems if the document has only 5 lines since they would map to
                    //(5, 2) & (5, 1).
                    end = start;
                }
            }
            else
            {
                start = new SnapshotPoint(snapshot, Math.Min(_originalSpan.Start, snapshot.Length));
                end = new SnapshotPoint(snapshot, Math.Min(_originalSpan.End, snapshot.Length));
            }

            var snapshotSpan = new SnapshotSpan(start, end);
            _span = snapshot.CreateTrackingSpan(snapshotSpan, _trackingMode);
            _originalSpan = snapshotSpan;

            _originalVersion = snapshot.Version;
            PersistentSpan.SnapshotPointToLineIndex(snapshotSpan.Start, out _startLine, out _startIndex);
            PersistentSpan.SnapshotPointToLineIndex(snapshotSpan.End, out _endLine, out _endIndex);
        }

        private SnapshotSpan UpdateStartEnd()
        {
            SnapshotSpan span = _span.GetSpan(_span.TextBuffer.CurrentSnapshot);

            PersistentSpan.SnapshotPointToLineIndex(span.Start, out _startLine, out _startIndex);
            PersistentSpan.SnapshotPointToLineIndex(span.End, out _endLine, out _endIndex);

            return span;
        }

        private static void SnapshotPointToLineIndex(SnapshotPoint p, out int line, out int index)
        {
            ITextSnapshotLine l = p.GetContainingLine();

            line = l.LineNumber;
            index = Math.Min(l.Length, p - l.Start);
        }

        internal static SnapshotPoint LineIndexToSnapshotPoint(int line, int index, ITextSnapshot snapshot)
        {
            if (line >= snapshot.LineCount)
            {
                return new SnapshotPoint(snapshot, snapshot.Length);
            }

            ITextSnapshotLine l = snapshot.GetLineFromLineNumber(line);
            return l.Start + Math.Min(index, l.Length);
        }
    }
}
