//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// A specialization of <see cref="IClassificationType"/> that includes a 'layer'
    /// designation.
    /// </summary>
    /// <remarks>
    /// <see cref="ILayeredClassificationType"/> can be used instead of <see cref="IClassificationType"/>
    /// to enable one component's classifications to categorically supersede another's classifications.
    /// </remarks>
    public interface ILayeredClassificationType : IClassificationType
    {
        /// <summary>
        /// The classification layer to which this classification belongs.
        /// </summary>
        ClassificationLayer Layer { get; }
    }
}
