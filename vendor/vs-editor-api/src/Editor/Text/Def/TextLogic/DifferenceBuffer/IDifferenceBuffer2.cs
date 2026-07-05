//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Differencing
{
    using System;

    public interface IDifferenceBuffer2 : IDifferenceBuffer
    {
        /// <summary>
        /// True if the BaseLeftBuffer can never change. If false, the BaseLeftBuffer can change (via setting the InnerLeftDataModel) and
        /// can be null.
        /// </summary>
        bool HasFixedBaseLeftBuffer { get; }

        /// <summary>
        /// Raised whenever the <see cref="IDifferenceBuffer.BaseLeftBuffer"/> is changed to a different buffer.
        /// </summary>
        event EventHandler<BaseLeftBufferChangedEventArgs> BaseLeftBufferChanged;

        /// <summary>
        /// The <see cref="ITextDataModel"/> for the BaseLeftBuffer. This can be created even if <see cref="IDifferenceBuffer.BaseLeftBuffer"/> is null.
        /// </summary>
        ITextDataModel LeftDataModel { get; }

        /// <summary>
        /// The <see cref="ITextDataModel"/> actual ITextDataModel for the BaseLeftBuffer. This value is only meaningful if <see cref="HasFixedBaseLeftBuffer"/> is
        /// false. Set InnerLeftDataModel to null to set the difference buffer's BaseLeftBuffer to null.
        /// </summary>
        ITextDataModel InnerLeftDataModel { get; set; }

        /// <summary>
        /// The <see cref="ITextDataModel"/> for the right buffer.
        /// </summary>
        ITextDataModel RightDataModel { get; }

        /// <summary>
        /// The <see cref="ITextDataModel"/> for the inline buffer. This can be created even if <see cref="IDifferenceBuffer.BaseLeftBuffer"/> is null.
        /// </summary>
        ITextDataModel InlineDataModel { get; }
    }
}
