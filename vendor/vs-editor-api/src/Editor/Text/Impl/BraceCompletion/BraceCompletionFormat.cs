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
    using Microsoft.VisualStudio.Utilities;
    using System.Composition;
    using Avalonia.Media;

    [Export(typeof(EditorFormatDefinition))]
    [Name(BraceCompletionFormat.FormatName)]
    [UserVisible(true)]
    [Order(Before = Priority.Default)]
    [Shared]
    public sealed class BraceCompletionFormat : EditorFormatDefinition
    {
        public const string FormatName = "BraceCompletionClosingBrace";

        public BraceCompletionFormat()
        {
            this.DisplayName = Strings.ClosingBraceColorDefinitionName;

            this.BackgroundBrush = Brushes.LightBlue;
            this.ForegroundCustomizable = false;
            this.BackgroundCustomizable = true;
        }
    }
}
