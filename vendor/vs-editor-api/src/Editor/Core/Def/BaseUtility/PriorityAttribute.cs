//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//

namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// Represents an attribute which assigns an integer priority to a MEF component part.
    /// </summary>
    public sealed class PriorityAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Creates a new instance of this attribute, assigning it a priority value.
        /// </summary>
        /// <param name="priority">The priority for the MEF component part.  Lower integer
        /// values represent higher precedence.</param>
        public PriorityAttribute(int priority)
        {
            this.Priority = priority;
        }

        /// <summary>
        /// Gets the priority for the attributed MEF extension.
        /// </summary>
        public int Priority { get; }
    }
}
