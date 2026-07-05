//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// An implementation of <see cref="IOverviewMarkTag" />.
    /// </summary>
    public class OverviewMarkTag : IOverviewMarkTag
    {
        readonly string _markKindName;

        /// <summary>
        /// Initializes a new instance of a <see cref="OverviewMarkTag"/> of the specified kind.
        /// </summary>
        /// <param name="markKindName">The name of the EditorFormatDefinition whose background color is used to draw the mark.</param>
        public OverviewMarkTag(string markKindName)
        {
            _markKindName = markKindName;
        }

        /// <summary>
        /// Gets the name of the EditorFormatDefinition whose background color is used to draw the mark.
        /// </summary>
        public string MarkKindName
        {
            get { return _markKindName; }
        }
    }
}
