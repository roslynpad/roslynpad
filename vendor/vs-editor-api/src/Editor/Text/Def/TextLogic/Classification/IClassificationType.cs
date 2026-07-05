//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using System.Collections.Generic;

    /// <summary>
    /// The logical classification type of a span of text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All classification types are identified by a unique name.
    /// The <see cref="IClassificationTypeRegistryService"></see> can return an <see cref="IClassificationType"/> object from this 
    /// unique name in order to allow clients to access additional information.
    /// </para>
    /// <para>
    /// Classification types can multiply inherit by stacking <see cref="ClassificationTypeAttribute" /> attributes./>
    /// </para>
    /// </remarks>
    public interface IClassificationType
    {
        /// <summary>
        /// Gets the name of the classification type.
        /// </summary>
        /// <remarks>All classification types are identified by a unique name.
        /// The <see cref="IClassificationTypeRegistryService"></see> can return an <see cref="IClassificationType"/> from this name.</remarks>
        /// <value>This name is never <c>null</c>.</value>
        string Classification { get; }

        /// <summary>
        /// Determines whether the current <see cref="IClassificationType"></see>
        /// derives from the classification type named <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The name of the base classification type.</param>
        /// <returns><c>true</c> if the current classification type derives from the one identified by <paramref name="type"/>, otherwise <c>false</c>.</returns>
        bool IsOfType(string type);

        /// <summary>
        /// Gets the classification types from which the current <see cref="IClassificationType"/> is derived.
        /// </summary>
        /// <value>This value is never <c>null</c>, though it may be the empty set.</value>
        IEnumerable<IClassificationType> BaseTypes { get; }
    }
}
