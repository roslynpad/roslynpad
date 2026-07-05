// Copyright (c) Microsoft Corporation
// All rights reserved

namespace Microsoft.VisualStudio.Language.Intellisense
{
    /// <summary>
    /// Represents an interactive Quick Info content. This interface can be used to add an interactive content such as hyperlinks to
    /// the Quick Info popup.
    /// If any object implementing this interface is provided to
    /// <see cref="IAsyncQuickInfoSource"/> via <see cref="IAsyncQuickInfoSource.GetQuickInfoItemAsync(IAsyncQuickInfoSession, System.Threading.CancellationToken,)"/>,
    /// the Quick Info presenter will allow to interact with this content, particulartly it will keep Quick Info popup open when mouse 
    /// is over it and will allow this content to recieve mouse events.
    /// </summary>
    public interface IInteractiveQuickInfoContent
    {
        /// <summary>
        /// Gets whether the interactive Quick Info content wants to keep current Quick Info session open. Until this property is true, 
        /// the <see cref="IAsyncQuickInfoSession"/> containing this content won't be dismissed even if mouse is moved somewhere else.
        /// This is useful in very rare scenarios when an interactive Quick Info content handles all input interaction, while needs to 
        /// keep this <see cref="IAsyncQuickInfoSession"/> open (the only known example so far is LightBulb in its expanded state hosted in 
        /// Quick Info).
        /// </summary>
        bool KeepQuickInfoOpen { get; }

        /// <summary>
        /// Gets a value indicating whether the mouse pointer is located over this interactive Quick Info content, 
        /// including any parts that are out of the Quick Info visual tree (such as popups).
        /// </summary>
        bool IsMouseOverAggregated { get; }
    }
}
