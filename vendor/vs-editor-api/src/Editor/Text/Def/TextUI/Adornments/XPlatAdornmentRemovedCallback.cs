//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Editor
{

    /// <summary>
    /// Defines the behavior when a <see cref="object"/> is removed from an <see cref="IXPlatAdornmentLayer"/>.
    /// </summary>
    /// <param name="tag">The tag associated with <paramref name="element"/>.</param>
    /// <param name="element">The <see cref="object"/> removed from the view.</param>
    public delegate void XPlatAdornmentRemovedCallback(object tag, object element);
}
