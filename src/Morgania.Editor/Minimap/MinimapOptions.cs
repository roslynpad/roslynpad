#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using System.Composition;

using Microsoft.VisualStudio.Utilities;

/// <summary>How tall the minimap draws the document against the height it has.</summary>
public enum MinimapSizing
{
    /// <summary>
    /// Every line takes <see cref="MinimapOptions.PixelsPerLineId"/>; a document taller than the margin scrolls with the
    /// view, so the viewport's band stays in sight.
    /// </summary>
    Proportional,

    /// <summary>As <see cref="Proportional"/>, but a document taller than the margin is scaled down until it fits.</summary>
    Fit,

    /// <summary>The document always spans the margin's height, scaled down or up.</summary>
    Fill,
}

/// <summary>
/// Where a click on the minimap's background puts the view. Either way the caret goes to the clicked code, unless
/// <see cref="MinimapOptions.MoveOnlyId"/> is set.
/// </summary>
public enum MinimapClickTarget
{
    /// <summary>The clicked line comes to the middle of the view.</summary>
    CodePosition,

    /// <summary>The viewport band comes to the pointer, centred on it, as a scroll bar's thumb comes to a click on its track.</summary>
    MousePosition,
}

/// <summary>When a click on the minimap's background jumps there.</summary>
public enum MinimapJumpTrigger
{
    /// <summary>Never: the minimap moves the view only by dragging its viewport band.</summary>
    None,

    /// <summary>When the button goes down; holding it and moving then drags the viewport band.</summary>
    MouseDown,

    /// <summary>When the button comes up without the pointer having dragged.</summary>
    MouseUp,
}

/// <summary>Which side of the text the minimap stands on.</summary>
public enum MinimapAlignment
{
    /// <summary>On the right, before the vertical scroll bar.</summary>
    Right,

    /// <summary>On the left, outside the other left margins.</summary>
    Left,
}

/// <summary>How the minimap draws the characters of a line.</summary>
public enum MinimapRenderStyle
{
    /// <summary>A run of characters in one colour as one solid bar: calm, and readable at any size.</summary>
    Clean,

    /// <summary>Every character as a miniature of its glyph, where the display's resolution allows one.</summary>
    Accurate,
}

/// <summary>
/// The options of the minimap (<see cref="MinimapMarginNames.Minimap"/>): a picture of the whole document beside the text
/// that shows where the view stands and moves it. They are ordinary editor options, so a host sets them globally, on a
/// buffer's options or on one view's (<see cref="IEditorOptions"/>) and gives different editors different minimaps. The
/// colours come from the <see cref="MinimapFormatNames.Name"/> entry of the editor format map; the two colour options here
/// override it where they are set.
/// </summary>
public static class MinimapOptions
{
    /// <summary>
    /// Whether the minimap is shown. On by default. The minimap has no control of its own to hide it: turning it on and
    /// off is the host's, from its settings, a command or a key binding.
    /// </summary>
    public const string EnabledOptionName = "Minimap/Enabled";

    /// <summary>See <see cref="EnabledOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> EnabledId = new(EnabledOptionName);

    /// <summary>
    /// The minimap's width, in device-independent pixels: 110 by default, 30 to 400. Dragging the minimap's inner edge
    /// sets it on the view's own options.
    /// </summary>
    public const string WidthOptionName = "Minimap/Width";

    /// <summary>See <see cref="WidthOptionName"/>.</summary>
    public static readonly EditorOptionKey<double> WidthId = new(WidthOptionName);

    /// <summary>Whether the width is kept fixed; otherwise dragging the minimap's inner edge resizes it. Off by default.</summary>
    public const string LockWidthOptionName = "Minimap/LockWidth";

    /// <summary>See <see cref="LockWidthOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> LockWidthId = new(LockWidthOptionName);

    /// <summary>
    /// The height of a line in the minimap, in device-independent pixels: 3 by default, 1 to 8. A character is half as
    /// wide, so the minimap is a scaled picture of the text.
    /// </summary>
    public const string PixelsPerLineOptionName = "Minimap/PixelsPerLine";

    /// <summary>See <see cref="PixelsPerLineOptionName"/>.</summary>
    public static readonly EditorOptionKey<double> PixelsPerLineId = new(PixelsPerLineOptionName);

