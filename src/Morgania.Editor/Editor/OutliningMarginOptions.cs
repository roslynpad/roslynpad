#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using System.Composition;

using Microsoft.VisualStudio.Utilities;

/// <summary>When the outlining margin shows the chevron of an expanded region; a collapsed region's always shows.</summary>
public enum OutliningChevronVisibility
{
    /// <summary>While the pointer is over the margin, as the VS Code gutter has it.</summary>
    MouseOver,

    /// <summary>Always, so the regions that fold can be seen without looking for them.</summary>
    Always,
}

/// <summary>
/// The options of the outlining margin (<see cref="PredefinedMarginNames.Outlining"/>). They are ordinary editor options,
/// so a host sets them globally, on a buffer's options or on one view's (<see cref="IEditorOptions"/>).
/// </summary>
public static class OutliningMarginOptions
{
    /// <summary>When an expanded region's chevron shows (<see cref="OutliningChevronVisibility"/>); on hover by default.</summary>
    public const string ChevronVisibilityOptionName = "OutliningMargin/ChevronVisibility";

    /// <summary>See <see cref="ChevronVisibilityOptionName"/>.</summary>
    public static readonly EditorOptionKey<OutliningChevronVisibility> ChevronVisibilityId = new(ChevronVisibilityOptionName);
}

[Export(typeof(EditorOptionDefinition))]
[Name(OutliningMarginOptions.ChevronVisibilityOptionName)]
[Shared]
public sealed class OutliningChevronVisibilityOption : EditorOptionDefinition<OutliningChevronVisibility>
{
    public override OutliningChevronVisibility Default => OutliningChevronVisibility.MouseOver;

    public override EditorOptionKey<OutliningChevronVisibility> Key => OutliningMarginOptions.ChevronVisibilityId;
}
