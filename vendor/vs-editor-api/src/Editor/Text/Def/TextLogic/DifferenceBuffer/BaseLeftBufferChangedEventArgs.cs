//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    using System;

    /// <summary>
    /// Raised whenever the left buffer of an <see cref="IDifferenceBuffer2"/> changes. This can only
    /// happen if <see cref="IDifferenceBuffer2.HasFixedBaseLeftBuffer"/> is false.
    /// </summary>
    public class BaseLeftBufferChangedEventArgs : EventArgs
    {
        public BaseLeftBufferChangedEventArgs(ITextBuffer oldBuffer, ITextBuffer newBuffer)
        {
            this.OldBuffer = oldBuffer;
            this.NewBuffer = newBuffer;
        }

        public ITextBuffer OldBuffer { get; }
        public ITextBuffer NewBuffer { get; }
    }
}
