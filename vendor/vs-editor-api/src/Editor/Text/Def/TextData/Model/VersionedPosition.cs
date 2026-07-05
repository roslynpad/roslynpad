//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Describes a location in a specific <see cref="ITextImageVersion"/>.
    /// </summary>
    public struct VersionedPosition : IEquatable<VersionedPosition>
    {

#pragma warning disable CA1051 // Do not declare visible instance fields
        [SuppressMessage("Microsoft.Security", "CA2104", Justification = "Type is readonly")]
        public readonly ITextImageVersion Version;
        public readonly int Position;
#pragma warning restore CA1051 // Do not declare visible instance fields

        public readonly static VersionedPosition Invalid = new VersionedPosition();

        public VersionedPosition(ITextImageVersion version, int position)
        {
            if (version == null)
            {
                throw new ArgumentNullException(nameof(version));
            }

            if ((position < 0) || (position > version.Length))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            this.Version = version;
            this.Position = position;
        }

        public static implicit operator int(VersionedPosition position)
        {
            return position.Position;
        }

        public VersionedPosition TranslateTo(ITextImageVersion other, PointTrackingMode mode)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            return new VersionedPosition(other, other.TrackTo(this, mode));
        }

        public override int GetHashCode()
        {
            return (this.Version != null) ? (this.Position ^ this.Version.GetHashCode()) : 0;
        }

        public override bool Equals(object obj)
        {
            if (obj is VersionedPosition)
            {
                var other = (VersionedPosition)obj;
                return this.Equals(other);
            }

            return false;
        }

        public bool Equals(VersionedPosition other)
        {
            return (other.Version == this.Version) && (other.Position == this.Position);
        }

        public static bool operator ==(VersionedPosition left, VersionedPosition right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VersionedPosition left, VersionedPosition right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return (this.Version == null)
                   ? nameof(Invalid)
                   : string.Format(System.Globalization.CultureInfo.CurrentCulture, "v{0}_{1}",
                                    this.Version.VersionNumber,
                                    this.Position);
        }
    }
}
