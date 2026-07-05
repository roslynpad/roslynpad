//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Utilities;
    using Microsoft.VisualStudio.Utilities;
    using System.Composition;

    [Export(typeof(ITextViewCreationListener))]
    [ContentType("text")]
    [TextViewRole(PredefinedTextViewRoles.Editable)]
    [Shared]
    public sealed class BraceCompletionManagerFactory : ITextViewCreationListener
    {
        #region Imports

        [Import]
        public IBraceCompletionAdornmentServiceFactory _adornmentServiceFactory { get; set; }

        [Import]
        public IBraceCompletionAggregatorFactory _aggregatorFactory { get; set; }

        [Import]
        public GuardedOperations _guardedOperations { get; set; }

        #endregion

        #region ITextViewCreationListener

        public void TextViewCreated(ITextView textView)
        {
            textView.Properties.AddProperty("BraceCompletionManager",
                new BraceCompletionManager(textView,
                    new BraceCompletionStack(textView, _adornmentServiceFactory, _guardedOperations), _aggregatorFactory, _guardedOperations));
        }

        #endregion
    }
}
