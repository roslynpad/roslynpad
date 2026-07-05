//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Differencing
{
    public static class DifferenceBufferOptions
    {
        /// <summary>
        /// What type of whitespace, if any, to ignore when performing line-level differencing.
        /// </summary>
        public static readonly EditorOptionKey<IgnoreWhiteSpaceBehavior> IgnoreWhiteSpaceBehaviorId = new EditorOptionKey<IgnoreWhiteSpaceBehavior>(IgnoreWhiteSpaceBehaviorName);
        public const string IgnoreWhiteSpaceBehaviorName = "Diff/Buffer/IgnoreWhitespaceBehavior";

        /// <summary>
        /// Whether or not to ignore case when performing line-level differencing.
        /// </summary>
        public static readonly EditorOptionKey<bool> IgnoreCaseId = new EditorOptionKey<bool>(IgnoreCaseName);
        public const string IgnoreCaseName = "Diff/Buffer/IgnoreCase";
    }

    /// <summary>
    /// A base class that can be used for options that are specific to an <see cref="IDifferenceBuffer"/>.
    /// </summary>
    public abstract class DifferenceBufferOption<T> : EditorOptionDefinition<T>
    {
        public override bool IsApplicableToScope(IPropertyOwner scope)
        {
            return scope is IDifferenceBuffer;
        }
    }
}