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
    using System.Collections;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics;
    using System.IO;
    using System.IO.MemoryMappedFiles;
    using System.Text;
    using Microsoft.VisualStudio.Text.Differencing;
    using Microsoft.VisualStudio.Text.Document;
    using Microsoft.VisualStudio.Text.Projection;
    using Microsoft.VisualStudio.Text.Projection.Implementation;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Factory for TextBuffers and ProjectionBuffers.
    /// </summary>
    [Export(typeof(ITextImageFactoryService))]
    [Export(typeof(ITextImageFactoryService2))]
    [Export(typeof(ITextBufferFactoryService))]
    [Export(typeof(ITextBufferFactoryService2))]
    [Export(typeof(ITextBufferFactoryService3))]
    [Export(typeof(IProjectionBufferFactoryService))]
    [Shared]
    public partial class BufferFactoryService : ITextBufferFactoryService2, ITextBufferFactoryService3, IProjectionBufferFactoryService, IInternalTextBufferFactory, ITextImageFactoryService2
    {
        #region Standard Content Type Definitions
        [Export]
        [Name("any")]
        public ContentTypeDefinition anyContentTypeDefinition { get; set; }

        [Export]
        [Name("text")]
        [BaseDefinition("any")]
        public ContentTypeDefinition textContentTypeDefinition { get; set; }

        [Export]
        [Name("projection")]
        [BaseDefinition("any")]
        public ContentTypeDefinition projectionContentTypeDefinition { get; set; }

        [Export]
        [Name("plaintext")]
        [BaseDefinition("text")]
        public ContentTypeDefinition plaintextContentTypeDefinition { get; set; }

        [Export]
        [Name("code")]
        [BaseDefinition("text")]
        public ContentTypeDefinition codeContentType { get; set; }

        [Export]
        [Name("inert")]
        // N.B.: This ContentType does NOT inherit from anything
        public ContentTypeDefinition inertContentTypeDefinition { get; set; }
        #endregion

        #region Service Consumptions

        [Import]
        public IContentTypeRegistryService _contentTypeRegistryService { get; set; }

        [Import]
        public IDifferenceService _differenceService { get; set; }

        [Import]
        public ITextDifferencingSelectorService _textDifferencingSelectorService { get; set; }

        [Import]
        public GuardedOperations _guardedOperations { get; set; }

        [Import]
        public IWhitespaceManagerFactory _whitespaceManagerFactory { get; set; }

        #endregion

        #region Private state
        private IContentType textContentType;
        private IContentType plaintextContentType;
        private IContentType inertContentType;
        private IContentType projectionContentType;
        #endregion

        #region ContentType accessors
        public IContentType TextContentType
        {
            get
            {
                if (this.textContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.textContentType = _contentTypeRegistryService.GetContentType("text");
                }
                return this.textContentType;
            }
        }

        public IContentType PlaintextContentType
        {
            get
            {
                if (this.plaintextContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.plaintextContentType = _contentTypeRegistryService.GetContentType("plaintext");
                }
                return this.plaintextContentType;
            }
        }

        public IContentType InertContentType
        {
            get
            {
                if (this.inertContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.inertContentType = _contentTypeRegistryService.GetContentType("inert");
                }
                return this.inertContentType;
            }
        }

        public IContentType ProjectionContentType
        {
            get
            {
                if (this.projectionContentType == null)
                {
                    // it's OK to evaluate this more than once, and the assignment is atomic, so we don't protect this with a lock
                    this.projectionContentType = _contentTypeRegistryService.GetContentType("projection");
                }
                return this.projectionContentType;
            }
        }
        #endregion

        public ITextBuffer CreateTextBuffer()
        {
            return Make(TextContentType, StringRebuilder.Empty, false);
        }

        public ITextBuffer CreateTextBuffer(IContentType contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
            return Make(contentType, StringRebuilder.Empty, false);
        }

        public ITextBuffer CreateTextBuffer(string text, IContentType contentType)
        {
            return CreateTextBuffer(text, contentType, false);
        }

        public ITextBuffer CreateTextBuffer(SnapshotSpan span, IContentType contentType)
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            StringRebuilder content = StringRebuilderFromSnapshotSpan(span);

            return Make(contentType, content, false);
        }

        public ITextBuffer CreateTextBuffer(ITextImage image, IContentType contentType)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            StringRebuilder content = StringRebuilder.Create(image);

            return Make(contentType, content, false);
        }

        public ITextBuffer CreateTextBuffer(string text, IContentType contentType, bool spurnGroup)
        {
            if (text == null)
            {
                throw new ArgumentNullException(nameof(text));
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
            return Make(contentType, StringRebuilder.Create(text), spurnGroup);
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType, long length, string traceId)
        {
            return this.CreateTextBuffer(reader, contentType, length, traceId, throwOnInvalidCharacters: false);
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType, long length, string traceId, bool throwOnInvalidCharacters)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
            if (length > int.MaxValue)
            {
                throw new InvalidOperationException(Strings.FileTooLarge);
            }

            int longestLineLength;
            StringRebuilder content = TextImageLoader.Load(
                reader,
                length,
                out var newlineState,
                out var leadingWhitespaceState,
                out longestLineLength,
                throwOnInvalidCharacters: throwOnInvalidCharacters);

            ITextBuffer buffer = Make(contentType, content, false);

            // Make the call to GetWhitespaceManager to add the manager to the properties. We don't need the return value here.
            var _ = _whitespaceManagerFactory.GetOrCreateWhitespaceManager(buffer, newlineState, leadingWhitespaceState);

            // Leave a sign about the longest line in the buffer. This is rather nasty, but for now
            // we don't want to pollute the API with this factoid
            buffer.Properties["LongestLineLength"] = longestLineLength;

            return buffer;
        }

        public ITextBuffer CreateTextBuffer(TextReader reader, IContentType contentType)
        {
            return CreateTextBuffer(reader, contentType, -1, "legacy", throwOnInvalidCharacters: false);
        }

        internal static StringRebuilder StringRebuilderFromSnapshotAndSpan(ITextSnapshot snapshot, Span span)
        {
            return AppendStringRebuildersFromSnapshotAndSpan(StringRebuilder.Empty, snapshot, span);
        }

        internal static StringRebuilder StringRebuilderFromSnapshotSpan(SnapshotSpan span)
        {
            return StringRebuilderFromSnapshotAndSpan(span.Snapshot, span.Span);
        }

        internal static StringRebuilder StringRebuilderFromSnapshotSpans(IList<SnapshotSpan> sourceSpans, Span selectedSourceSpans)
        {
            StringRebuilder content = StringRebuilder.Empty;
            for (int i = 0; (i < selectedSourceSpans.Length); ++i)
            {
                var span = sourceSpans[selectedSourceSpans.Start + i];
                content = AppendStringRebuildersFromSnapshotAndSpan(content, span.Snapshot, span.Span);
            }

            return content;
        }

        internal static StringRebuilder AppendStringRebuildersFromSnapshotAndSpan(StringRebuilder content, ITextSnapshot snapshot, Span span)
        {
            if (span.Length != 0)
            {
                var baseSnapshot = snapshot as BaseSnapshot;
                if (baseSnapshot != null)
                {
                    content = content.Append(baseSnapshot.Content.GetSubText(span));
                }
                else
                {
                    // The we don't know what to do fallback. This should never be called unless someone provides a new snapshot
                    // implementation.
                    content = content.Append(snapshot.GetText(span));
                }
            }

            return content;
        }

        #region ITextImageFactoryService members
        public ITextImage CreateTextImage(string text)
        {
            return CachingTextImage.Create(StringRebuilder.Create(text), null);
        }

        public ITextImage CreateTextImage(TextReader reader, long length)
        {
            return CachingTextImage.Create(TextImageLoader.Load(reader, length, out var _, out var _, out var _), null);
        }

        public ITextImage CreateTextImage(MemoryMappedFile source)
        {
            // Evil implementation (for now) that just reads the entire contents of the MMF.
            // Eventually to be replaced with something along the lines of a version of the StringRebuilderForCompressedChars that uses the MMF directly.
            using (var stream = source.CreateViewStream())
            {
                using (var reader = new StreamReader(stream, Encoding.Unicode))
                {
                    return this.CreateTextImage(reader, -1);
                }
            }
        }
        #endregion

        private TextBuffer Make(IContentType contentType, StringRebuilder content, bool spurnGroup)
        {
            TextBuffer buffer = new TextBuffer(contentType, content, _textDifferencingSelectorService.DefaultTextDifferencingService, _guardedOperations, spurnGroup);
            RaiseTextBufferCreatedEvent(buffer);
            return buffer;
        }

        public IProjectionBuffer CreateProjectionBuffer(IProjectionEditResolver projectionEditResolver,
                                                        IList<object> trackingSpans,
                                                        ProjectionBufferOptions options,
                                                        IContentType contentType)
        {
            // projectionEditResolver is allowed to be null.
            if (trackingSpans == null)
            {
                throw new ArgumentNullException(nameof(trackingSpans));
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }
            IProjectionBuffer buffer =
                new ProjectionBuffer(this, projectionEditResolver, contentType, trackingSpans, _differenceService, _textDifferencingSelectorService.DefaultTextDifferencingService, options, _guardedOperations);
            RaiseProjectionBufferCreatedEvent(buffer);
            return buffer;
        }

        public IProjectionBuffer CreateProjectionBuffer(IProjectionEditResolver projectionEditResolver,
                                                        IList<object> trackingSpans,
                                                        ProjectionBufferOptions options)
        {
            // projectionEditResolver is allowed to be null.
            if (trackingSpans == null)
            {
                throw new ArgumentNullException(nameof(trackingSpans));
            }

            IProjectionBuffer buffer =
                new ProjectionBuffer(this, projectionEditResolver, ProjectionContentType, trackingSpans, _differenceService, _textDifferencingSelectorService.DefaultTextDifferencingService, options, _guardedOperations);
            RaiseProjectionBufferCreatedEvent(buffer);
            return buffer;
        }

        public IElisionBuffer CreateElisionBuffer(IProjectionEditResolver projectionEditResolver,
                                                  NormalizedSnapshotSpanCollection exposedSpans,
                                                  ElisionBufferOptions options,
                                                  IContentType contentType)
        {
            // projectionEditResolver is allowed to be null.
            if (exposedSpans == null)
            {
                throw new ArgumentNullException(nameof(exposedSpans));
            }
            if (exposedSpans.Count == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exposedSpans));  // really?
            }
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            if (exposedSpans[0].Snapshot != exposedSpans[0].Snapshot.TextBuffer.CurrentSnapshot)
            {
                // TODO:
                // build against given snapshot and then move forward if necessary?
                throw new ArgumentException("Elision buffer must be created against the current snapshot of its source buffer");
            }

            IElisionBuffer buffer = new ElisionBuffer(projectionEditResolver, contentType, exposedSpans[0].Snapshot.TextBuffer,
                                                      exposedSpans, options, _textDifferencingSelectorService.DefaultTextDifferencingService, _guardedOperations);
            RaiseProjectionBufferCreatedEvent(buffer);
            return buffer;
        }

        public IElisionBuffer CreateElisionBuffer(IProjectionEditResolver projectionEditResolver,
                                                  NormalizedSnapshotSpanCollection exposedSpans,
                                                  ElisionBufferOptions options)
        {
            return CreateElisionBuffer(projectionEditResolver, exposedSpans, options, ProjectionContentType);
        }

        public event EventHandler<TextBufferCreatedEventArgs> TextBufferCreated;
        public event EventHandler<TextBufferCreatedEventArgs> ProjectionBufferCreated;

        private void RaiseTextBufferCreatedEvent(ITextBuffer buffer)
        {
            EventHandler<TextBufferCreatedEventArgs> handler = TextBufferCreated;
            if (handler != null)
            {
                handler(this, new TextBufferCreatedEventArgs(buffer));
            }
        }

        private void RaiseProjectionBufferCreatedEvent(IProjectionBufferBase buffer)
        {
            EventHandler<TextBufferCreatedEventArgs> handler = ProjectionBufferCreated;
            if (handler != null)
            {
                handler(this, new TextBufferCreatedEventArgs(buffer));
            }
        }
    }
}
