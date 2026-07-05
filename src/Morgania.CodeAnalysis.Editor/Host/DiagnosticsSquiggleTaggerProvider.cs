using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Tagging;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

/// <summary>Host-configurable options for the diagnostics squiggles.</summary>
public static class DiagnosticsSquiggles
{
    /// <summary>
    /// Diagnostic ids that should not produce squiggles, from the host's settings
    /// (set by the host after composition; the tagger is composed by MEF, so the set
    /// cannot flow through the constructor).
    /// </summary>
    public static ImmutableHashSet<string> DisabledDiagnostics { get; set; } = [];
}

/// <summary>
/// The classic diagnostics squiggle tagger. Visual Studio gets its squiggles through LSP pull
/// diagnostics these days, so upstream EditorFeatures no longer ships an IErrorTag tagger —
/// but the underlying pull-diagnostics tagging machinery
/// (<see cref="AbstractDiagnosticsTaggerProvider{TTag}"/>, which asks
/// IDiagnosticAnalyzerService for compiler + analyzer diagnostics per kind) survives and runs
/// fully in-proc. This provider is the host's stand-in for the removed
/// DiagnosticsSquiggleTaggerProvider: it turns those diagnostics into error tags that the
/// host's squiggle adornment renderer draws.
/// </summary>
[Export(typeof(ITaggerProvider))]
[Shared]
[ContentType("Roslyn Languages")]
[TagType(typeof(IErrorTag))]
internal sealed class DiagnosticsSquiggleTaggerProvider : AbstractDiagnosticsTaggerProvider<IErrorTag>
{
    [ImportingConstructor]
    public DiagnosticsSquiggleTaggerProvider(TaggerHost taggerHost)
        : base(taggerHost, FeatureAttribute.ErrorSquiggles)
    {
    }

    protected override ImmutableArray<IOption2> Options => [];

    protected override bool IncludeDiagnostic(DiagnosticData data) =>
        data.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning or DiagnosticSeverity.Info &&
        !string.IsNullOrWhiteSpace(data.Message) &&
        !DiagnosticsSquiggles.DisabledDiagnostics.Contains(data.Id);

    protected override IErrorTag? CreateTag(Workspace workspace, DiagnosticData diagnostic) =>
        GetErrorType(diagnostic.Severity) is { } errorType
            ? new ErrorTag(errorType, $"{diagnostic.Id}: {diagnostic.Message}")
            : null;

    private static string? GetErrorType(DiagnosticSeverity severity) => severity switch
    {
        DiagnosticSeverity.Error => PredefinedErrorTypeNames.SyntaxError,
        DiagnosticSeverity.Warning => PredefinedErrorTypeNames.Warning,
        DiagnosticSeverity.Info => PredefinedErrorTypeNames.Suggestion,
        _ => null,
    };

    protected override bool TagEquals(IErrorTag tag1, IErrorTag tag2) =>
        tag1.ErrorType == tag2.ErrorType && Equals(tag1.ToolTipContent, tag2.ToolTipContent);
}
