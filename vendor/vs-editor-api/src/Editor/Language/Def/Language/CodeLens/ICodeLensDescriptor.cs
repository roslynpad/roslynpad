//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.CodeLens
{
    /// <summary>
    /// Represents an object describing a code element at the location where an <see cref="ICodeLensTag"/> will be created.
    /// </summary>
    public interface ICodeLensDescriptor
    {
        /// <summary>
        /// The absolute file path of the document in which the descriptor is created.
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// The containing project of the document. Can be <see cref="Guid.Empty"/>
        /// if the document is a solution's miscellaneous file, or if it does not need to be specified.
        /// </summary>
        Guid ProjectGuid { get; }

        /// <summary>
        /// A short description of the element for which this descriptor is created.
        /// </summary>
        string ElementDescription { get; }

        /// <summary>
        /// The <see cref="Span"/> of the element.
        /// </summary>
        Span? ApplicableSpan { get; }

        /// <summary>
        /// The <see cref="CodeElementKinds"/> of the element.
        /// </summary>
        CodeElementKinds Kind { get; }
    }
}
