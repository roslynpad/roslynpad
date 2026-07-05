//
//  Copyright (c) Microsoft Corporation. All rights reserved.
//  Licensed under the MIT License. See License.txt in the project root for license information.
//
using System;
using System.Composition;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods
{
    /// <summary>
    /// Provides methods for <see cref="ITextView"/>-related options.
    /// </summary>
    public static class TextViewOptionExtensions
    {
        #region Extension methods

        /// <summary>
        /// Determines whether virtual space is enabled for the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if virtual space is enabled, otherwise <c>false</c>.</returns>
        public static bool IsVirtualSpaceEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.UseVirtualSpaceId);
        }

        /// <summary>
        /// Determines whether overwrite mode is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if overwrite mode is enabled, otherwise <c>false</c>.</returns>
        public static bool IsOverwriteModeEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.OverwriteModeId);
        }

        /// <summary>
        /// Determines whether auto-scroll is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if auto-scroll is enabled, otherwise <c>false</c>.</returns>
        public static bool IsAutoScrollEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.AutoScrollId);
        }

        /// <summary>
        /// Gets the set of word wrap styles with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns>The <see cref="WordWrapStyles"/> of the set of editor options.</returns>
        public static WordWrapStyles WordWrapStyle(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<WordWrapStyles>(DefaultTextViewOptions.WordWrapStyleId);
        }

        /// <summary>
        /// Determines whether visible whitespace is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if visible whitespace is enabled, otherwise <c>false</c>.</returns>
        public static bool IsVisibleWhitespaceEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.UseVisibleWhitespaceId);
        }

        public static bool IsVisibleWhitespaceOnlyWhenSelectedEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedId);
        }

        public static DefaultTextViewOptions.IncludeWhitespaces VisibleWhitespaceEnabledTypes(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<DefaultTextViewOptions.IncludeWhitespaces>(DefaultTextViewOptions.UseVisibleWhitespaceIncludeId);
        }

        /// <summary>
        /// Determines whether the view prohibits all user input.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns>if <c>true</c> then all user input to the view is prohibited.</returns>
        /// <remarks>The view's underlying buffer can still be modified even if this option is set.</remarks>
        public static bool DoesViewProhibitUserInput(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.ViewProhibitUserInputId);
        }

        /// <summary>
        /// Determines whether the option for outlining undo enabled in the specified <see cref="IEditorOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/>.</param>
        /// <returns><c>true</c> if the option is enabled, otherwise <c>false</c>.</returns>
        public static bool IsOutliningUndoEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultTextViewOptions.OutliningUndoOptionId);
        }

        /// <summary>
        /// Determines whether the option for drag/drop editing is enabled in the specified <see cref="IEditorOptions"/>.
        /// </summary>
        /// <param name="options">The <see cref="IEditorOptions"/> used to look up the option value.</param>
        /// <returns><c>true</c> if the drag/drop editing option is enabled, <c>false</c> otherwise.</returns>
        public static bool IsDragDropEditingEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultTextViewOptions.DragDropEditingId);
        }

        /// <summary>
        /// Determines whether the view's ViewportLeft property is clipped to the text width.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if ViewportLeft is clipped, otherwise <c>false</c>.</returns>
        public static bool IsViewportLeftClipped(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewOptions.IsViewportLeftClippedId);
        }

        /// <summary>
        /// Determines if the caret should be moved to the end of the selection after performing the "select all" operation.
        /// </summary>
        public static bool ShouldMoveCaretOnSelectAll(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue(DefaultTextViewOptions.ShouldMoveCaretOnSelectAllId);
        }
        #endregion
    }

    /// <summary>
    /// Provides methods for <see cref="ITextView"/> host related options.
    /// </summary>
    public static class TextViewHostOptionExtensions
    {
        #region Extension methods

        /// <summary>
        /// Determines whether the vertical scrollbar is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the vertical scrollbar is enabled, otherwise <c>false</c>.</returns>
        public static bool IsVerticalScrollBarEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.VerticalScrollBarId);
        }

        /// <summary>
        /// Determines whether the horizontal scrollbar is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the horizontal scrollbar is enabled, otherwise <c>false</c>.</returns>
        public static bool IsHorizontalScrollBarEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.HorizontalScrollBarId);
        }

        /// <summary>
        ///  Determines whether the glyph margin is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the glyph margin is enabled, otherwise <c>false</c>.</returns>
        public static bool IsGlyphMarginEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.GlyphMarginId);
        }

        /// <summary>
        /// Determines whether the selection margin is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the selection margin is enabled, otherwise <c>false</c>.</returns>
        public static bool IsSelectionMarginEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.SelectionMarginId);
        }

        /// <summary>
        /// Determines whether the line number margin is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the line number margin is enabled, otherwise <c>false</c>.</returns>
        public static bool IsLineNumberMarginEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.LineNumberMarginId);
        }

        /// <summary>
        /// Determines whether change tracking is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if change tracking is enabled, otherwise <c>false</c>.</returns>
        public static bool IsChangeTrackingEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.ChangeTrackingId);
        }

        /// <summary>
        ///  Determines whether the Outlining margin is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the Outlining margin is enabled, otherwise <c>false</c>.</returns>
        /// <remarks>Disabling the margin does NOT turn off Outlining (it just hides the margin</remarks>
        public static bool IsOutliningMarginEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.OutliningMarginId);
        }

        /// <summary>
        /// Determines whether the zoom control is enabled with the specified set of editor options.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the zoom control is enabled, otherwise <c>false</c>.</returns>
        public static bool IsZoomControlEnabled(this IEditorOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.ZoomControlId);
        }

        /// <summary>
        /// Determines whether the editor is in either "Extra Contrast" or "High Contrast" modes.
        /// </summary>
        /// <param name="options">The set of editor options.</param>
        /// <returns><c>true</c> if the editor is in either "Extra Contrast" or "High Contrast" modes, otherwise <c>false</c>.</returns>
        public static bool IsInContrastMode(this IEditorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            return options.GetOptionValue<bool>(DefaultTextViewHostOptions.IsInContrastModeId);
        }
        #endregion
    }
}

