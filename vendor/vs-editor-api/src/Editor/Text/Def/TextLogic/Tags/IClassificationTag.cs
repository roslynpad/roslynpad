//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    using Microsoft.VisualStudio.Text.Classification;

    /// <summary>
    /// A tag that represents a classification type.
    /// </summary>
    public interface IClassificationTag : ITag
    {
        /// <summary>
        /// The classification type associated with this tag.
        /// </summary>
        IClassificationType ClassificationType { get; }
    }
}
