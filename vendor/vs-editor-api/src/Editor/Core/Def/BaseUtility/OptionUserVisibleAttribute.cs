//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A MEF attribute determining if an option is visible to the user.
    /// </summary>
    public sealed class OptionUserVisibleAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionUserVisibleAttribute"/>.
        /// </summary>
        /// <param name="userVisible"><c>true</c> if the option is visible to the user; otherwise <c>false</c>.</param>
        public OptionUserVisibleAttribute(bool userVisible)
        {
            this.OptionUserVisible = userVisible;
        }

        /// <summary>
        /// Determines whether the option is visible to the user.
        /// </summary>
        public bool OptionUserVisible { get; }
    }
}