namespace Microsoft.VisualStudio.Text.Editor
{
    /// <summary>
    /// Defines common <see cref="ITextView"/> options.
    /// </summary>
    public static class DefaultTextViewOptions
    {
        #region Option identifiers

        /// <summary>
        /// Determines whether cut and copy causes a blank line to be cut or copied when the selection is empty.
        /// </summary>
        public static readonly EditorOptionKey<bool> CutOrCopyBlankLineIfNoSelectionId = new EditorOptionKey<bool>(CutOrCopyBlankLineIfNoSelectionName);
        public const string CutOrCopyBlankLineIfNoSelectionName = "TextView/CutOrCopyBlankLineIfNoSelection";

        /// <summary>
        /// Determines whether to prohibit user input. The text in the view's
        /// buffer can still be modified, and other views on the same buffer may allow user input.
        /// </summary>
        public static readonly EditorOptionKey<bool> ViewProhibitUserInputId = new EditorOptionKey<bool>(ViewProhibitUserInputName);
        public const string ViewProhibitUserInputName = "TextView/ProhibitUserInput";

        /// <summary>
        /// Gets the word wrap style for the underlying view.
        /// </summary>
        /// <remarks>Turning word wrap on will always hide the host's horizontal scroll bar. Turning word wrap off
        /// will always expose the host's horizontal scroll bar.</remarks>
        public static readonly EditorOptionKey<WordWrapStyles> WordWrapStyleId = new EditorOptionKey<WordWrapStyles>(WordWrapStyleName);
        public const string WordWrapStyleName = "TextView/WordWrapStyle";

        /// <summary>
        /// Determines whether to enable virtual space in the view.
        /// </summary>
        public static readonly EditorOptionKey<bool> UseVirtualSpaceId = new EditorOptionKey<bool>(UseVirtualSpaceName);
        public const string UseVirtualSpaceName = "TextView/UseVirtualSpace";

        /// <summary>
        /// Determines whether the view's ViewportLeft property is clipped to the text width.
        /// </summary>
        public static readonly EditorOptionKey<bool> IsViewportLeftClippedId = new EditorOptionKey<bool>(IsViewportLeftClippedName);
        public const string IsViewportLeftClippedName = "TextView/IsViewportLeftClipped";

        /// <summary>
        /// Determines whether overwrite mode is enabled.
        /// </summary>
        public static readonly EditorOptionKey<bool> OverwriteModeId = new EditorOptionKey<bool>(OverwriteModeName);
        public const string OverwriteModeName = "TextView/OverwriteMode";

        /// <summary>
        /// Determines whether the view should auto-scroll on text changes.
        /// </summary>
        /// <remarks>
        /// If this option is enabled, whenever a text change occurs and the caret is on the last line,
        /// the view will be scrolled to make the caret visible.
        /// </remarks>
        public static readonly EditorOptionKey<bool> AutoScrollId = new EditorOptionKey<bool>(AutoScrollName);
        public const string AutoScrollName = "TextView/AutoScroll";

        /// <summary>
        /// Determines whether to show spaces and tabs as visible glyphs.
        /// </summary>
        public static readonly EditorOptionKey<bool> UseVisibleWhitespaceId = new EditorOptionKey<bool>(UseVisibleWhitespaceName);
        public const string UseVisibleWhitespaceName = "TextView/UseVisibleWhitespace";

