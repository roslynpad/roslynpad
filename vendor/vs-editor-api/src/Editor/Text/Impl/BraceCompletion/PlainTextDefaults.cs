//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using Microsoft.VisualStudio.Utilities;
    using System.Composition;

    /// <summary>
    /// Sets the default braces to auto complete on plain text files
    /// </summary>
    [Export(typeof(IBraceCompletionDefaultProvider))]
    [BracePair('(', ')')]
    [BracePair('"', '"')]
    [BracePair('{', '}')]
    [BracePair('[', ']')]
    [ContentType("plaintext")]
    [Shared]
    public sealed class PlainTextDefaults : IBraceCompletionDefaultProvider
    {

    }
}
