//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain internal APIs that are subject to change without notice.
// Use at your own risk.
//
using System.Collections.Generic;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Provides metadata for ClassificationFormatDefinitions.
    /// </summary>
    public interface IClassificationFormatMetadata : IEditorFormatMetadata, IOrderable
    {
        /// <summary>
        /// Gets a set of ClassificationTypeName objects from the ClassificationTypeAttribute.
        /// </summary>
        IEnumerable<string> ClassificationTypeNames { get; }
    }
}