    /// <summary>How tall the document is drawn (<see cref="MinimapSizing"/>); proportional by default.</summary>
    public const string SizingOptionName = "Minimap/Sizing";

    /// <summary>See <see cref="SizingOptionName"/>.</summary>
    public static readonly EditorOptionKey<MinimapSizing> SizingId = new(SizingOptionName);

    /// <summary>How characters are drawn (<see cref="MinimapRenderStyle"/>); clean by default.</summary>
    public const string RenderStyleOptionName = "Minimap/RenderStyle";

    /// <summary>See <see cref="RenderStyleOptionName"/>.</summary>
    public static readonly EditorOptionKey<MinimapRenderStyle> RenderStyleId = new(RenderStyleOptionName);

    /// <summary>Which side the minimap stands on (<see cref="MinimapAlignment"/>); the right by default.</summary>
    public const string AlignmentOptionName = "Minimap/Alignment";

    /// <summary>See <see cref="AlignmentOptionName"/>.</summary>
    public static readonly EditorOptionKey<MinimapAlignment> AlignmentId = new(AlignmentOptionName);

    /// <summary>Where a click puts the view (<see cref="MinimapClickTarget"/>); the clicked code by default.</summary>
    public const string ClickTargetOptionName = "Minimap/ClickTarget";

    /// <summary>See <see cref="ClickTargetOptionName"/>.</summary>
    public static readonly EditorOptionKey<MinimapClickTarget> ClickTargetId = new(ClickTargetOptionName);

    /// <summary>When a click jumps (<see cref="MinimapJumpTrigger"/>); when the button goes down by default.</summary>
    public const string JumpTriggerOptionName = "Minimap/JumpTrigger";

    /// <summary>See <see cref="JumpTriggerOptionName"/>.</summary>
    public static readonly EditorOptionKey<MinimapJumpTrigger> JumpTriggerId = new(JumpTriggerOptionName);

    /// <summary>Whether a click only scrolls and leaves the caret where it is; Shift with a click extends the selection otherwise. Off by default.</summary>
    public const string MoveOnlyOptionName = "Minimap/MoveOnly";

    /// <summary>See <see cref="MoveOnlyOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> MoveOnlyId = new(MoveOnlyOptionName);

    /// <summary>The fewest lines a document needs for the minimap to draw it: 0 by default.</summary>
    public const string MinLineCountOptionName = "Minimap/MinLineCount";

    /// <summary>See <see cref="MinLineCountOptionName"/>.</summary>
    public static readonly EditorOptionKey<int> MinLineCountId = new(MinLineCountOptionName);

    /// <summary>The most lines the minimap draws: 20000 by default; 0 sets no limit.</summary>
    public const string MaxLineCountOptionName = "Minimap/MaxLineCount";

    /// <summary>See <see cref="MaxLineCountOptionName"/>.</summary>
    public static readonly EditorOptionKey<int> MaxLineCountId = new(MaxLineCountOptionName);

    /// <summary>
    /// What the minimap does for a document outside <see cref="MinLineCountId"/> and <see cref="MaxLineCountId"/>: keep its
    /// place and draw nothing, so the text does not move when the document crosses a limit (the default), or step aside.
    /// </summary>
    public const string EmptyOutOfRangeOptionName = "Minimap/EmptyOutOfRange";

    /// <summary>See <see cref="EmptyOutOfRangeOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> EmptyOutOfRangeId = new(EmptyOutOfRangeOptionName);

    /// <summary>Whether the picture is drawn at the display's resolution; off draws it at one pixel per unit and lets the display scale it. On by default.</summary>
    public const string HiDpiOptionName = "Minimap/HiDpi";

    /// <summary>See <see cref="HiDpiOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> HiDpiId = new(HiDpiOptionName);

    /// <summary>Whether characters take their classification colours; off draws them all in the text colour. On by default.</summary>
    public const string SyntaxHighlightOptionName = "Minimap/SyntaxHighlight";

    /// <summary>See <see cref="SyntaxHighlightOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> SyntaxHighlightId = new(SyntaxHighlightOptionName);

    /// <summary>Whether errors, warnings and suggestions (<see cref="Tagging.IErrorTag"/>) are marked. On by default.</summary>
    public const string ErrorHighlightOptionName = "Minimap/ErrorHighlight";

