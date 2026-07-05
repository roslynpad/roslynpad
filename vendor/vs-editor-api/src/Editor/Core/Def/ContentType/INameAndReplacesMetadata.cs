//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;
using System.ComponentModel;

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents MEF metadata view corresponding to the <see cref="NameAttribute"/> and ReplacesAttributes.
    /// </summary>
    public interface INameAndReplacesMetadata
    {
        /// <summary>
        /// Declared name value.
        /// </summary>
        [DefaultValue(null)]
        string Name { get; }

        /// <summary>
        /// Declared Replaces values.
        /// </summary>
        [DefaultValue(null)]
        IEnumerable<string> Replaces { get; }
    }
}
