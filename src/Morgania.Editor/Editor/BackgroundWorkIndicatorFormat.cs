#nullable enable

namespace Microsoft.VisualStudio.Text.Editor;

using Avalonia.Media;

/// <summary>
/// The editor-format-map key and property names the background-work progress indicator reads
/// its colors from. Hosts theme the indicator by setting these properties (as
/// <see cref="IBrush"/> values) on the map returned by
/// <see cref="Microsoft.VisualStudio.Text.Classification.IEditorFormatMapService"/>;
/// properties left unset fall back to the built-in blue.
/// </summary>
public static class BackgroundWorkIndicatorFormatNames
{
    /// <summary>The editor format map key.</summary>
    public const string Name = "Background Work Indicator";

    /// <summary>The progress bar's fill brush.</summary>
    public const string Foreground = "Foreground";
}