    /// <summary>See <see cref="ErrorHighlightOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ErrorHighlightId = new(ErrorHighlightOptionName);

    /// <summary>
    /// Whether text markers (<see cref="Tagging.ITextMarkerTag"/>, such as highlighted references), the find panel's
    /// matches and the selection are marked. On by default.
    /// </summary>
    public const string MarkupHighlightOptionName = "Minimap/MarkupHighlight";

    /// <summary>See <see cref="MarkupHighlightOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> MarkupHighlightId = new(MarkupHighlightOptionName);

    /// <summary>
    /// Whether lines added since the reference text are marked green and changed ones blue, across the minimap and under
    /// the text: the file as committed in git where it is tracked, else as last saved (<see cref="TextChangeBaseline"/>).
    /// On by default.
    /// </summary>
    public const string ChangeHighlightOptionName = "Minimap/ChangeHighlight";

    /// <summary>See <see cref="ChangeHighlightOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ChangeHighlightId = new(ChangeHighlightOptionName);

    /// <summary>Whether an error mark spans the minimap's whole width rather than the characters it covers. On by default.</summary>
    public const string ErrorFullLineOptionName = "Minimap/ErrorFullLine";

    /// <summary>See <see cref="ErrorFullLineOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ErrorFullLineId = new(ErrorFullLineOptionName);

    /// <summary>Whether any other mark spans the minimap's whole width rather than the characters it covers. Off by default.</summary>
    public const string MarkupFullLineOptionName = "Minimap/MarkupFullLine";

    /// <summary>See <see cref="MarkupFullLineOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> MarkupFullLineId = new(MarkupFullLineOptionName);

    /// <summary>Whether lines matching <see cref="MarkersPatternId"/> carry their label in larger type. On by default.</summary>
    public const string ShowMarkersOptionName = "Minimap/ShowMarkers";

    /// <summary>See <see cref="ShowMarkersOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ShowMarkersId = new(ShowMarkersOptionName);

    /// <summary>
    /// The regular expression a marker line matches; the rest of the line after the match is its label. By default
    /// <c>MARK:</c> comments and regions.
    /// </summary>
    public const string MarkersPatternOptionName = "Minimap/MarkersPattern";

    /// <summary>See <see cref="MarkersPatternOptionName"/>.</summary>
    public static readonly EditorOptionKey<string> MarkersPatternId = new(MarkersPatternOptionName);

    /// <summary>How tall a marker's label is, in minimap lines: 3 by default, 1 to 8.</summary>
    public const string MarkersScaleOptionName = "Minimap/MarkersScale";

    /// <summary>See <see cref="MarkersScaleOptionName"/>.</summary>
    public static readonly EditorOptionKey<double> MarkersScaleId = new(MarkersScaleOptionName);

    /// <summary>
    /// Whether the vertical scroll bar steps aside while the minimap shows, so the minimap is the view's scroll bar. Off by
    /// default.
    /// </summary>
    public const string HideOriginalScrollBarOptionName = "Minimap/HideOriginalScrollBar";

    /// <summary>See <see cref="HideOriginalScrollBarOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> HideOriginalScrollBarId = new(HideOriginalScrollBarOptionName);

    /// <summary>
    /// Whether the minimap stays out of the way and floats over the text while the pointer is on the vertical scroll bar
    /// or on the minimap itself. Only for a minimap on the right, beside the scroll bar, and not while the scroll bar is
    /// hidden (<see cref="HideOriginalScrollBarId"/>). Off by default.
    /// </summary>
    public const string ShowOnScrollBarHoverOptionName = "Minimap/ShowOnScrollBarHover";

    /// <summary>See <see cref="ShowOnScrollBarHoverOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ShowOnScrollBarHoverId = new(ShowOnScrollBarHoverOptionName);

    /// <summary>How long the pointer rests on the scroll bar before the minimap appears, in milliseconds: 0 by default.</summary>
    public const string ScrollBarHoverDelayOptionName = "Minimap/ScrollBarHoverDelay";

    /// <summary>See <see cref="ScrollBarHoverDelayOptionName"/>.</summary>
    public static readonly EditorOptionKey<int> ScrollBarHoverDelayId = new(ScrollBarHoverDelayOptionName);

