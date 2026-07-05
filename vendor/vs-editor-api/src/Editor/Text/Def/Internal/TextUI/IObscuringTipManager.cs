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
    /// Class used to manage tips displayed in a view.
    /// </summary>
    public interface IObscuringTipManager
    {
        void PushTip(ITextView view, IObscuringTip tip);

        void RemoveTip(ITextView view, IObscuringTip tip);
    }
}
