//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text
{
    /// <summary>
    /// Edit tag indicating that the edit should be ignored by the undo system.
    /// </summary>
    /// <remarks>
    /// <para>
    ///  Yes this is as dangerous as it sounds. Using it will corrupt the undo stack so
    ///  do not use unless you are prepared to do the appropriate clean-up.
    /// </para>
    /// </remarks>
    public interface IBypassUndoEditTag : IUndoEditTag
    {
    }
}
