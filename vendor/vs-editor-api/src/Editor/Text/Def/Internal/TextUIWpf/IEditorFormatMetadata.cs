//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System.ComponentModel;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Provides metadata for EditorFormatDefinitions.
    /// </summary>
    public interface IEditorFormatMetadata
    {
        /// <summary>
        /// Gets the name that identifies a format.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Determines whether this format is visible to users.
        /// </summary>
        [DefaultValue(false)]
        bool UserVisible { get; }

        /// <summary>
        /// Priority of the format map, used to resolve situations where two format maps have
        /// the same name.
        /// </summary>
        [DefaultValue(0)]
        int Priority { get; }
    }
}
