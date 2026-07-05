//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Describes a span in a specific <see cref="ITextImageVersion"/>.
    /// </summary>
    public struct VersionedSpan : IEquatable<VersionedSpan>
    {
#pragma warning disable CA1051 // Do not declare visible instance fields
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Type is readonly")]
        public readonly ITextImageVersion Version;
        public readonly Span Span;
#pragma warning disable CA1051 // Do not declare visible instance fields

        public readonly static VersionedSpan Invalid = new VersionedSpan();

        public VersionedSpan(ITextImageVersion version, Span span)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if (span.End > version.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(span));
            }

            this.Version = version;
            this.Span = span;
        }

        public static implicit operator Span(VersionedSpan span)
        {
            return span.Span;
        }

        public VersionedSpan TranslateTo(ITextImageVersion other, SpanTrackingMode mode)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new VersionedSpan(other, other.TrackTo(this, mode));
        }

        public override int GetHashCode()
        {
            return (this.Version != null) ? (this.Span.GetHashCode() ^ this.Version.GetHashCode()) : 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is VersionedSpan)
            {
                var other = (VersionedSpan)obj;
                return this.Equals(other);
            }

            return false;
        }

        public bool Equals(VersionedSpan other)
        {
            return (other.Version == this.Version) && (other.Span == this.Span);
        }

        public static bool operator ==(VersionedSpan left, VersionedSpan right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VersionedSpan left, VersionedSpan right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return (this.Version == null)
                   ? nameof(Invalid)
                   : string.Format(System.Globalization.CultureInfo.CurrentCulture, "v{0}_{1}",
                                   this.Version.VersionNumber,
                                   this.Span);
        }
    }
}
