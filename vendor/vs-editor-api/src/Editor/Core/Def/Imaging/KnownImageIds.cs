using System;

namespace Microsoft.VisualStudio.Imaging
{
    /// <summary>
    /// Minimal subset of the Visual Studio image catalog identifiers used by consumers of this
    /// editor platform. The catalog itself is not available here; hosts map these identifiers to
    /// their own imagery.
    /// </summary>
    public static class KnownImageIds
    {
        /// <summary>
        /// The identifier of the Visual Studio image catalog.
        /// </summary>
        public static readonly Guid ImageCatalogGuid = new Guid("ae27a6b0-e345-4288-96df-5eaf394ee369");

        /// <summary>
        /// The "expand scope" image, used by the completion list's expander button.
        /// </summary>
        public const int ExpandScope = 1275;
    }
}
