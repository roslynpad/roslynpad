//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Text.Differencing
{
    public static class DifferenceBufferProperties
    {
        /// <summary>
        /// Add this property to an <see cref="ITextBuffer.Properties"/> to prevent a difference buffer from computing differences when the buffer
        /// is used as the left buffer.
        /// </summary>
        /// <remarks>
        /// This is intended for situations where you want to open a difference buffer but have not, yet, loaded the baseline. You can set the
        /// <see cref="IDifferenceBuffer.BaseLeftBuffer"/> to this and then change it to the correct buffer once it is available.
        /// </remarks>
        public const string PlaceholderBuffer = "PlaceholderBuffer";
    }
}
