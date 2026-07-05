//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Operations
{
    using Microsoft.VisualStudio.Text.Editor;

    /// <summary>
    /// Options applicable to Expand and Contract Selection.
    /// </summary>
    public static class ExpandContractSelectionOptions
    {
        /// <summary>
        /// The option that determines whether or not expand and contract selection is enable for a particular language.
        /// </summary>
        public const string ExpandContractSelectionEnabledOptionId = "ExpandContractSelectionEnabled";
        public static readonly EditorOptionKey<bool> ExpandContractSelectionEnabledKey = new EditorOptionKey<bool>(ExpandContractSelectionEnabledOptionId);
    }
}
