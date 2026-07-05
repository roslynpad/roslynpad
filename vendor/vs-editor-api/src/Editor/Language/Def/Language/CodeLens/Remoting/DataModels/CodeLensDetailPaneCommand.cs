//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a navigation command in the detail pane.
    /// </summary>
    public sealed class CodeLensDetailPaneCommand
    {
        /// <summary>
        /// The command text displayed in the pane.
        /// </summary>
        public string CommandDisplayName { get; set; }

        /// <summary>
        /// The navigation command.
        /// </summary>
        public CodeLensDetailEntryCommand CommandId { get; set; }

        /// <summary>
        /// The command arguments.
        /// </summary>
        [JsonProperty(ItemTypeNameHandling = TypeNameHandling.Auto)]
        public IEnumerable<object> CommandArgs { get; set; }
    }
}
