//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Utilities
{
    using System;

    /// <summary>
    /// Orders multiple instances of an extension part.
    /// </summary>
    public sealed class OrderAttribute : MultipleBaseMetadataAttribute
    {
        private string before;
        private string after;

        /// <summary>
        /// The extension part to which this attribute is applied should be ordered before 
        /// the extension part with the name specified.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <exception cref="ArgumentException">The value is an empty string.</exception>
        public string Before
        {
            get 
            { 
                return this.before; 
            }
            set 
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length == 0)
                {
                    throw new ArgumentException("Before value must not be empty", nameof(value));
                }
                this.before = value; 
            }
        }

        /// <summary>
        /// The extension part to which this attribute is applied should be ordered after
        /// the extension part with the name specified.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value is null.</exception>
        /// <exception cref="ArgumentException">The value is an empty string.</exception>
        public string After
        {
            get 
            { 
                return this.after; 
            }
            set 
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length == 0)
                {
                    throw new ArgumentException("After value must not be empty", nameof(value));
                }
                this.after = value; 
            }
        }
    }
}
