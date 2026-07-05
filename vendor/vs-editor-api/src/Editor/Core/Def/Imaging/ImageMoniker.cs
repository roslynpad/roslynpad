//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored replacement for the legacy image moniker interop struct
//  (PLAN §3.3: recreated from public documentation, learn.microsoft.com
//  "Microsoft.VisualStudio.Imaging.Interop.ImageMoniker"; the proprietary
//  Imaging packages are never referenced). A moniker identifies an image as a
//  (catalog guid, integer id) pair; the modern IntelliSense path uses
//  Morgania.Core.Imaging.ImageId instead, and this type exists only so the
//  legacy completion/suggested-action members keep their original shape.
//
using System;

namespace Microsoft.VisualStudio.Imaging.Interop
{
    /// <summary>
    /// Uniquely identifies an image within an image catalog.
    /// </summary>
    public struct ImageMoniker : IEquatable<ImageMoniker>
    {
        /// <summary>
        /// The <see cref="System.Guid"/> of the image catalog containing the image.
        /// </summary>
        public Guid Guid;

        /// <summary>
        /// The id of the image within the catalog.
        /// </summary>
        public int Id;

        public bool Equals(ImageMoniker other) => this.Guid == other.Guid && this.Id == other.Id;

        public override bool Equals(object obj) => obj is ImageMoniker other && this.Equals(other);

        public override int GetHashCode() => this.Guid.GetHashCode() ^ this.Id;

        public static bool operator ==(ImageMoniker left, ImageMoniker right) => left.Equals(right);

        public static bool operator !=(ImageMoniker left, ImageMoniker right) => !left.Equals(right);
    }
}
