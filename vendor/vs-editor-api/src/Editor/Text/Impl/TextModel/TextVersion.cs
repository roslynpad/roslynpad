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
    using System.Globalization;

    /// <summary>
    /// An internal implementation of ITextVersion
    /// </summary>
    internal partial class TextVersion : ITextVersion, ITextVersion2
    {
        private readonly TextImageVersion _textImageVersion;

        /// <summary>
        /// Initializes a new instance of a <see cref="TextVersion"/>.
        /// </summary>
        /// <param name="textBuffer">The <see cref="ITextBuffer"/> to which the version belongs.</param>
        /// <param name="imageVersion">The <see cref="ITextImageVersion"/> of the associated snapshot.</param>
        public TextVersion(ITextBuffer textBuffer, TextImageVersion imageVersion)
        {
            if (textBuffer == null)
            {
                throw new ArgumentNullException(nameof(textBuffer));
            }

            if (imageVersion == null)
            {
                throw new ArgumentNullException(nameof(imageVersion));
            }

            this.TextBuffer = textBuffer;
            _textImageVersion = imageVersion;
        }

        /// <summary>
        /// Create a new version based on applying <paramref name="changes"/> to this.
        /// </summary>
        /// <param name="changes">null if set later</param>
        /// <param name="newLength">use -1 to compute a length</param>
        /// <param name="reiteratedVersionNumber">use -1 to get the default value</param>
        /// <remarks>
        /// <para>If <paramref name="changes"/> can be null, then <paramref name="newLength"/> cannot be -1.</para>
        /// </remarks>
        internal TextVersion CreateNext(INormalizedTextChangeCollection changes, int newLength = -1, int reiteratedVersionNumber = -1)
        {
            if (this.Next != null)
                throw new InvalidOperationException("Not allowed to CreateNext twice");

            var newTextImageVersion = this._textImageVersion.CreateNext(reiteratedVersionNumber: reiteratedVersionNumber, length: newLength, changes: changes);

            var next = new TextVersion(this.TextBuffer, newTextImageVersion);
            this.Next = next;

            return next;
        }

        internal void SetLength(int length)
        {
            _textImageVersion.SetLength(length);
        }

        internal void SetChanges(INormalizedTextChangeCollection changes)
        {
            _textImageVersion.SetChanges(changes);
        }

        public ITextBuffer TextBuffer
        {
            get;
        }

        public int VersionNumber
        {
            get { return this.ImageVersion.VersionNumber; }
        }

        public int ReiteratedVersionNumber
        {
            get { return this.ImageVersion.ReiteratedVersionNumber; }
        }

        /// <summary>
        /// Gets the next version node
        /// </summary>
        public ITextVersion Next
        {
            get; private set;
        }

        /// <summary>
        /// Gets the current change information
        /// </summary>
        public INormalizedTextChangeCollection Changes
        {
            get { return this.ImageVersion.Changes; }
        }

        public int Length
        {
            get { return this.ImageVersion.Length; }
        }

        public ITextImageVersion ImageVersion { get { return _textImageVersion; } }

        #region Point and Span Factories
        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode)
        {
            // Forward fidelity is implicit
            return new ForwardFidelityTrackingPoint(this, position, trackingMode);
        }

        public ITrackingPoint CreateTrackingPoint(int position, PointTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            if (trackingFidelity == TrackingFidelityMode.Forward)
            {
                return new ForwardFidelityTrackingPoint(this, position, trackingMode);
            }
            else
            {
                return new HighFidelityTrackingPoint(this, position, trackingMode, trackingFidelity);
            }
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode)
        {
            // Forward fidelity is implicit
            if (trackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException(nameof(trackingMode));
            }
            return new ForwardFidelityTrackingSpan(this, new Span(start, length), trackingMode);
        }

        public ITrackingSpan CreateTrackingSpan(int start, int length, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            return CreateTrackingSpan(new Span(start, length), trackingMode, trackingFidelity);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode)
        {
            // Forward fidelity is implicit
            if (trackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException(nameof(trackingMode));
            }
            return new ForwardFidelityTrackingSpan(this, span, trackingMode);
        }

        public ITrackingSpan CreateTrackingSpan(Span span, SpanTrackingMode trackingMode, TrackingFidelityMode trackingFidelity)
        {
            if (trackingMode == SpanTrackingMode.Custom)
            {
                throw new ArgumentOutOfRangeException(nameof(trackingMode));
            }
            if (trackingFidelity == TrackingFidelityMode.Forward)
            {
                return new ForwardFidelityTrackingSpan(this, span, trackingMode);
            }
            else 
            {
                return new HighFidelityTrackingSpan(this, span, trackingMode, trackingFidelity);
            }
        }

        public ITrackingSpan CreateCustomTrackingSpan(Span span, TrackingFidelityMode trackingFidelity, object customState, CustomTrackToVersion behavior)
        {
            if (behavior == null)
            {
                throw new ArgumentNullException(nameof(behavior));
            }
            if (trackingFidelity != TrackingFidelityMode.Forward)
            {
                throw new NotImplementedException();
            }
            return new ForwardFidelityCustomTrackingSpan(this, span, customState, behavior);
        }
        #endregion

        public override string ToString()
        {
            return String.Format(CultureInfo.CurrentCulture, "V{0} (r{1})", this.VersionNumber, ReiteratedVersionNumber);
        }
    }
}
