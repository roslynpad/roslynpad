//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.Language.CodeLens.Remoting
{
    /// <summary>
    /// Represents a data model describing the code element in a document on which CodeLens data point indicators would be requested.
    /// </summary>
    public sealed class CodeLensDescriptor
    {
        /// <summary>
        /// Full path to the document on which data points are requested.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// A <see cref="Guid"/> identifying the project to which the document belongs.
        /// </summary>
        public Guid ProjectGuid { get; set; }

        /// <summary>
        /// A text description for the code element with which a data point is associated.
        /// </summary>
        /// <remarks>
        /// Language services use this property to pass the text of the code element to data points.
        /// </remarks>
        public string ElementDescription { get; set; }

        /// <summary>
        /// A <see cref="Span"/> identifying the place of the code element with which a data point is associated.
        /// </summary>
        public Span? ApplicableToSpan { get; set; }

        /// <summary>
        /// The <see cref="CodeElementKinds"/> of the code element with which a data point is associated.
        /// </summary>
        public CodeElementKinds Kind { get; set; }
    }
}
