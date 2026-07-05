//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a navigation command invokable from the details pane.
    /// </summary>
    /// <remarks>
    /// A command can only have either the <see cref="CommandName"/> or
    /// the pair of <see cref="CommandSet"/> and <see cref="CommandId"/>,
    /// depending on the platform on which the code runs:
    /// <list type="bullet">
    /// <item>On Windows, the <see cref="CommandSet"/> and <see cref="CommandId"/> pair is used.</item>
    /// <item>On Mac, the <see cref="CommandName"/> is used.</item>
    /// </list>
    /// </remarks>
    public sealed class CodeLensDetailEntryCommand
    {
        /// <summary>
        /// The command name.
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// The command group <see cref="Guid"/>.
        /// </summary>
        public Guid? CommandSet { get; set; }

        /// <summary>
        /// The command Id.
        /// </summary>
        public int? CommandId { get; set; }
    }
}
