//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
using System;
using System.Threading;

namespace Microsoft.VisualStudio.Text.Implementation
{
    /// <summary>
    /// An immutable variation on the StringBuilder class.
    /// </summary>
    internal class TextImageVersion : ITextImageVersion
    {
        public TextImageVersion(int length)
            : this(versionNumber: 0, reiteratedVersionNumber: 0, length: length, identifier: new object())
        {
        }

        private TextImageVersion(int versionNumber, int reiteratedVersionNumber, int length, object identifier)
        {
            this.VersionNumber = versionNumber;
            this.ReiteratedVersionNumber = reiteratedVersionNumber;
            this.Identifier = identifier;
            this.Length = length;
        }

        internal TextImageVersion CreateNext(int reiteratedVersionNumber, int length, INormalizedTextChangeCollection changes)
        {
            int newVersionNumber = this.VersionNumber + 1;

            if (reiteratedVersionNumber < 0)
            {
                // If there are no changes (e.g. readonly region edit or content type change), then
                // we consider this a reiteration of the current version. changes can be null in the special case
                // of doing a reload (at which point the reload code will call SetChanges after computing the diff).
                reiteratedVersionNumber = ((changes != null) && (changes.Count == 0)) ? this.ReiteratedVersionNumber : newVersionNumber;
            }
            else if (reiteratedVersionNumber > newVersionNumber)
            {
                throw new ArgumentOutOfRangeException(nameof(reiteratedVersionNumber));
            }

            if (length == -1)
            {
                length = this.Length;
                int changeCount = changes.Count;
                for (int c = 0; c < changeCount; ++c)
                {
                    length += changes[c].Delta;
                }
            }

            var newVersion = new TextImageVersion(newVersionNumber, reiteratedVersionNumber, length, this.Identifier);

            // Arguably this should happen as an atomic operation but it is unlikely to cause a race condition
            // because, in general, people won't even be looking at these properties until they get a change event
            // (which happens after everything has been set).
            this.SetChanges(changes);
            this.Next = newVersion;

            return newVersion;
        }

        // The length needs to be set after the creation of the version in some cases (projection, for example).
        internal void SetLength(int length)
        {
            if (this.Length != 0)
                throw new InvalidOperationException("Not allowed to SetLength twice");

            this.Length = length;
        }

        internal void SetChanges(INormalizedTextChangeCollection changes)
        {
            if (this.Changes != null)
                throw new InvalidOperationException("Not allowed to SetChanges twice");

            this.Changes = changes;
        }

        #region ITextImageVersion members
        public ITextImageVersion Next { get; private set; }

        public int Length { get; private set; }

        public INormalizedTextChangeCollection Changes { get; private set; }

        public int VersionNumber { get; }

        public int ReiteratedVersionNumber { get; }

        public object Identifier { get; }

        public int TrackTo(VersionedPosition other, PointTrackingMode mode)
        {
            if (other.Version == null)
                throw new ArgumentException(nameof(other) + " version cannot be null");

            if (other.Version.VersionNumber == this.VersionNumber)
                return other.Position;

            if (other.Version.VersionNumber > this.VersionNumber)
                return Tracking.TrackPositionForwardInTime(mode, other.Position, this, other.Version);
            else
                return Tracking.TrackPositionBackwardInTime(mode, other.Position, this, other.Version);
        }

        public Span TrackTo(VersionedSpan span, SpanTrackingMode mode)
        {
            if (span.Version == null)
                throw new ArgumentException(nameof(span) + " version cannot be null");

            if (span.Version.VersionNumber == this.VersionNumber)
                return span.Span;

            if (span.Version.VersionNumber > this.VersionNumber)
                return Tracking.TrackSpanForwardInTime(mode, span.Span, this, span.Version);
            else
                return Tracking.TrackSpanBackwardInTime(mode, span.Span, this, span.Version);
        }
        #endregion
    }
}
