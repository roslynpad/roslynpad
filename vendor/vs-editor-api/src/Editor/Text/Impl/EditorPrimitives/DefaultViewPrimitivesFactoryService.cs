//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.EditorPrimitives.Implementation
{
    using System.Composition;

    using Microsoft.VisualStudio.Text.Editor;

    [Export(typeof(IViewPrimitivesFactoryService))]
    [Shared]
    public sealed class DefaultViewPrimitivesFactoryService : IViewPrimitivesFactoryService
    {
        [Import]
        public IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }

        [Import]
        public IBufferPrimitivesFactoryService BufferPrimitivesFactoryService { get; set; }

        #region IViewPrimitivesFactoryService Members

        public TextView CreateTextView(ITextView textView)
        {
            TextView textViewPrimitive = null;

            if (!textView.Properties.TryGetProperty<TextView>(EditorPrimitiveIds.ViewPrimitiveId, out textViewPrimitive))
            {
                textViewPrimitive = new DefaultTextViewPrimitive(textView, this, BufferPrimitivesFactoryService);
                textView.Properties.AddProperty(EditorPrimitiveIds.ViewPrimitiveId, textViewPrimitive);
            }
            return textViewPrimitive;
        }

        public DisplayTextPoint CreateDisplayTextPoint(TextView textView, int position)
        {
            return new DefaultDisplayTextPointPrimitive(textView, position, EditorOptionsFactoryService.GetOptions(textView.AdvancedTextView));
        }

        public DisplayTextRange CreateDisplayTextRange(TextView textView, TextRange textRange)
        {
            return new DefaultDisplayTextRangePrimitive(textView, textRange);
        }

        public LegacySelection CreateSelection(TextView textView)
        {
            if (textView.Selection == null)
            {
                // The selection will add itself to the view.
                return new DefaultSelectionPrimitive(textView, EditorOptionsFactoryService.GetOptions(textView.AdvancedTextView));
            }

            return textView.Selection;
        }

        public Caret CreateCaret(TextView textView)
        {
            if (textView.Caret == null)
            {
                // The caret will add itself to the view.
                return new DefaultCaretPrimitive(textView, EditorOptionsFactoryService.GetOptions(textView.AdvancedTextView));
            }

            return textView.Caret;
        }

        #endregion

    }
}