        /// <summary>
        /// Determines whether to show spaces, tabs and EndOfLine as visible glyphs only under selection.
        /// </summary>
        public static readonly EditorOptionKey<bool> UseVisibleWhitespaceOnlyWhenSelectedId = new EditorOptionKey<bool>(UseVisibleWhitespaceOnlyWhenSelectedName);
        public const string UseVisibleWhitespaceOnlyWhenSelectedName = "TextView/UseVisibleWhitespace/OnlyWhenSelected";

        /// <summary>
        /// Determines whether to show spaces, tabs and EndOfLine as visible glyphs only for specific type of whitespace.
        /// </summary>
        public static readonly EditorOptionKey<IncludeWhitespaces> UseVisibleWhitespaceIncludeId = new EditorOptionKey<IncludeWhitespaces>(UseVisibleWhitespaceIncludeName);
        public const string UseVisibleWhitespaceIncludeName = "TextView/UseVisibleWhitespace/Include";

        [Flags]
        public enum IncludeWhitespaces
        {
            None = 0x0,
            Spaces = 0x1,
            Tabs = 0x2,
            LineEndings = 0x4,
            Ideographics = 0x8,
            All = 0xf,
        }

        /// <summary>
        /// Enables or disables the code block structure visualizer text adornment feature.
        /// </summary>
        public static readonly EditorOptionKey<bool> ShowBlockStructureId = new EditorOptionKey<bool>(ShowBlockStructureName);
        public const string ShowBlockStructureName = "TextView/ShowBlockStructure";

        /// <summary>
        /// Should the carets be rendered.
        /// </summary>
        public static readonly EditorOptionKey<bool> ShouldCaretsBeRenderedId = new EditorOptionKey<bool>(ShouldCaretsBeRenderedName);
        public const string ShouldCaretsBeRenderedName = "TextView/ShouldCaretsBeRendered";

        /// <summary>
        /// Should the selections be rendered.
        /// </summary>
        public static readonly EditorOptionKey<bool> ShouldSelectionsBeRenderedId = new EditorOptionKey<bool>(ShouldSelectionsBeRenderedName);
        public const string ShouldSelectionsBeRenderedName = "TextView/ShouldSelectionsBeRendered";

        /// <summary>
        /// Whether or not to replace the coding characters and special symbols (such as (,),{,},etc.) with their textual representation
        /// for automated objects to produce friendly text for screen readers.
        /// </summary>
        public static readonly EditorOptionKey<bool> ProduceScreenReaderFriendlyTextId = new EditorOptionKey<bool>(ProduceScreenReaderFriendlyTextName);
        public const string ProduceScreenReaderFriendlyTextName = "TextView/ProduceScreenReaderFriendlyText";

        /// <summary>
        /// The default option that determines whether outlining is undoable.
        /// </summary>
        public static readonly EditorOptionKey<bool> OutliningUndoOptionId = new EditorOptionKey<bool>(OutliningUndoOptionName);
        public const string OutliningUndoOptionName = "TextView/OutliningUndo";

        /// <summary>
        /// Determines whether URLs should be displayed as hyperlinks.
        /// </summary>
        public static readonly EditorOptionKey<bool> DisplayUrlsAsHyperlinksId = new EditorOptionKey<bool>(DisplayUrlsAsHyperlinksName);
        public const string DisplayUrlsAsHyperlinksName = "TextView/DisplayUrlsAsHyperlinks";

        /// <summary>
        /// The default option that determines whether drag/drop editing is enabled.
        /// </summary>
        public static readonly EditorOptionKey<bool> DragDropEditingId = new EditorOptionKey<bool>(DragDropEditingName);
        public const string DragDropEditingName = "TextView/DragDrop";

        /// <summary>
        /// Determines if automatic brace completion is enabled.
        /// </summary>
        public const string BraceCompletionEnabledOptionName = "BraceCompletion/Enabled";
        public readonly static EditorOptionKey<bool> BraceCompletionEnabledOptionId = new EditorOptionKey<bool>(BraceCompletionEnabledOptionName);

        /// <summary>
        /// Defines how wide the caret should be rendered. This is typically used to support accessibility requirements.
        /// </summary>
        public const string CaretWidthOptionName = "TextView/CaretWidth";
        public readonly static EditorOptionKey<double> CaretWidthId = new EditorOptionKey<double>(CaretWidthOptionName);

        /// <summary>
        /// Determines whether to enable the highlight current line adornment.
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableHighlightCurrentLineId = new EditorOptionKey<bool>(EnableHighlightCurrentLineName);
        public const string EnableHighlightCurrentLineName = "Adornments/HighlightCurrentLine/Enable";

        /// <summary>
        /// Determines whether to enable the highlight current line adornment.
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableSimpleGraphicsId = new EditorOptionKey<bool>(EnableSimpleGraphicsName);
        public const string EnableSimpleGraphicsName = "Graphics/Simple/Enable";

