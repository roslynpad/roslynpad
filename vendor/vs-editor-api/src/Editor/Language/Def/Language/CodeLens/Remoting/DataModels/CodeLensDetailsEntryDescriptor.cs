//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Defines a row entry in <see cref="CodeLensDetailsDescriptor"/>.
    /// </summary>
    public sealed class CodeLensDetailEntryDescriptor
    {
        /// <summary>
        /// A list of field values in the entry.
        /// </summary>
        /// <remarks>
        /// The order of <see cref="Fields"/> in an entry must be the same as the order of <see cref="CodeLensDetailsDescriptor.Headers"/>
        /// in the <see cref="CodeLensDetailsDescriptor"/>.
        /// </remarks>
        public IEnumerable<CodeLensDetailEntryField> Fields { get; set; }

        /// <summary>
        /// Tooltip for the entry.
        /// </summary>
        public string Tooltip { get; set; }

        /// <summary>
        /// Navigation command associated with the entry.
        /// </summary>
        public CodeLensDetailEntryCommand NavigationCommand { get; set; }

        /// <summary>
        /// Arguments for the navigation command.
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public IEnumerable<object> NavigationCommandArgs { get; set; }
    }
}
