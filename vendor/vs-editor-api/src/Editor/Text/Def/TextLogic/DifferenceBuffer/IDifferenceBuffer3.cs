//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    using System;

    public interface IDifferenceBuffer3 : IDifferenceBuffer2
    {
        /// <summary>
        /// Raised whenever the <see cref="IDifferenceBuffer.BaseLeftBuffer"/> is about to change to a different buffer.
        /// </summary>
        event EventHandler<BaseLeftBufferChangedEventArgs> BaseLeftBufferChanging;
    }
}
