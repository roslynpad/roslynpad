//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Formatting
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides <see cref="ILineTransformSource"/> objects.  
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(ILineTransformSourceProvider))]
    /// Exporters must supply a ContentTypeAttribute and TextViewRoleAttribute.
    /// </remarks>
    public interface ILineTransformSourceProvider
    {
        /// <summary>
        /// Creates an <see cref="ILineTransformSource"/> for the given <paramref name="textView"/>.
        /// </summary>
        /// <param name="textView">The <see cref="ITextView"/> on which the <see cref="ILineTransformSource"/> will format.</param>
        /// <returns>The new <see cref="ILineTransformSource"/>.  
        /// The value may be null if this <see cref="ILineTransformSourceProvider"/> decides not to participate.</returns>
        ILineTransformSource Create(ITextView textView);
    }
}
