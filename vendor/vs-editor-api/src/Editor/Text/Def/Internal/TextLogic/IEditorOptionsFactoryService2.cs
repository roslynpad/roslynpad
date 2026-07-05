//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Support the scenario where we want to create the editor options and then create the corresponding view (so that the editor
    /// options can be seeded with options the view will want).
    /// </summary>
    public interface IEditorOptionsFactoryService2 : IEditorOptionsFactoryService
    {
        /// <summary>
        /// Create a new <see cref="IEditorOptions"/> that is not bound to a particular scope.
        /// </summary>
        /// <param name="allowLateBinding">If true, this option can be bound to a scope after it has been created using <see cref="TryBindToScope(IEditorOptions, IPropertyOwner)"/>.</param>
        /// <returns></returns>
        IEditorOptions CreateOptions(bool allowLateBinding);


        /// <summary>
        /// Binds <paramref name="option"/> to the specified scope if the scope does not have pre-existing <see cref="IEditorOptions"/> and <paramref name="option"/> was
        /// created using <see cref="CreateOptions(bool)"/> with the late binding allowed.
        /// </summary>
        /// <returns>true if <paramref name="option"/> was bound to <paramref name="scope"/>.</returns>
        bool TryBindToScope(IEditorOptions option, IPropertyOwner scope);

        /// <summary>
        /// Get the option definition associated with <paramref name="optionId"/>.
        /// </summary>
        /// <param name="optionId"></param>
        /// <returns></returns>
        EditorOptionDefinition GetOptionDefinition(string optionId);
    }
}
