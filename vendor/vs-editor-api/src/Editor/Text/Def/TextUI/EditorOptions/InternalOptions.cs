//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Various editor options that we shouldn't be made public.
    /// </remarks>
    internal static class InternalOptions
    {
        public const string SuppressOutliningOptionName = "SuppressOutlining";
        public readonly static EditorOptionKey<bool> SuppressOutliningOptionId = new EditorOptionKey<bool>(SuppressOutliningOptionName);

        /// <summary>
        /// The option definition that determines the vertical scroll sensitivity in Editor.
        /// </summary>
        internal static readonly EditorOptionKey<int> EditorVerticalScrollSensitivityId = new EditorOptionKey<int>(EditorVerticalScrollSensitivityName);
        internal const string EditorVerticalScrollSensitivityName = "EditorVerticalScrollSensitivity";

        /// <summary>
        /// The option definition that determines the horizontal scroll sensitivity in Editor.
        /// </summary>
        internal static readonly EditorOptionKey<int> EditorHorizontalScrollSensitivityId = new EditorOptionKey<int>(EditorHorizontalScrollSensitivityName);
        internal const string EditorHorizontalScrollSensitivityName = "EditorHorizontalScrollSensitivity";
    }
}
