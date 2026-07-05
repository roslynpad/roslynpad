//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using Microsoft.VisualStudio.Text.Classification;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Utilities;
    using System.Composition;

    [Export(typeof(IBraceCompletionAdornmentServiceFactory))]
    [Shared]
    public class BraceCompletionAdornmentServiceFactory : IBraceCompletionAdornmentServiceFactory
    {
        [Export]
        [Name(PredefinedAdornmentLayers.BraceCompletion)]
        [Order(After = PredefinedAdornmentLayers.DifferenceSpace)]
        public AdornmentLayerDefinition braceCompletionAdornmentLayerDefinition { get; set; }

        [Import]
        public IEditorFormatMapService _editorFormatMapService { get; set; }

        #region IBraceCompletionAdornmentServiceFactory

        public IBraceCompletionAdornmentService GetOrCreateService(ITextView textView)
        {
            // Get the service from the view's property bag
            return textView.Properties.GetOrCreateSingletonProperty<IBraceCompletionAdornmentService>(
                () => new BraceCompletionAdornmentService(textView, _editorFormatMapService.GetEditorFormatMap(textView)));
        }

        #endregion
    }
}
