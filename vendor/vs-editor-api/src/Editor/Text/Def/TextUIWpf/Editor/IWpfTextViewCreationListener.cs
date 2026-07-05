//
//  Copyright (c) Morgania contributors. Licensed under the MIT License.
//
//  Morgania-authored recreation (PLAN §3.3/§5.4, from public documentation:
//  learn.microsoft.com "Microsoft.VisualStudio.Text.Editor.IWpfTextViewCreationListener").
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Called when an <see cref="IWpfTextView"/> is created.
    /// </summary>
    /// <remarks>
    /// This is a MEF component part: export with [Export(typeof(IWpfTextViewCreationListener))]
    /// and [ContentType]/[TextViewRole] metadata to scope which views it observes.
    /// </remarks>
    public interface IWpfTextViewCreationListener
    {
        /// <summary>
        /// Called after the text view is created and its initial layer stack exists.
        /// </summary>
        void TextViewCreated(IWpfTextView textView);
    }
}