    /// <summary>
    /// The viewport band's fill as <c>#RRGGBB</c>, drawn translucent, or <c>#AARRGGBB</c>; empty (the default) takes the
    /// format map's <see cref="MinimapFormatNames.Viewport"/>.
    /// </summary>
    public const string ViewportColorOptionName = "Minimap/ViewportColor";

    /// <summary>See <see cref="ViewportColorOptionName"/>.</summary>
    public static readonly EditorOptionKey<string> ViewportColorId = new(ViewportColorOptionName);

    /// <summary>
    /// The viewport band's border as <c>#RRGGBB</c> or <c>#AARRGGBB</c>; empty (the default) takes the format map's
    /// <see cref="MinimapFormatNames.ViewportBorder"/>.
    /// </summary>
    public const string ViewportBorderColorOptionName = "Minimap/ViewportBorderColor";

    /// <summary>See <see cref="ViewportBorderColorOptionName"/>.</summary>
    public static readonly EditorOptionKey<string> ViewportBorderColorId = new(ViewportBorderColorOptionName);

    /// <summary>The viewport band's border thickness: 0 (none) by default, up to 4.</summary>
    public const string ViewportBorderThicknessOptionName = "Minimap/ViewportBorderThickness";

    /// <summary>See <see cref="ViewportBorderThicknessOptionName"/>.</summary>
    public static readonly EditorOptionKey<double> ViewportBorderThicknessId = new(ViewportBorderThicknessOptionName);

    /// <summary>
    /// Whether resting the pointer on the minimap shows the code there in a popup over the editor page. Not on the
    /// viewport band, which stands for what the view shows already. On by default.
    /// </summary>
    public const string ShowPreviewOptionName = "Minimap/ShowPreview";

    /// <summary>See <see cref="ShowPreviewOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> ShowPreviewId = new(ShowPreviewOptionName);

    /// <summary>
    /// How long the pointer rests on the minimap before the preview opens, in milliseconds: 400 by default, up to 5000.
    /// Once open, the preview follows the pointer at once.
    /// </summary>
    public const string PreviewDelayOptionName = "Minimap/PreviewDelay";

    /// <summary>See <see cref="PreviewDelayOptionName"/>.</summary>
    public static readonly EditorOptionKey<int> PreviewDelayId = new(PreviewDelayOptionName);

    /// <summary>How many lines the preview shows: 10 by default, 1 to 50.</summary>
    public const string PreviewLineCountOptionName = "Minimap/PreviewLineCount";

    /// <summary>See <see cref="PreviewLineCountOptionName"/>.</summary>
    public static readonly EditorOptionKey<int> PreviewLineCountId = new(PreviewLineCountOptionName);

    /// <summary>Whether the wheel moves the code in an open preview rather than scrolling the view. Off by default.</summary>
    public const string WheelMovesPreviewOptionName = "Minimap/WheelMovesPreview";

    /// <summary>See <see cref="WheelMovesPreviewOptionName"/>.</summary>
    public static readonly EditorOptionKey<bool> WheelMovesPreviewId = new(WheelMovesPreviewOptionName);

    /// <summary>The default of <see cref="MarkersPatternId"/>: <c>MARK:</c> and <c>MARK: -</c> comments, and regions.</summary>
    public const string DefaultMarkersPattern = @"\bMARK:(?: -)?(?=\s|$)|#?region\b";
}

/// <summary>The names of the minimap's margins.</summary>
public static class MinimapMarginNames
{
    /// <summary>The minimap in the right margin container; <see cref="MinimapOptions.AlignmentId"/> picks this side or the other.</summary>
    public const string Minimap = "Minimap";

    /// <summary>The minimap in the left margin container.</summary>
    public const string LeftMinimap = "LeftMinimap";
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.EnabledOptionName)]
[Shared]
public sealed class MinimapEnabled : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.EnabledId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.WidthOptionName)]
[Shared]
public sealed class MinimapWidth : EditorOptionDefinition<double>
{
    internal const double Narrowest = 30.0;
    internal const double Widest = 400.0;

    public override double Default => 110.0;

    public override EditorOptionKey<double> Key => MinimapOptions.WidthId;

    public override bool IsValid(ref double proposedValue)
    {
        if (double.IsNaN(proposedValue))
        {
            return false;
        }

        proposedValue = Math.Clamp(proposedValue, Narrowest, Widest);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.LockWidthOptionName)]
