//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Tagging
{
    /// <summary>
    /// Represents a tag for outlining regions.
    /// </summary>
    public class OutliningRegionTag : IOutliningRegionTag
    {
        #region IOutliningRegionTag Members
        /// <summary>
        /// Determines whether the region is collapsed by default.
        /// </summary>
        public bool IsDefaultCollapsed { get; private set; }

        /// <summary>
        /// Determines whether a region is an implementation region. 
        /// </summary>
        /// <remarks>
        /// Implementation regions are the blocks of code following a method definition. 
        /// They are used for commands such as the Visual Studio Collapse to Definition command, 
        /// which hides the implementation region and leaves only the method definition exposed.
        /// </remarks>
        public bool IsImplementation { get; private set; }

        /// <summary>
        /// Gets the data object for the collapsed UI. If the default is set, returns null.
        /// </summary>
        public object CollapsedForm { get; private set; }

        /// <summary>
        /// Gets the data object for the collapsed UI tooltip. If the default is set, returns null.
        /// </summary>
        public object CollapsedHintForm { get; private set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of a <see cref="OutliningRegionTag"/>. 
        /// </summary>
        public OutliningRegionTag()
            : this(false, false, null, null)
        { }
        
        /// <summary>
        /// Initializes a new instance of a <see cref="OutliningRegionTag"/> with the specified objects. 
        /// </summary>
        public OutliningRegionTag(object collapsedForm, object collapsedHintForm)
            : this(false, false, collapsedForm, collapsedHintForm)
        { }

        /// <summary>
        /// Initializes a new instance of a <see cref="OutliningRegionTag"/> with the specified default collapsed state.
        /// </summary>
        public OutliningRegionTag(bool isDefaultCollapsed, bool isImplementation, object collapsedForm, object collapsedHintForm)
        {
            IsDefaultCollapsed = isDefaultCollapsed;
            IsImplementation = isImplementation;
            CollapsedForm = collapsedForm;
            CollapsedHintForm = collapsedHintForm;
        }
    }
}
