//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{

    using Microsoft.VisualStudio.Utilities;

    /// <summary>
    /// Represents a service that gets <see cref="IEditorOptions"/> for a specified scope or for the global scope.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IEditorOptionsFactoryService factory = null;
    /// </remarks>
    public interface IEditorOptionsFactoryService
    {
        /// <summary>
        /// Gets the <see cref="IEditorOptions"/> for the <see cref="IPropertyOwner"/>. Buffers and views are
        /// property owners. Creates new options for the scope if none have previously been created.
        /// </summary>
        /// <param name="scope">The <see cref="IPropertyOwner"/>.</param>
        /// <returns>The <see cref="IEditorOptions"/> for the given <see cref="IPropertyOwner"/>.</returns>
        /// <remarks>
        /// This method returns a set of options for a given scope. Options defined in this scope will
        /// not affect options in its ancestor scopes. If you try to get an option in this scope, the method checks
        /// for any overridden values in the scope. If there are none, it gets the value from the options of
        /// its parent scope. The set of applicable options may change depending on the
        /// scope. An option defined in a text view scope will not apply to text buffers.
        /// </remarks>
        IEditorOptions GetOptions(IPropertyOwner scope);

        /// <summary>
        /// Creates a new instance of <see cref="IEditorOptions"/> that is not bound to any
        /// particular scope.
        /// </summary>
        /// <returns>A new instance of <see cref="IEditorOptions"/>, parented on the
        /// <see cref="GlobalOptions"/></returns>
        IEditorOptions CreateOptions();

        /// <summary>
        /// Gets the global <see cref="IEditorOptions"/>.
        /// </summary>
        /// <remarks>
        /// An option set in the global scope does not override the same option set in a specific scope, but it is visible in
        /// a specific scope that has not overridden that option.
        /// </remarks>
        IEditorOptions GlobalOptions { get; }
    }
}
