//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.IWpfTextViewMarginProvider").
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Creates an <see cref="IWpfTextViewMargin"/> for a view host.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part: export with [Export(typeof(IWpfTextViewMarginProvider))]
    /// and provide [Name], [MarginContainer], [Order], [ContentType] (and optionally
    /// [TextViewRole]) metadata.
    /// </remarks>
    public interface IWpfTextViewMarginProvider
    {
        /// <summary>
        /// Creates the margin, or returns null if the margin is not applicable to the host.
        /// </summary>
        IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer);
    }
}
