// Copyright (c) Microsoft Corporation
// All rights reserved.

using Microsoft.VisualStudio.Imaging.Interop;
using System;
using System.Diagnostics;
using Avalonia.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// CompletionIcon2 uses <see cref="ImageMoniker"/>s instead of <see cref="IImage"/>s to reference icons.
    /// </summary>
    [CLSCompliant(false)]
#pragma warning disable CA1036 // Override methods on comparable types
    public class CompletionIcon2 : CompletionIcon
#pragma warning restore CA1036 // Override methods on comparable types
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CompletionIcon2"/>.
        /// </summary>
        public CompletionIcon2() : base()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionIcon2"/> with the given image, automation values, and position.
        /// </summary>
        /// <param name="imageMoniker">The moniker for the icon that describes the completion item.</param>
        /// <param name="automationName">The automation name for the icon.</param>
        /// <param name="automationId">The automation id for the icon.</param>
        /// <param name="position">The display position of the icon. If no value is provided this will be zero.</param>
        public CompletionIcon2(ImageMoniker imageMoniker, string automationName, string automationId, int position=0) : base()
        {
            this.IconMoniker = imageMoniker;
            this.AutomationName = automationName;
            this.AutomationId = automationId;
            this.Position = position;
        }

        /// <summary>
        /// Gets or sets the moniker used to define a multi-resolution image.
        /// </summary>
        public virtual ImageMoniker IconMoniker
        {
            get;
            private set;
        }

        /// <summary>
        /// This property is not supported by <see cref="CompletionIcon2"/> and will always return <value>null</value>.
        /// To get the current icon use <see cref="IconMoniker"/>.
        /// </summary>
        public override IImage IconSource
        {
            get
            {
                Debug.Fail("IconSource should not be used on Completion3");
                return null;
            }
            set
            {
                Debug.Assert(value == null, "IconSource should never be set on Completion3");
            }
        }
    }
}
