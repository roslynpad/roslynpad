//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Outlining
{
    using System;
    using System.Composition;
    using Microsoft.VisualStudio.Text.Editor;
    using Microsoft.VisualStudio.Text.Tagging;
    using Microsoft.VisualStudio.Threading;
    using Microsoft.VisualStudio.Utilities;
    using Microsoft.Win32;

    [Export(typeof(IOutliningManagerService))]
    [Shared]
    public class OutliningManagerService : IOutliningManagerService
    {
        [Import]
        public IBufferTagAggregatorFactoryService TagAggregatorFactory { get; set; }

        [Import]
        public IEditorOptionsFactoryService EditorOptionsFactoryService { get; set; }

        // While these are IDisposable, they are kept for the life of the textView, meaning that callers shouldn't actually
        // dispose of them.
        public IOutliningManager GetOutliningManager(ITextView textView)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));

            if (!textView.Roles.Contains(PredefinedTextViewRoles.Structured))
                return null;

            return textView.Properties.GetOrCreateSingletonProperty(delegate
            {
                var tagAggregator = TagAggregatorFactory.CreateTagAggregator<IOutliningRegionTag>(textView.TextBuffer);
                var manager = new OutliningManager(textView.TextBuffer, tagAggregator, EditorOptionsFactoryService.GlobalOptions);
                textView.Closed += delegate { manager.Dispose(); };
                return manager;
            });
        }

        [Export(typeof(EditorOptionDefinition))]
        [Name(InternalOptions.SuppressOutliningOptionName)]
        [Shared]
        public sealed class SuppressOutliningOption : EditorOptionDefinition<bool>
        {
            public override bool Default => false;

            public override EditorOptionKey<bool> Key => InternalOptions.SuppressOutliningOptionId;
        }
    }
}
