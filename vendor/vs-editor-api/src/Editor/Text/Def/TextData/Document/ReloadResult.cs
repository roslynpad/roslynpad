//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// The return value of Reload methods on <see cref="ITextDocument" />.
    /// </summary>
    public enum ReloadResult
    {
        /// <summary>
        /// The reload was blocked by the text document buffer's read only regions or <see cref="ITextBuffer.Changing"/> event.
        /// </summary>
        Aborted,

        /// <summary>
        /// The reload completed.
        /// </summary>
        Succeeded,

        /// <summary>
        /// The reload completed but some bytes could not be decoded and were replaced with a replacement character.
        /// </summary>
        SucceededWithCharacterSubstitutions
    }
}
