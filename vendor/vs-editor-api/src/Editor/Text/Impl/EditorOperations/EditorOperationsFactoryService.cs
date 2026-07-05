//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations.Implementation
{
    using System;
    using System.Composition;

    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Formatting;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.VisualStudio.Text.Outlining;
    using Microsoft.VisualStudio.Language.Intellisense.Utilities;
    using Microsoft.VisualStudio.Text.Document;

    [Export(typeof(IEditorOperationsFactoryService))]
    [Shared]
    public sealed class EditorOperationsFactoryService : IEditorOperationsFactoryService
    {
        [Import]
        public IMultiSelectionBrokerFactory MultiSelectionBrokerFactory { get; set; }

        [Import]
        public ITextStructureNavigatorSelectorService TextStructureNavigatorFactory { get; set; }

        // This service should be optional: it is implemented on the VS side and other hosts may not implement it.
        [Import(AllowDefault = true)]
        public IWaitIndicator WaitIndicator { get; set; }

        [Import]
        public ITextSearchService TextSearchService { get; set; }

        [Import]
        public ITextUndoHistoryRegistry UndoHistoryRegistry { get; set; }

        [Import]
        public ITextBufferUndoManagerProvider TextBufferUndoManagerProvider { get; set; }

        [Import]
        public IEditorPrimitivesFactoryService EditorPrimitivesProvider { get; set; }

        [Import]
        public IEditorOptionsFactoryService EditorOptionsProvider { get; set; }

        //[Import]
        //internal IRtfBuilderService RtfBuilderService { get; set; }

        [Import]
        public ISmartIndentationService SmartIndentationService { get; set; }

        [Import]
        public ITextDocumentFactoryService TextDocumentFactoryService { get; set; }

        [Import]
        public IContentTypeRegistryService ContentTypeRegistryService { get; set; }

        [Import(AllowDefault = true)]
        public IOutliningManagerService OutliningManagerService { get; set; }

        [Import]
        public IWhitespaceManagerFactory WhitespaceManagerFactory { get; set; }

        [Import]
        public ITextViewZoomManager ZoomManager { get; set; }

        /// <summary>
        /// Provides a operations implementation for a given text view.
        /// </summary>
        /// <param name="textView">
        /// The text view to which the operations will be bound.
        /// </param> 
        /// <returns>
        /// An implementation of IEditorOperations that can provide operations implementations for the given text view.
        /// </returns>
        public IEditorOperations GetEditorOperations(ITextView textView)
        {
            // Validate
            if (textView == null)
            {
                throw new ArgumentNullException(nameof(textView));
            }

            // Only one EditorOperations should be created per ITextView
            IEditorOperations editorOperations = null;

            // We create one, only if it doesn't already exist
            if (!textView.Properties.TryGetProperty<IEditorOperations>(typeof(EditorOperationsFactoryService), out editorOperations))
            {
                editorOperations = new EditorOperations(textView, this);
                textView.Properties.AddProperty(typeof(EditorOperationsFactoryService), editorOperations);
            }

            return editorOperations;
        }
    }
}