[Shared]
public sealed class MinimapLockWidth : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => MinimapOptions.LockWidthId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.PixelsPerLineOptionName)]
[Shared]
public sealed class MinimapPixelsPerLine : EditorOptionDefinition<double>
{
    public override double Default => 3.0;

    public override EditorOptionKey<double> Key => MinimapOptions.PixelsPerLineId;

    public override bool IsValid(ref double proposedValue)
    {
        if (double.IsNaN(proposedValue))
        {
            return false;
        }

        proposedValue = Math.Clamp(proposedValue, 1.0, 8.0);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.SizingOptionName)]
[Shared]
public sealed class MinimapSizingOption : EditorOptionDefinition<MinimapSizing>
{
    public override MinimapSizing Default => MinimapSizing.Proportional;

    public override EditorOptionKey<MinimapSizing> Key => MinimapOptions.SizingId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.RenderStyleOptionName)]
[Shared]
public sealed class MinimapRenderStyleOption : EditorOptionDefinition<MinimapRenderStyle>
{
    public override MinimapRenderStyle Default => MinimapRenderStyle.Clean;

    public override EditorOptionKey<MinimapRenderStyle> Key => MinimapOptions.RenderStyleId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.AlignmentOptionName)]
[Shared]
public sealed class MinimapAlignmentOption : EditorOptionDefinition<MinimapAlignment>
{
    public override MinimapAlignment Default => MinimapAlignment.Right;

    public override EditorOptionKey<MinimapAlignment> Key => MinimapOptions.AlignmentId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ClickTargetOptionName)]
[Shared]
public sealed class MinimapClickTargetOption : EditorOptionDefinition<MinimapClickTarget>
{
    public override MinimapClickTarget Default => MinimapClickTarget.CodePosition;

    public override EditorOptionKey<MinimapClickTarget> Key => MinimapOptions.ClickTargetId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.JumpTriggerOptionName)]
[Shared]
public sealed class MinimapJumpTriggerOption : EditorOptionDefinition<MinimapJumpTrigger>
{
    public override MinimapJumpTrigger Default => MinimapJumpTrigger.MouseDown;

    public override EditorOptionKey<MinimapJumpTrigger> Key => MinimapOptions.JumpTriggerId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MoveOnlyOptionName)]
[Shared]
public sealed class MinimapMoveOnly : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => MinimapOptions.MoveOnlyId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MinLineCountOptionName)]
[Shared]
public sealed class MinimapMinLineCount : EditorOptionDefinition<int>
{
    public override int Default => 0;

    public override EditorOptionKey<int> Key => MinimapOptions.MinLineCountId;

    public override bool IsValid(ref int proposedValue)
    {
        proposedValue = Math.Max(0, proposedValue);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MaxLineCountOptionName)]
[Shared]
public sealed class MinimapMaxLineCount : EditorOptionDefinition<int>
{
    public override int Default => 20000;

    public override EditorOptionKey<int> Key => MinimapOptions.MaxLineCountId;

    public override bool IsValid(ref int proposedValue)
    {
        proposedValue = Math.Max(0, proposedValue);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.EmptyOutOfRangeOptionName)]
[Shared]
public sealed class MinimapEmptyOutOfRange : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.EmptyOutOfRangeId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.HiDpiOptionName)]
[Shared]
public sealed class MinimapHiDpi : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.HiDpiId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.SyntaxHighlightOptionName)]
[Shared]
public sealed class MinimapSyntaxHighlight : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.SyntaxHighlightId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ErrorHighlightOptionName)]
[Shared]
public sealed class MinimapErrorHighlight : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.ErrorHighlightId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MarkupHighlightOptionName)]
[Shared]
public sealed class MinimapMarkupHighlight : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.MarkupHighlightId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ChangeHighlightOptionName)]
[Shared]
public sealed class MinimapChangeHighlight : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.ChangeHighlightId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ErrorFullLineOptionName)]
[Shared]
public sealed class MinimapErrorFullLine : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.ErrorFullLineId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MarkupFullLineOptionName)]
[Shared]
public sealed class MinimapMarkupFullLine : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => MinimapOptions.MarkupFullLineId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ShowMarkersOptionName)]
[Shared]
public sealed class MinimapShowMarkers : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.ShowMarkersId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MarkersPatternOptionName)]
[Shared]
public sealed class MinimapMarkersPattern : EditorOptionDefinition<string>
{
    public override string Default => MinimapOptions.DefaultMarkersPattern;

