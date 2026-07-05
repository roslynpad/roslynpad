//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Classification
{
    /// <summary>
    /// Determining if an export should be visible to the user.
    /// </summary>
    public sealed class UserVisibleAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserVisibleAttribute"/>.
        /// </summary>
        /// <param name="userVisible"><c>true</c> if the extension is visible to the user, otherwise <c>false</c>.</param>
        public UserVisibleAttribute(bool userVisible)
        {
            this.UserVisible = userVisible;
        }

        /// <summary>
        /// Determines whether the extension is visible to the user.
        /// </summary>
        public bool UserVisible { get; private set; }
    }
}
