//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines a <see cref="ITextView"/>-specific editor option.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with:
    /// [Export(typeof(EditorOptionDefinition))]
    /// </remarks>
    public abstract class ViewOptionDefinition<T> : EditorOptionDefinition<T>
    {
        /// <summary>
        /// Determines whether the option is applicable to the specified scope.
        /// </summary>
        public override bool IsApplicableToScope(IPropertyOwner scope)
        {
            return scope is ITextView;
        }
    }
}
