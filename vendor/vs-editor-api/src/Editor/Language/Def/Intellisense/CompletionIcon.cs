////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using Avalonia.Media;

namespace Microsoft.VisualStudio.Language.Intellisense
{
#pragma warning disable CA1036 // Override methods on comparable types
    /// <summary>
    /// Represents an icon used in the completion.
    /// </summary>
    public class CompletionIcon : IComparable<CompletionIcon>
#pragma warning restore CA1036 // Override methods on comparable types
    {
        public virtual IImage IconSource { get; set; }
        public virtual string AutomationName { get; set; }
        public virtual string AutomationId { get; set; }
        public virtual int Position { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionIcon"/>.
        /// </summary>
        public CompletionIcon()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CompletionIcon"/> with the given image, automation values, and position.
        /// </summary>
        /// <param name="imageSource">The icon to describe the completion item.</param>
        /// <param name="automationName">The automation name for the icon.</param>
        /// <param name="automationId">The automation id for the icon.</param>
        /// <param name="position">The display position of the icon. If no value is provided this will be zero.</param>
        public CompletionIcon(IImage imageSource, string automationName, string automationId, int position=0)
        {
            this.IconSource = imageSource ?? throw new ArgumentNullException(nameof(imageSource));
            this.AutomationName = automationName;
            this.AutomationId = automationId;
            this.Position = position;
        }

        public int CompareTo(CompletionIcon obj)
        {
            // Sort CompletionIcons by position.
            int x = this.Position.CompareTo(obj.Position);

            if (x == 0 && this.AutomationName != null && obj.AutomationName != null)
            {
                x = string.Compare(this.AutomationName, obj.AutomationName, StringComparison.Ordinal);
            }

            return x;
        }
    }
}
