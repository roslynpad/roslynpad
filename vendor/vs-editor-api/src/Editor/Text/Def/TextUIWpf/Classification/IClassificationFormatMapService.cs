//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Classification.
//  IClassificationFormatMapService").
//
namespace Microsoft.VisualStudio.Text.Classification
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Provides an <see cref="IClassificationFormatMap"/> for a view or for an appearance category.
    /// </summary>
    /// <remarks>This is a MEF component part; import it via [Import].</remarks>
    public interface IClassificationFormatMapService
    {
        /// <summary>
        /// Gets the <see cref="IClassificationFormatMap"/> appropriate for the given view. The map
        /// is shared by all views with the same appearance category option value.
        /// </summary>
        IClassificationFormatMap GetClassificationFormatMap(ITextView textView);

        /// <summary>
        /// Gets the <see cref="IClassificationFormatMap"/> for the given appearance category.
        /// </summary>
        IClassificationFormatMap GetClassificationFormatMap(string category);
    }
}