        /// <summary>
        /// Determines whether the opacity of text markers and selection is reduced in high contrast mode.
        /// </summary>
        public static readonly EditorOptionKey<bool> UseReducedOpacityForHighContrastOptionId = new EditorOptionKey<bool>(UseReducedOpacityForHighContrastOptionName);
        public const string UseReducedOpacityForHighContrastOptionName = "UseReducedOpacityForHighContrast";

        /// <summary>
        /// Determines whether to enable mouse wheel zooming
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableMouseWheelZoomId = new EditorOptionKey<bool>(EnableMouseWheelZoomName);
        public const string EnableMouseWheelZoomName = "TextView/MouseWheelZoom";

        /// <summary>
        /// Determines the appearance category of a view, which selects a ClassificationFormatMap and EditorFormatMap.
        /// </summary>
        public static readonly EditorOptionKey<string> AppearanceCategory = new EditorOptionKey<string>(AppearanceCategoryName);
        public const string AppearanceCategoryName = "Appearance/Category";

        /// <summary>
        /// Determines the view zoom level.
        /// </summary>
        public static readonly EditorOptionKey<double> ZoomLevelId = new EditorOptionKey<double>(ZoomLevelName);
        public const string ZoomLevelName = "TextView/ZoomLevel";

        /// <summary>
        /// Determines the minimum view zoom level.
        /// </summary>
        public static readonly EditorOptionKey<double> MinZoomLevelId = new EditorOptionKey<double>(MinZoomLevelName);
        public const string MinZoomLevelName = "TextView/MinZoomLevel";

        /// <summary>
        /// Determines the maximum view zoom level.
        /// </summary>
        public static readonly EditorOptionKey<double> MaxZoomLevelId = new EditorOptionKey<double>(MaxZoomLevelName);
        public const string MaxZoomLevelName = "TextView/MaxZoomLevel";

        /// <summary>
        /// Determines whether to enable mouse click + modifier keypress for go to definition.
        /// </summary>
        public const string ClickGoToDefEnabledName = "TextView/ClickGoToDefEnabled";
        public static readonly EditorOptionKey<bool> ClickGoToDefEnabledId = new EditorOptionKey<bool>(ClickGoToDefEnabledName);

        /// <summary>
        /// Determines whether to open definition target in Peek view for mouse click + modifier keypress.
        /// </summary>
        public const string ClickGoToDefOpensPeekName = "TextView/ClickGoToDefOpensPeek";
        public static readonly EditorOptionKey<bool> ClickGoToDefOpensPeekId = new EditorOptionKey<bool>(ClickGoToDefOpensPeekName);

        /// <summary>
        /// The default option that determines whether to move the caret when performing the "select all" operation.
        /// </summary>
        public static readonly EditorOptionKey<bool> ShouldMoveCaretOnSelectAllId = new EditorOptionKey<bool>(ShouldMoveCaretOnSelectAllName);
        public const string ShouldMoveCaretOnSelectAllName = "TextView/ShouldMoveCaretOnSelectAll";

        /// <summary>
        /// Defines where vertical rulers, if any, are to be drawn in the editor.
        /// </summary>
        public const string VerticalRulersName = "TextView/VerticalRulers";
        public static readonly EditorOptionKey<int[]> VerticalRulersId = new EditorOptionKey<int[]>(VerticalRulersName);
        #endregion
    }

    /// <summary>
    /// Names of common <see cref="ITextView"/> host-related options.
    /// </summary>
    public static class DefaultTextViewHostOptions
    {
        #region Option identifiers

        /// <summary>
        /// Determines whether to have a vertical scroll bar.
        /// </summary>
        public static readonly EditorOptionKey<bool> VerticalScrollBarId = new EditorOptionKey<bool>(VerticalScrollBarName);
        public const string VerticalScrollBarName = "TextViewHost/VerticalScrollBar";

        /// <summary>
        /// Determines whether to have a horizontal scroll bar.
        /// </summary>
        public static readonly EditorOptionKey<bool> HorizontalScrollBarId = new EditorOptionKey<bool>(HorizontalScrollBarName);
        public const string HorizontalScrollBarName = "TextViewHost/HorizontalScrollBar";

        /// <summary>
        /// Determines whether to have a glyph margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> GlyphMarginId = new EditorOptionKey<bool>(GlyphMarginName);
        public const string GlyphMarginName = "TextViewHost/GlyphMargin";

        /// <summary>
        /// Determines whether to have a suggestion margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> SuggestionMarginId = new EditorOptionKey<bool>(SuggestionMarginName);
        public const string SuggestionMarginName = "TextViewHost/SuggestionMargin";

        /// <summary>
        /// Determines whether to have a selection margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> SelectionMarginId = new EditorOptionKey<bool>(SelectionMarginName);
        public const string SelectionMarginName = "TextViewHost/SelectionMargin";

        /// <summary>
        /// Determines whether to have a line number margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> LineNumberMarginId = new EditorOptionKey<bool>(LineNumberMarginName);
        public const string LineNumberMarginName = "TextViewHost/LineNumberMargin";

