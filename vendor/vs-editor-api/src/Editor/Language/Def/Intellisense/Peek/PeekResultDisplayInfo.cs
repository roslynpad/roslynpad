// Copyright (c) Microsoft Corporation
// All rights reserved

using System;
using Avalonia;

namespace Microsoft.VisualStudio.Language.Intellisense
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    /// <summary>
    /// Defines elements of <see cref="IPeekResult"/> display information.
    /// </summary>
    public class PeekResultDisplayInfo : IPeekResultDisplayInfo
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        /// <summary>
        /// Defines the localized label used for displaying this result to the user.
        /// This value will be used to represent <see cref="IPeekResult"/> in the Peek control's result list.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Defines the localized label tooltip used for displaying this result to the user.
        /// </summary>
        /// <remarks>
        /// Supported content types are strings and <see cref="Control" /> instances.
        /// </remarks>
        public object LabelTooltip { get; private set; }

        /// <summary>
        /// Defines the localized title used for displaying this result to the user.
        /// </summary>
        public string Title { get; private set; }

        /// <summary>
        /// Defines the localized title tooltip used for displaying this result to the user.
        /// </summary>
        /// // <remarks>
        /// Supported content types are strings and <see cref="Control" /> instances.
        /// </remarks>
        public object TitleTooltip { get; private set; }

        /// <summary>
        /// Creates new instance of the <see cref="PeekResultDisplayInfo"/> class.
        /// </summary>
        public PeekResultDisplayInfo(string label, object labelTooltip, string title, string titleTooltip)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                throw new ArgumentException(nameof(label) + " cannot be null or white space");
            }
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new ArgumentException(nameof(title) + " cannot be null or white space");
            }

            this.Label = label;
            this.LabelTooltip = labelTooltip;
            this.Title = title;
            this.TitleTooltip = titleTooltip;
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
        /// <summary>
        /// Disposes the <see cref="PeekResultDisplayInfo"/> instance.
        /// </summary>
        public void Dispose()
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            GC.SuppressFinalize(this);
            // Nothing to dispose.
        }
    }
}
