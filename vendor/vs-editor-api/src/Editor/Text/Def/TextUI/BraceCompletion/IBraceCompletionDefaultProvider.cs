//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion
{
    /// <summary>
    /// Represents an extension point for the default non language-specific
    /// brace completion behaviors. It should be used to export metadata 
    /// containing the opening and closing braces to provide 
    /// for the given ContentType.
    /// </summary>
    /// <remarks>
    /// <para>This is a MEF component part, and should be exported with the following attribute:
    /// [Export(typeof(IBraceCompletionDefaultProvider))]
    /// </para>
    /// <para>
    /// Exports must include at least one [BracePair] attribute and at least one [ContentType] attribute.
    /// </para>
    /// </remarks>
    public interface IBraceCompletionDefaultProvider
    {

    }
}
