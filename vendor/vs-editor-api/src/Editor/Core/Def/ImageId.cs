namespace Microsoft.VisualStudio.Core.Imaging
{
    using System;

#pragma warning disable CA1720 // Identifier contains type name
#pragma warning disable CA1051 // Do not declare visible instance fields
    /// <summary>
    /// Unique identifier for Visual Studio image asset.
    /// </summary>
    /// <remarks>
    /// On Windows systems, <see cref="ImageId"/> can be converted to and from
    /// various other image representations via the ImageIdExtensions extension methods.
    /// </remarks>
    public struct ImageId : IEquatable<ImageId>
    {
        /// <summary>
        /// The <see cref="Guid"/> identifying the group to which this image belongs.
        /// </summary>
        public readonly Guid Guid;

        /// <summary>
        /// The <see cref="int"/> identifying the particular image from the group that this id maps to.
        /// </summary>
        public readonly int Id;

        /// <summary>
        /// Creates a new instance of ImageId.
        /// </summary>
        /// <param name="guid">The <see cref="Guid"/> identifying the group to which this image belongs.</param>
        /// <param name="id">The <see cref="int"/> identifying the particular image from the group that this id maps to.</param>
        public ImageId(Guid guid, int id)
        {
            this.Guid = guid;
            this.Id = id;
        }
        public override string ToString()
        {
            return this.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        public string ToString(IFormatProvider provider)
        {
            return string.Format(provider, @"{0} : {1}", this.Guid.ToString("D", provider), Id.ToString(provider));
        }

        bool IEquatable<ImageId>.Equals(ImageId other) => Id.Equals(other.Id) && Guid.Equals(other.Guid);

        public override bool Equals(object other) => other is ImageId otherImage && ((IEquatable<ImageId>)this).Equals(otherImage);

        public static bool operator ==(ImageId left, ImageId right) => left.Equals(right);

        public static bool operator !=(ImageId left, ImageId right) => !(left == right);

        public override int GetHashCode() => Guid.GetHashCode() ^ Id.GetHashCode();
    }
#pragma warning restore CA1720 // Identifier contains type name
#pragma warning restore CA1051 // Do not declare visible instance fields
}
