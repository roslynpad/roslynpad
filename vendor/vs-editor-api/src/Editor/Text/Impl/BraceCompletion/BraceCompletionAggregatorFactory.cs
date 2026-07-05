//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using Microsoft.VisualStudio.Text.Operations;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Linq;

    [Export(typeof(IBraceCompletionAggregatorFactory))]
    [Shared]
    public class BraceCompletionAggregatorFactory : IBraceCompletionAggregatorFactory
    {
        #region Internal Properties

        internal IEnumerable<Lazy<IBraceCompletionSessionProvider, BraceCompletionMetadata>> SessionProviders { get; private set; }
        internal IEnumerable<Lazy<IBraceCompletionContextProvider, BraceCompletionMetadata>> ContextProviders { get; private set; }
        internal IEnumerable<Lazy<IBraceCompletionDefaultProvider, BraceCompletionMetadata>> DefaultProviders { get; private set; }
        internal IContentTypeRegistryService ContentTypeRegistryService { get; private set; }
        internal ITextBufferUndoManagerProvider UndoManager { get; private set; }
        internal IEditorOperationsFactoryService EditorOperationsFactoryService { get; private set; }
        internal IGuardedOperations GuardedOperations { get; private set; }

        #endregion

        #region Constructors

        [ImportingConstructor]
        public BraceCompletionAggregatorFactory(
            [ImportMany]IEnumerable<Lazy<IBraceCompletionSessionProvider, BraceCompletionMetadata>> sessionProviders,
            [ImportMany]IEnumerable<Lazy<IBraceCompletionContextProvider, BraceCompletionMetadata>> contextProviders,
            [ImportMany]IEnumerable<Lazy<IBraceCompletionDefaultProvider, BraceCompletionMetadata>> defaultProviders,
            IContentTypeRegistryService contentTypeRegistryService,
            ITextBufferUndoManagerProvider undoManager,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IGuardedOperations guardedOperations)
        {
            SessionProviders = sessionProviders;
            ContextProviders = contextProviders;
            DefaultProviders = defaultProviders;
            ContentTypeRegistryService = contentTypeRegistryService;
            UndoManager = undoManager;
            EditorOperationsFactoryService = editorOperationsFactoryService;
            GuardedOperations = guardedOperations;
        }

        #endregion

        #region IBraceCompletionAggregatorFactory

        public IBraceCompletionAggregator CreateAggregator()
        {
            return new BraceCompletionAggregator(this);
        }

        public IEnumerable<string> ContentTypes
        {
            get
            {
                return DefaultProviders.SelectMany(export => export.Metadata.ContentTypes)
                    .Concat(ContextProviders.SelectMany(export => export.Metadata.ContentTypes))
                    .Concat(SessionProviders.SelectMany(export => export.Metadata.ContentTypes));
            }
        }

        #endregion
    }
}
