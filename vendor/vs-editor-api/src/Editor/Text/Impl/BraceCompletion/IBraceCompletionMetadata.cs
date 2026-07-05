//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
// This file contain implementations details that are subject to change without notice.
// Use at your own risk.
//
namespace Microsoft.VisualStudio.Text.BraceCompletion.Implementation
{
    using System.Collections.Generic;

    /// <summary>
    /// Metadata for IBraceCompletionSessionProvider exports
    /// </summary>
    public interface IBraceCompletionMetadata
    {
        /// <summary>
        /// List of opening tokens.
        /// </summary>
        IEnumerable<char> OpeningBraces { get; }

        /// <summary>
        /// List of closing tokens.
        /// </summary>
        IEnumerable<char> ClosingBraces { get; }

        /// <summary>
        /// Supported content types.
        /// </summary>
        IEnumerable<string> ContentTypes { get; }
    }

    /// <summary>
    /// Concrete metadata view for <see cref="IBraceCompletionMetadata"/>; System.Composition cannot
    /// proxy interface views, so imports use this class (PLAN §5.2 rule 2).
    /// </summary>
    public sealed class BraceCompletionMetadata : IBraceCompletionMetadata
    {
        public BraceCompletionMetadata(System.Collections.Generic.IDictionary<string, object> data)
        {
            this.OpeningBraces = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<char>(data, nameof(OpeningBraces));
            this.ClosingBraces = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<char>(data, nameof(ClosingBraces));
            this.ContentTypes = Microsoft.VisualStudio.Utilities.MetadataValue.GetMany<string>(data, nameof(ContentTypes));
        }

        public System.Collections.Generic.IEnumerable<char> OpeningBraces { get; }
        public System.Collections.Generic.IEnumerable<char> ClosingBraces { get; }
        public System.Collections.Generic.IEnumerable<string> ContentTypes { get; }
    }
}
