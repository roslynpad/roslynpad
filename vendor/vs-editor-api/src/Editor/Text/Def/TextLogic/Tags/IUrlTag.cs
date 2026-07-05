//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// A tag that represents a URL.
    /// </summary>
    public interface IUrlTag : ITag
    {
        /// <summary>
        /// The URL.
        /// </summary>
        Uri Url { get; }
    }
}
