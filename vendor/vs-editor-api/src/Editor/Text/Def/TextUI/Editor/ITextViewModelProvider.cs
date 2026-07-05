//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides <see cref="ITextViewModel"/> objects.
    /// </summary>
    /// <remarks>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(NameSource=typeof(ITextViewModelProvider))]
    /// Component exporters must specify at least one ContentTypeAttribute characterizing the data
    /// models to which they apply and at least one TextViewRoleAttribute characterizing the views to which they apply.
    /// </remarks>
    public interface ITextViewModelProvider
    {
        /// <summary>
        /// Creates an <see cref="ITextViewModel"/> for the given <see cref="ITextDataModel"/>.
        /// </summary>
        /// <param name="dataModel">The <see cref="ITextDataModel"/> for which to create the <see cref="ITextViewModel"/>.</param>
        /// <param name="roles">The <see cref="ITextViewRoleSet"/> for the view that is about to be created.</param>
        /// <returns>The <see cref="ITextViewModel"/> created for <paramref name="dataModel"/>, 
        /// or <c>null</c> if the text view model cannot be created.</returns>
        ITextViewModel CreateTextViewModel(ITextDataModel dataModel, ITextViewRoleSet roles);
    }
}
