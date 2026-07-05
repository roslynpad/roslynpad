//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text
{
    using System;

    /// <summary>
    /// Describes a version of an <see cref="ITextBuffer"/>. Each application of an <see cref="ITextEdit"/> to a text buffer
    /// generates a new <see cref="ITextVersion"/>.
    /// </summary>
    /// <remarks>Any <see cref="ITextVersion"/> will be upcastable to an <see cref="ITextVersion2"/>.</remarks>
    public interface ITextVersion2 : ITextVersion
    {
        ITextImageVersion ImageVersion { get; }
    }
}
