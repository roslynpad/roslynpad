//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// An attribute to be placed on an event handler for <see cref="ITextView.MouseHover"/>,
    /// specifying the delay between the time when the mouse stops moving
    /// and the generation of the hover event.
    /// </summary>
    /// <remarks>The default, if no MouseHoverAttribute is specified, is 150ms.</remarks>
    [global::System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class MouseHoverAttribute : Attribute
    {
        #region Private Members
        private readonly int _delay;
        #endregion

        /// <summary>
        /// Initializes a new instance of <see cref="MouseHoverAttribute"/>.
        /// </summary>
        /// <param name="delay">The time in milliseconds between the time when the mouse stops moving and the generation of the hover event.</param>
        public MouseHoverAttribute(int delay)
        {
            _delay = delay;
        }

        /// <summary>
        /// Gets the time in milliseconds between the time when the mouse stops moving and the generation of the hover event.
        /// </summary>
        public int Delay
        {
            get
            {
                return _delay;
            }
        }
    }
}
