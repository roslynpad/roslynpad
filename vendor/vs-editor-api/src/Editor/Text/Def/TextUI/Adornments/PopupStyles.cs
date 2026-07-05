//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
namespace Microsoft.VisualStudio.Text.Adornments
{
    /// <summary>
    /// Represents the styles associated with pop-up windows.
    /// </summary>
    [System.Flags]
    public enum PopupStyles
    {
        /// <summary>
        /// Sets the default behavior: the pop-up window has no border, is not resizable, is not dismissed when the mouse moves,
        /// </summary>
        None = 0x00,                             
        
        /// <summary>
        /// Dismiss the pop-up window if the mouse leaves the associated text span.  
        /// This setting is mutually exclusive with <see cref="DismissOnMouseLeaveTextOrContent"/>.
        /// </summary>
        DismissOnMouseLeaveText = 0x04,          
        
        /// <summary>
        /// Dismiss the pop-up window if the mouse leaves the associated text span or the pop-up content.  
        /// This setting is mutually exclusive with <see cref="DismissOnMouseLeaveText"/>.
        /// </summary>
        DismissOnMouseLeaveTextOrContent = 0x08, 
        /// <summary>
        /// Try to position the pop-up window to the left or right of the visual span.
        /// </summary>
        PositionLeftOrRight = 0x10,              
        
        /// <summary>
        ///  Try to position the pop-up window to the left or above the visual span.
        /// </summary>
        PreferLeftOrTopPosition = 0x20,         

        /// <summary>
        ///  Align the right or bottom edges of the pop-up window with those of the visual span.
        /// </summary>
        RightOrBottomJustify = 0x40,

        /// <summary>
        /// Use the positioning preference specified, but if the opposite positioning can get the popup
        /// closer to the visual span, use the opposition positioning.
        /// </summary>
        PositionClosest= 0x80
    };
}