    public override EditorOptionKey<string> Key => MinimapOptions.MarkersPatternId;

    public override bool IsValid(ref string proposedValue)
    {
        proposedValue ??= string.Empty;
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.MarkersScaleOptionName)]
[Shared]
public sealed class MinimapMarkersScale : EditorOptionDefinition<double>
{
    public override double Default => 3.0;

    public override EditorOptionKey<double> Key => MinimapOptions.MarkersScaleId;

    public override bool IsValid(ref double proposedValue)
    {
        if (double.IsNaN(proposedValue))
        {
            return false;
        }

        proposedValue = Math.Clamp(proposedValue, 1.0, 8.0);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.HideOriginalScrollBarOptionName)]
[Shared]
public sealed class MinimapHideOriginalScrollBar : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => MinimapOptions.HideOriginalScrollBarId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ShowOnScrollBarHoverOptionName)]
[Shared]
public sealed class MinimapShowOnScrollBarHover : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => MinimapOptions.ShowOnScrollBarHoverId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ScrollBarHoverDelayOptionName)]
[Shared]
public sealed class MinimapScrollBarHoverDelay : EditorOptionDefinition<int>
{
    public override int Default => 0;

    public override EditorOptionKey<int> Key => MinimapOptions.ScrollBarHoverDelayId;

    public override bool IsValid(ref int proposedValue)
    {
        proposedValue = Math.Clamp(proposedValue, 0, 5000);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ViewportColorOptionName)]
[Shared]
public sealed class MinimapViewportColor : EditorOptionDefinition<string>
{
    public override string Default => string.Empty;

    public override EditorOptionKey<string> Key => MinimapOptions.ViewportColorId;

    public override bool IsValid(ref string proposedValue)
    {
        proposedValue ??= string.Empty;
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ViewportBorderColorOptionName)]
[Shared]
public sealed class MinimapViewportBorderColor : EditorOptionDefinition<string>
{
    public override string Default => string.Empty;

    public override EditorOptionKey<string> Key => MinimapOptions.ViewportBorderColorId;

    public override bool IsValid(ref string proposedValue)
    {
        proposedValue ??= string.Empty;
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ViewportBorderThicknessOptionName)]
[Shared]
public sealed class MinimapViewportBorderThickness : EditorOptionDefinition<double>
{
    public override double Default => 0.0;

    public override EditorOptionKey<double> Key => MinimapOptions.ViewportBorderThicknessId;

    public override bool IsValid(ref double proposedValue)
    {
        if (double.IsNaN(proposedValue))
        {
            return false;
        }

        proposedValue = Math.Clamp(proposedValue, 0.0, 4.0);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.ShowPreviewOptionName)]
[Shared]
public sealed class MinimapShowPreview : EditorOptionDefinition<bool>
{
    public override bool Default => true;

    public override EditorOptionKey<bool> Key => MinimapOptions.ShowPreviewId;
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.PreviewDelayOptionName)]
[Shared]
public sealed class MinimapPreviewDelay : EditorOptionDefinition<int>
{
    public override int Default => 400;

    public override EditorOptionKey<int> Key => MinimapOptions.PreviewDelayId;

    public override bool IsValid(ref int proposedValue)
    {
        proposedValue = Math.Clamp(proposedValue, 0, 5000);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.PreviewLineCountOptionName)]
[Shared]
public sealed class MinimapPreviewLineCount : EditorOptionDefinition<int>
{
    public override int Default => 10;

    public override EditorOptionKey<int> Key => MinimapOptions.PreviewLineCountId;

    public override bool IsValid(ref int proposedValue)
    {
        proposedValue = Math.Clamp(proposedValue, 1, 50);
        return true;
    }
}

[Export(typeof(EditorOptionDefinition))]
[Name(MinimapOptions.WheelMovesPreviewOptionName)]
[Shared]
public sealed class MinimapWheelMovesPreview : EditorOptionDefinition<bool>
{
    public override bool Default => false;

    public override EditorOptionKey<bool> Key => MinimapOptions.WheelMovesPreviewId;
}

