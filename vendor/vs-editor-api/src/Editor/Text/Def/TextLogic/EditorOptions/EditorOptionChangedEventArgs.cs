//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Provides information for the <see cref="IEditorOptions.OptionChanged"/> event.
    /// </summary>
    public class EditorOptionChangedEventArgs : EventArgs
    {
        private string _optionId;

        /// <summary>
        /// Initializes a new instance of <see cref="EditorOptionChangedEventArgs"/>.
        /// </summary>
        /// <param name="optionId">The ID of the option.</param>
        public EditorOptionChangedEventArgs(string optionId)
        {
            _optionId = optionId;
        }

        /// <summary>
        /// Gets the ID of the option that has changed.
        /// </summary>
        public string OptionId { get { return _optionId; } }
    }
}
