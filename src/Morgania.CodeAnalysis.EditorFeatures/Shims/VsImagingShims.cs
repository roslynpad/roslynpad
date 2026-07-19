// VS-shell imaging shims: the recompiled sources reference the proprietary
// Microsoft.VisualStudio.Imaging surface, recreated here from public documentation
// (learn.microsoft.com) over the host's image catalog.

namespace Microsoft.VisualStudio.Imaging
{
    using Microsoft.VisualStudio.Imaging.Interop;

    /// <summary>
    /// The image-catalog monikers the recompiled sources use. Ids are from the public
    /// KnownImageIds documentation (learn.microsoft.com,
    /// "Microsoft.VisualStudio.Imaging.KnownImageIds").
    /// </summary>
    public static class KnownMonikers
    {
        private static readonly Guid s_imageCatalog = new("ae27a6b0-e345-4288-96df-5eaf394ee369");

        public static ImageMoniker StatusError => new() { Guid = s_imageCatalog, Id = 2926 };

        public static ImageMoniker StatusWarning => new() { Guid = s_imageCatalog, Id = 2956 };
    }

    /// <summary>
    /// VS CrispImage stand-in: an image resolved from a moniker. Resolution is a host concern
    /// (Morgania has no proprietary image catalog); the host installs <see cref="ImageResolver"/>.
    /// </summary>
    public sealed class CrispImage : Avalonia.Controls.Image
    {
        private ImageMoniker _moniker;

        public static Func<ImageMoniker, Avalonia.Media.IImage?>? ImageResolver { get; set; }

        public ImageMoniker Moniker
        {
            get => _moniker;
            set
            {
                _moniker = value;
                Source = ImageResolver?.Invoke(value);
            }
        }
    }
}

namespace Microsoft.VisualStudio.PlatformUI
{
    public static class ImageThemingUtilities
    {
        /// <summary>
        /// In VS this re-tints catalog images against the given background; the host's image
        /// catalog already adapts icon colors to the theme background, so this is a no-op.
        /// </summary>
        public static void SetImageBackgroundColor(Avalonia.AvaloniaObject element, Avalonia.Media.Color color)
        {
        }
    }
}
