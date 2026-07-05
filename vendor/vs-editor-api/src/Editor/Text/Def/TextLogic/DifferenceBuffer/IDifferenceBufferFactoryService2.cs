//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Text.Projection;

namespace Microsoft.VisualStudio.Text.Differencing
{
    /// <summary>
    /// A factory for creating <see cref="IDifferenceBuffer"/> instances.
    /// </summary>
    /// <remarks>
    /// This is a MEF service and can be imported.
    /// </remarks>
    public interface IDifferenceBufferFactoryService2 : IDifferenceBufferFactoryService
    {
        /// <summary>
        /// Create an <see cref="IDifferenceBuffer"/> for the given left and right buffers and with the given difference options.
        /// </summary>
        /// <param name="leftBaseBuffer">The left (old, before) buffer.</param>
        /// <param name="rightBaseBuffer">The right (new, after) buffer.</param>
        /// <param name="options">The options to use in computing differences between the buffers.</param>
        /// <param name="disableEditing">If true, disable editing in the right and inlines views.</param>
        /// <param name="wrapLeftBuffer">If true, create a read-only projection of <paramref name="leftBaseBuffer"/> (which will prevent
        /// that buffer from being modified through the difference buffers).</param>
        /// <param name="wrapRightBuffer">If true and editing is disabled, create a read-only projection of <paramref name="rightBaseBuffer"/> (which will prevent
        /// that buffer from being modified through the difference buffers).</param>
        /// <param name="fixedBaseLeftBuffer">Allows, if false, the <see cref="IDifferenceBuffer.BaseLeftBuffer"/> can be changed.</param>
        /// <remarks>
        /// <para>If <paramref name="disableEditing"/> is false, then <paramref name="wrapRightBuffer"/> is ignored (and the right buffer will not be wrapped).</para>
        /// <para>If <paramref name="wrapLeftBuffer"/> is false, then the caller of this method is responsible for making sure <paramref name="leftBaseBuffer"/> is read-only.</para>
        /// <para>If <paramref name="disableEditing"/> is true and <paramref name="wrapRightBuffer"/> is false, then the caller of this method is responsible for making sure <paramref name="rightBaseBuffer"/> is read-only.</para>
        /// <para>If <paramref name="fixedBaseLeftBuffer"/> is false, then <paramref name="wrapLeftBuffer"/> is ignored and <paramref name="leftBaseBuffer"/> can be null.</para>
        /// </remarks>
        IDifferenceBuffer2 CreateDifferenceBuffer(ITextBuffer leftBaseBuffer, ITextBuffer rightBaseBuffer, StringDifferenceOptions options,
                                                  bool disableEditing, bool wrapLeftBuffer, bool wrapRightBuffer, bool fixedBaseLeftBuffer);

        /// <summary>
        /// Create an <see cref="IDifferenceBuffer"/> for the given left and right buffers and with the given difference options.
        /// </summary>
        /// <param name="innerLeftDataModel">The data model for the left buffer. This can be null.</param>
        /// <param name="rightDataModel">The right (new, after) buffer.</param>
        /// <param name="options">The options to use in computing differences between the buffers.</param>
        /// <param name="disableEditing">If true, disable editing in the right and inlines views.</param>
        /// <param name="wrapLeftBuffer">If true, create a read-only projection of <paramref name="leftBaseBuffer"/> (which will prevent
        /// that buffer from being modified through the difference buffers).</param>
        /// <param name="wrapRightBuffer">If true and editing is disabled, create a read-only projection of <paramref name="rightBaseBuffer"/> (which will prevent
        /// that buffer from being modified through the difference buffers).</param>
        /// <param name="fixedBaseLeftBuffer">Allows, if false, the <see cref="IDifferenceBuffer.BaseLeftBuffer"/> can be changed.</param>
        /// <remarks>
        /// <para>If <paramref name="disableEditing"/> is false, then <paramref name="wrapRightBuffer"/> is ignored (and the right buffer will not be wrapped).</para>
        /// <para>If <paramref name="wrapLeftBuffer"/> is false, then the caller of this method is responsible for making sure <paramref name="leftBaseBuffer"/> is read-only.</para>
        /// <para>If <paramref name="disableEditing"/> is true and <paramref name="wrapRightBuffer"/> is false, then the caller of this method is responsible for making sure <paramref name="rightBaseBuffer"/> is read-only.</para>
        /// <para>If <paramref name="fixedBaseLeftBuffer"/> is false, then <paramref name="wrapLeftBuffer"/> is ignored and <paramref name="innerLeftDataModel"/> can be null.</para>
        /// </remarks>
        IDifferenceBuffer2 CreateDifferenceBuffer(ITextDataModel innerLeftDataModel, ITextDataModel rightDataModel, StringDifferenceOptions options,
                                                  bool disableEditing, bool wrapLeftBuffer, bool wrapRightBuffer, bool fixedBaseLeftBuffer);
    }
}
