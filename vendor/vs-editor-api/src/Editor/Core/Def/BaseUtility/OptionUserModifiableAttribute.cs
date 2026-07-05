//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    /// <summary>
    /// A MEF attribute determining if an option is user modifiable.
    /// </summary>
    public sealed class OptionUserModifiableAttribute : SingletonBaseMetadataAttribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="OptionUserModifiableAttribute"/>.
        /// </summary>
        /// <param name="userModifiable"><c>true</c> if the option is user modifiable; otherwise <c>false</c>.</param>
        public OptionUserModifiableAttribute(bool userModifiable)
        {
            this.OptionUserModifiable = userModifiable;
        }

        /// <summary>
        /// Determines whether the option is modifiable to the user.
        /// </summary>
        public bool OptionUserModifiable { get; }
    }
}