        /// <summary>
        /// Determines whether to have the change tracking margin.
        /// </summary>
        /// <remarks>The change tracking margins will "reset" (lose the change history) when this option is turned off.
        /// If it is turned back on, it will track changes from the time the margin is turned on.</remarks>
        public static readonly EditorOptionKey<bool> ChangeTrackingId = new EditorOptionKey<bool>(ChangeTrackingName);
        public const string ChangeTrackingName = "TextViewHost/ChangeTracking";

        /// <summary>
        /// Determines whether to have an outlining margin.
        /// </summary>
        public static readonly EditorOptionKey<bool> OutliningMarginId = new EditorOptionKey<bool>(OutliningMarginName);
        public const string OutliningMarginName = "TextViewHost/OutliningMargin";

        /// <summary>
        /// Determines whether to have a zoom control.
        /// </summary>
        public static readonly EditorOptionKey<bool> ZoomControlId = new EditorOptionKey<bool>(ZoomControlName);
        public const string ZoomControlName = "TextViewHost/ZoomControl";

        /// <summary>
        /// Determines whether the editor is in either "Extra Contrast" or "High Contrast" modes.
        /// </summary>
        public static readonly EditorOptionKey<bool> IsInContrastModeId = new EditorOptionKey<bool>(IsInContrastModeName);
        public const string IsInContrastModeName = "TextViewHost/IsInContrastMode";

        /// <summary>
        /// Determines whether any annotations are shown over the vertical scroll bar.
        /// </summary>
        public const string ShowScrollBarAnnotationsOptionName = "OverviewMargin/ShowScrollBarAnnotationsOption";
        public readonly static EditorOptionKey<bool> ShowScrollBarAnnotationsOptionId = new EditorOptionKey<bool>(ShowScrollBarAnnotationsOptionName);

        /// <summary>
        /// Determines whether the vertical scroll bar is shown as a standard WPF scroll bar or the new enhanced scroll bar.
        /// </summary>
        public const string ShowEnhancedScrollBarOptionName = "OverviewMargin/ShowEnhancedScrollBar";
        public readonly static EditorOptionKey<bool> ShowEnhancedScrollBarOptionId = new EditorOptionKey<bool>(ShowEnhancedScrollBarOptionName);

        /// <summary>
        /// Determines whether changes are shown over the vertical scroll bar.
        /// </summary>
        public const string ShowChangeTrackingMarginOptionName = "OverviewMargin/ShowChangeTracking";
        public readonly static EditorOptionKey<bool> ShowChangeTrackingMarginOptionId = new EditorOptionKey<bool>(ShowChangeTrackingMarginOptionName);

        /// <summary>
        /// Determines the width of the change tracking margin.
        /// </summary>
        public const string ChangeTrackingMarginWidthOptionName = "OverviewMargin/ChangeTrackingWidth";
        public readonly static EditorOptionKey<double> ChangeTrackingMarginWidthOptionId = new EditorOptionKey<double>(ChangeTrackingMarginWidthOptionName);

        /// <summary>
        /// Determines whether a preview tip is shown when the mouse moves over the vertical scroll bar.
        /// </summary>
        public const string ShowPreviewOptionName = "OverviewMargin/ShowPreview";
        public readonly static EditorOptionKey<bool> ShowPreviewOptionId = new EditorOptionKey<bool>(ShowPreviewOptionName);

        /// <summary>
        /// Determines the size (in lines of text) of the default tip.
        /// </summary>
        public const string PreviewSizeOptionName = "OverviewMargin/PreviewSize";
        public readonly static EditorOptionKey<int> PreviewSizeOptionId = new EditorOptionKey<int>(PreviewSizeOptionName);

        /// <summary>
        /// Determines whether the vertical margin shows the location of the caret.
        /// </summary>
        public const string ShowCaretPositionOptionName = "OverviewMargin/ShowCaretPosition";
        public readonly static EditorOptionKey<bool> ShowCaretPositionOptionId = new EditorOptionKey<bool>(ShowCaretPositionOptionName);

        /// <summary>
        /// Determines whether the source image margin is displayed.
        /// </summary>
        /// <remarks>
        /// This margin is only shown if this option and the ShowEnhancedScrollBarOption, and the SourceImageMarginWidth is >= 25.0.
        /// </remarks>
        public const string SourceImageMarginEnabledOptionName = "OverviewMargin/ShowSourceImageMargin";
        public readonly static EditorOptionKey<bool> SourceImageMarginEnabledOptionId = new EditorOptionKey<bool>(SourceImageMarginEnabledOptionName);

        /// <summary>
        /// Determines the width of the source image margin.
        /// </summary>
        public const string SourceImageMarginWidthOptionName = "OverviewMargin/SourceImageMarginWidth";
        public readonly static EditorOptionKey<double> SourceImageMarginWidthOptionId = new EditorOptionKey<double>(SourceImageMarginWidthOptionName);

