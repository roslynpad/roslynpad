//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Document
{
    /// <summary>
    /// Specifies the types of changes for modified text.
    /// </summary>
    [System.Flags]
    public enum ChangeTypes
    {
        /// <summary>
        /// No change types are set.
        /// </summary>
        None = 0x00,

        /// <summary>
        /// The change occurred after the document was opened.
        /// </summary>
        ChangedSinceOpened = 0x01,

        /// <summary>
        /// The change occurred after the document was saved.
        /// </summary>
        ChangedSinceSaved = 0x02
    }
}
