////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Microsoft Corporation.  All rights reserved.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.TextFormatting;

namespace Microsoft.VisualStudio.Language.Intellisense
{
    ///<summary>
    /// Defines a set of properties that will be used to style the default completion presenter.
    ///</summary>
    /// <remarks>
    /// This is a MEF component part, and should be exported with the following attributes:
    /// [Export(typeof(CompletionPresenterStyle))]
    /// [ContentType]
    /// [Name]
    /// [Order]
    /// All exports of this component part should be ordered after the "default" completion presenter style.  At a minimum,
    /// this means adding [Order(After="default")] to the export metadata.
    /// </remarks>
    public class CompletionPresenterStyle
    {
        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of the individual completion items.
        /// </summary>
        /// <remarks>
        /// The individual completion items may override this value by implementing the <see cref="ITextFormattable"/> interface.
        /// </remarks>
        public virtual TextRunProperties CompletionTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the borders in the completion presenter.
        /// </summary>
        public virtual IBrush BorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the background of the completion presenter.
        /// </summary>
        public virtual IBrush BackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the border rectangle around the selected completion item.
        /// </summary>
        public virtual IBrush SelectionBorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the background of the selected completion item.
        /// </summary>
        public virtual IBrush SelectionBackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to paint the foreground of the completion item's suffix text.
        /// </summary>
        public virtual TextRunProperties SuffixTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to paint the text of the selected completion item.
        /// </summary>
        /// <remarks>
        /// This <see cref="TextRunProperties"/> object should be constructed so as to keep from clashing with the
        /// SelectionBackgroundBrush.
        /// </remarks>
        public virtual TextRunProperties SelectionTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the border around the completion tooltip.
        /// </summary>
        public virtual IBrush TooltipBorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the background of the completion tooltip.
        /// </summary>
        public virtual IBrush TooltipBackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text in the completion tooltip.
        /// </summary>
        public virtual TextRunProperties TooltipTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the background of the completion tab panel.
        /// </summary>
        public virtual IBrush TabPanelBackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the border of a completion tab item when the mouse is
        /// hovering over it.
        /// </summary>
        public virtual IBrush TabItemHotBorderBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="IBrush"/> that will be used to paint the background of a completion tab item when the mouse is
        /// hovering over it.
        /// </summary>
        public virtual IBrush TabItemHotBackgroundBrush { get; protected set; }

        /// <summary>
        /// Gets a <see cref="TextRunProperties"/> that will be used to format the text of a completion tab item when the mouse is
        /// hovering over it.
        /// </summary>
        public virtual TextRunProperties TabItemHotTextRunProperties { get; protected set; }

        /// <summary>
        /// Gets a value determining whether or not gradients should be used in the presentation of a
        /// <see cref="ICompletionSession"/>.
        /// </summary>
        public virtual bool? AreGradientsAllowed { get; protected set; }

        /// <summary>
        /// Gets a <see cref="BitmapInterpolationMode"/> value that indicates the desired scaling mode for items' images 
        /// </summary>
        public virtual BitmapInterpolationMode BitmapInterpolationMode { get; protected set; }
    }
}