        /// <summary>
        /// Determines whether marks (bookmarks, breakpoints, etc.) are shown over the vertical scroll bar.
        /// </summary>
        public const string ShowMarksOptionName = "OverviewMargin/ShowMarks";
        public readonly static EditorOptionKey<bool> ShowMarksOptionId = new EditorOptionKey<bool>(ShowMarksOptionName);

        /// <summary>
        /// Determines whether errors are shown over the vertical scroll bar.
        /// </summary>
        public const string ShowErrorsOptionName = "OverviewMargin/ShowErrors";
        public readonly static EditorOptionKey<bool> ShowErrorsOptionId = new EditorOptionKey<bool>(ShowErrorsOptionName);

        /// <summary>
        /// Determines the width of the marks margin.
        /// </summary>
        public const string MarkMarginWidthOptionName = "OverviewMargin/MarkMarginWidth";
        public readonly static EditorOptionKey<double> MarkMarginWidthOptionId = new EditorOptionKey<double>(MarkMarginWidthOptionName);

        /// <summary>
        /// Determines the width of the error margin.
        /// </summary>
        public const string ErrorMarginWidthOptionName = "OverviewMargin/ErrorMarginWidth";
        public readonly static EditorOptionKey<double> ErrorMarginWidthOptionId = new EditorOptionKey<double>(ErrorMarginWidthOptionName);

        /// <summary>
        /// Determines whether to have a file health indicator.
        /// </summary>
        public static readonly EditorOptionKey<bool> EnableFileHealthIndicatorOptionId = new EditorOptionKey<bool>(EnableFileHealthIndicatorOptionName);
        public const string EnableFileHealthIndicatorOptionName = "TextViewHost/FileHealthIndicator";

        #endregion
    }

