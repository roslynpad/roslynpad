//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using Microsoft.VisualStudio.Text.Editor;
    using System;

    /// <summary>
    /// Provides the <see cref="IOutliningManager"/> for a given view model.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part, and should be imported as follows:
    /// [Import]
    /// IOutliningManagerService outliningManager = null;
    /// </remarks>
    public interface IOutliningManagerService
    {
        /// <summary>
        /// Gets an <see cref="IOutliningManager" /> for the given view.
        /// </summary>
        /// <remarks>
        /// The outlining manager is available only for views that have the <see cref="PredefinedTextViewRoles.Structured"/> role.
        /// Also, while IOutliningManager implements IDisposable, callers should take care to not dispose of it.
        /// </remarks>
        /// <param name="textView">The <see cref="ITextView"/> from which to get the outlining manager.</param>
        /// <returns>A valid outlining manager if the view model supports outlining,
        /// otherwise null.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="textView"/> is null.</exception>
        IOutliningManager GetOutliningManager(ITextView textView);
    }
}
