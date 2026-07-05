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
    using Microsoft.VisualStudio.Utilities;
    using System.Composition;

    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.BraceCompletionEnabledOptionName)]
    [Shared]
    public sealed class BraceCompletionEnabledOption : EditorOptionDefinition<bool>
    {
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.BraceCompletionEnabledOptionId; } }
        public override bool Default { get { return true; } }
    }
}
