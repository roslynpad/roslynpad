namespace Microsoft.VisualStudio.Text.Adornments
{
    using System;
    using Microsoft.VisualStudio.Core.Imaging;

    /// <summary>
    /// Represents cross platform compatible image.
    /// </summary>
    ///
    /// <remarks>
    /// <see cref="ImageElement"/>s should be constructed with <see cref="Morgania.Core.Imaging.ImageId"/>s
    /// that correspond to an image on that platform.
    /// </remarks>
    public class ImageElement
    {
        /// <summary>
        /// Creates a new instance of an image element.
        /// </summary>
        /// <param name="imageId"> A unique identifier for an image</param>
        public ImageElement(ImageId imageId)
        {
            this.ImageId = imageId;
        }

        /// <summary>
        /// Creates a new instance of an image element.
        /// </summary>
        /// <param name="imageId"> A unique identifier for an image</param>
        /// <param name="automationName"> Localized description of the image</param>
        public ImageElement(ImageId imageId, string automationName)
            : this(imageId)
        {
            // Let's allow empty strings, as long as they are not null references
            this.AutomationName = automationName ?? throw new ArgumentNullException(nameof(automationName));
        }

        /// <summary>
        /// A unique identifier for an image.
        /// </summary>
        public ImageId ImageId { get; }

        /// <summary>
        /// Localized description of the image
        /// </summary>
        public string AutomationName { get; }
    }
}
