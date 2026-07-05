//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines an editor option specific to an <see cref="IWpfTextView"/>.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with:
    /// [Export(typeof(EditorOptionDefinition))]
    /// </remarks>
    public abstract class WpfViewOptionDefinition<T> : EditorOptionDefinition<T>
    {
        /// <summary>
        /// Determines whether this definition is applicable only to text views.
        /// </summary>
        public override bool IsApplicableToScope(IPropertyOwner scope)
        {
            return scope is ITextView;
        }
    }
}