    /// <summary>
    /// Defines the view option for drag/drop editing.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.DragDropEditingName)]
    [Shared]
    public sealed class DragDropEditing : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default key for the drag/drop editing option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.DragDropEditingId; } }
    }

    /// <summary>
    /// Defines the view option for overwrite mode.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.OverwriteModeName)]
    [Shared]
    public sealed class OverwriteMode : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.OverwriteModeId; } }
    }

    /// <summary>
    /// Defines the Use Virtual Space option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.UseVirtualSpaceName)]
    [Shared]
    public sealed class UseVirtualSpace : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.UseVirtualSpaceId; } }
    }

    /// <summary>
    /// Defines the Use Virtual Space option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.IsViewportLeftClippedName)]
    [Shared]
    public sealed class IsViewportLeftClipped : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.IsViewportLeftClippedId; } }
    }

    /// <summary>
    /// Defines the Prohibit User Input option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ViewProhibitUserInputName)]
    [Shared]
    public sealed class ViewProhibitUserInput : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// GGets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.ViewProhibitUserInputId; } }
    }

    /// <summary>
    /// Defines the option to cut or copy a blank line if the selection is empty.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionName)]
    [Shared]
    public sealed class CutOrCopyBlankLineIfNoSelection : ViewOptionDefinition<bool>
    {

        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.CutOrCopyBlankLineIfNoSelectionId; } }
    }

    /// <summary>
    /// Defines the word wrap style option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.WordWrapStyleName)]
    [Shared]
    public sealed class WordWrapStyle : ViewOptionDefinition<WordWrapStyles>
    {
        /// <summary>
        /// Gets the default value, which is <c>WordWrapStyles.None</c>.
        /// </summary>
        public override WordWrapStyles Default { get { return WordWrapStyles.AutoIndent; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<WordWrapStyles> Key { get { return DefaultTextViewOptions.WordWrapStyleId; } }
    }

    /// <summary>
    /// Defines the Use Visible Whitespace option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.UseVisibleWhitespaceName)]
    [Shared]
    public sealed class UseVisibleWhitespace : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.UseVisibleWhitespaceId; } }
    }

    /// <summary>
    /// Defines the Use Visible Whitespace option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedName)]
    [Shared]
    public sealed class UseVisibleWhitespaceOnlyWhenSelected : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedId; } }
    }

    /// <summary>
    /// Defines the Use Visible Whitespace option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.UseVisibleWhitespaceIncludeName)]
    [Shared]
    public sealed class UseVisibleWhitespaceEnabledTypes : ViewOptionDefinition<DefaultTextViewOptions.IncludeWhitespaces>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override DefaultTextViewOptions.IncludeWhitespaces Default { get { return DefaultTextViewOptions.IncludeWhitespaces.All; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<DefaultTextViewOptions.IncludeWhitespaces> Key { get { return DefaultTextViewOptions.UseVisibleWhitespaceIncludeId; } }
    }

    /// <summary>
    /// Defines the Show Block Structure option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ShowBlockStructureName)]
    [Shared]
    public sealed class ShowBlockStructure : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.ShowBlockStructureId; } }
    }

    /// <summary>
    /// Defines the Should Carets Be Rendered option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ShouldCaretsBeRenderedName)]
    [Shared]
    public sealed class ShouldCaretsBeRendered : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.ShouldCaretsBeRenderedId; } }
    }

    /// <summary>
    /// Defines the Should Selection Be Rendered option.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ShouldSelectionsBeRenderedName)]
    [Shared]
    public sealed class ShouldSelectionsBeRendered : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.ShouldSelectionsBeRenderedId; } }
    }

    /// <summary>
    /// Defines the option to enable providing annotated text in automation controls so that screen readers can properly
    /// read contents of code.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ProduceScreenReaderFriendlyTextName)]
    [Shared]
    public sealed class ProduceScreenReaderFriendlyText : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.ProduceScreenReaderFriendlyTextId; } }
    }

    /// <summary>
    /// Defines the option to enable the vertical scroll bar.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.VerticalScrollBarName)]
    [Shared]
    public sealed class VerticalScrollBarEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.VerticalScrollBarId; } }
    }

    /// <summary>
    /// Defines the option to enable the horizontal scroll bar.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.HorizontalScrollBarName)]
    [Shared]
    public sealed class HorizontalScrollBarEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.HorizontalScrollBarId; } }
    }

    /// <summary>
    /// Defines the option to enable the glyph margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.GlyphMarginName)]
    [Shared]
    public sealed class GlyphMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.GlyphMarginId; } }
    }

    /// <summary>
    /// Defines the option to enable the suggestion margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.SuggestionMarginName)]
    [Shared]
    public sealed class SuggestionMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.SuggestionMarginId; } }
    }

    /// <summary>
    /// Defines the option to enable the selection margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.SelectionMarginName)]
    [Shared]
    public sealed class SelectionMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.SelectionMarginId; } }
    }

    /// <summary>
    /// Defines the option to enable the line number margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.LineNumberMarginName)]
    [Shared]
    public sealed class LineNumberMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.LineNumberMarginId; } }
    }

    /// <summary>
    /// Defines the option to enable auto-scroll.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.AutoScrollName)]
    [Shared]
    public sealed class AutoScrollEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.AutoScrollId; } }
    }

    /// <summary>
    /// Defines the option to enable the change-tracking margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.ChangeTrackingName)]
    [Shared]
    public sealed class ChangeTrackingMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>false</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.ChangeTrackingId; } }
    }

    /// <summary>
    /// Defines the option to enable the Outlining margin.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.OutliningMarginName)]
    [Shared]
    public sealed class OutliningMarginEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.OutliningMarginId; } }
    }

    /// <summary>
    /// The option definition that determines whether outlining is undoable.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.OutliningUndoOptionName)]
    [Shared]
    public sealed class OutliningUndoEnabled : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (<c>true</c>)>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.OutliningUndoOptionId; } }
    }


    /// <summary>
    /// Defines the option to enable the Zoom Control.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.ZoomControlName)]
    [Shared]
    public sealed class ZoomControlEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.ZoomControlId; } }
    }

    /// <summary>
    /// Determines whether the editor is in either "Extra Contrast" or "High Contrast" modes.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.IsInContrastModeName)]
    [Shared]
    public sealed class IsInContrastModeOption : EditorOptionDefinition<bool>
    {
        public override bool Default => false;
        public override EditorOptionKey<bool> Key => DefaultTextViewHostOptions.IsInContrastModeId;
    }

    /// <summary>
    /// The option definition that determines if URLs should be displayed as hyperlinks.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.DisplayUrlsAsHyperlinksName)]
    [Shared]
    public sealed class DisplayUrlsAsHyperlinks : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (<c>true</c>)>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.DisplayUrlsAsHyperlinksId; } }
    }

    /// <summary>
    /// The option definition that determines how wide the caret should be rendered.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.CaretWidthOptionName)]
    [Shared]
    public sealed class CaretWidthOption : EditorOptionDefinition<double>
    {
        /// <summary>
        /// Gets the default value <c>1.0</c>.
        /// </summary>
        public override double Default => 1.0;

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<double> Key => DefaultTextViewOptions.CaretWidthId;
    }

    /// <summary>
    /// Defines the option to enable the File Health Indicator.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewHostOptions.EnableFileHealthIndicatorOptionName)]
    [Shared]
    public sealed class FileHealthIndicatorEnabled : ViewOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the default text view host value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewHostOptions.EnableFileHealthIndicatorOptionId; } }
    }


    /// <summary>
    /// Represents the option to highlight the current line.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.EnableHighlightCurrentLineName)]
    [Shared]
    public sealed class HighlightCurrentLineOption : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the key for the highlight current line option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.EnableHighlightCurrentLineId; } }
    }

    /// <summary>
    /// Represents the option to draw a selection gradient as opposed to a solid color selection.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.EnableSimpleGraphicsName)]
    [Shared]
    public sealed class SimpleGraphicsOption : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the key for the simple graphics option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.EnableSimpleGraphicsId; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.UseReducedOpacityForHighContrastOptionName)]
    [Shared]
    public sealed class UseReducedOpacityForHighContrastOption : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default { get { return false; } }

        /// <summary>
        /// Gets the key for the use reduced opacity option.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.UseReducedOpacityForHighContrastOptionId; } }
    }

    /// <summary>
    /// Defines the option to enable the mouse wheel zoom
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.EnableMouseWheelZoomName)]
    [Shared]
    public sealed class MouseWheelZoomEnabled : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value, which is <c>true</c>.
        /// </summary>
        public override bool Default { get { return true; } }

        /// <summary>
        /// Gets the wpf text view  value.
        /// </summary>
        public override EditorOptionKey<bool> Key { get { return DefaultTextViewOptions.EnableMouseWheelZoomId; } }
    }

    /// <summary>
    /// Defines the appearance category.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.AppearanceCategoryName)]
    [Shared]
    public sealed class AppearanceCategoryOption : EditorOptionDefinition<string>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override string Default { get { return "text"; } }

        /// <summary>
        /// Gets the key for the appearance category option.
        /// </summary>
        public override EditorOptionKey<string> Key { get { return DefaultTextViewOptions.AppearanceCategory; } }
    }

    /// <summary>
    /// Defines the zoomlevel.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ZoomLevelName)]
    [Shared]
    public sealed class ZoomLevel : EditorOptionDefinition<double>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override double Default { get { return (int)ZoomConstants.DefaultZoom; } }

        /// <summary>
        /// Gets the key for the text view zoom level.
        /// </summary>
        public override EditorOptionKey<double> Key { get { return DefaultTextViewOptions.ZoomLevelId; } }
    }

    /// <summary>
    /// Defines the minimum zoomlevel.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.MinZoomLevelName)]
    [Shared]
    public sealed class MinZoomLevel : EditorOptionDefinition<double>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override double Default => ZoomConstants.MinZoom;

        /// <summary>
        /// Gets the key for the text view zoom level.
        /// </summary>
        public override EditorOptionKey<double> Key => DefaultTextViewOptions.MinZoomLevelId;
    }

    /// <summary>
    /// Defines the maximum zoomlevel.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.MaxZoomLevelName)]
    [Shared]
    public sealed class MaxZoomLevel : EditorOptionDefinition<double>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override double Default => ZoomConstants.MaxZoom;

        /// <summary>
        /// Gets the key for the text view zoom level.
        /// </summary>
        public override EditorOptionKey<double> Key => DefaultTextViewOptions.MaxZoomLevelId;
    }

    /// <summary>
    /// Determines whether to enable mouse click + modifier keypress for go to definition.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ClickGoToDefEnabledName)]
    [Shared]
    public sealed class ClickGotoDefEnabledOption : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default => true;

        /// <summary>
        /// Gets the key for the option.
        /// </summary>
        public override EditorOptionKey<bool> Key => DefaultTextViewOptions.ClickGoToDefEnabledId;
    }

    /// <summary>
    /// Determines whether to open definition target in Peek view for mouse click + modifier keypress.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ClickGoToDefOpensPeekName)]
    [Shared]
    public sealed class ClickGotoDefOpensPeekOption : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value.
        /// </summary>
        public override bool Default => false;

        /// <summary>
        /// Gets the key for the option.
        /// </summary>
        public override EditorOptionKey<bool> Key => DefaultTextViewOptions.ClickGoToDefOpensPeekId;
    }

    /// <summary>
    /// The option definition that determines if the caret should be moved to the end of the selection after performing the "select all" operation.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.ShouldMoveCaretOnSelectAllName)]
    [Shared]
    public sealed class ShouldMoveCaretOnSelectAll : EditorOptionDefinition<bool>
    {
        /// <summary>
        /// Gets the default value (true).
        /// </summary>
        public override bool Default { get => true; }

        /// <summary>
        /// Gets the editor option key.
        /// </summary>
        public override EditorOptionKey<bool> Key => DefaultTextViewOptions.ShouldMoveCaretOnSelectAllId;
    }

    /// <summary>
    /// Determines whether to display the vertical ruler or not.
    /// </summary>
    [Export(typeof(EditorOptionDefinition))]
    [Name(DefaultTextViewOptions.VerticalRulersName)]
    [Shared]
    public sealed class VerticalRulersOption : EditorOptionDefinition<int[]>
    {
        public override int[] Default => Array.Empty<int>();
        public override EditorOptionKey<int[]> Key => DefaultTextViewOptions.VerticalRulersId;
    }
}