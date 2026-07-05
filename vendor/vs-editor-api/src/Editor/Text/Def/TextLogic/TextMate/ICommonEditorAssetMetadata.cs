namespace Microsoft.VisualStudio.Editor
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Common Editor asset metadata.
    /// </summary>
    public interface ICommonEditorAssetMetadata : IOrderable
    {
        /// <summary>
        /// The type of tags supported by the Common Editor asset.
        /// </summary>
        [DefaultValue(null)]
        IEnumerable<Type> TagTypes { get; }
    }
}
