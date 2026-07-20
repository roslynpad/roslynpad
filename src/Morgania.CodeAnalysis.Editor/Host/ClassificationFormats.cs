using System.Composition;
using Avalonia.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

// Fallback classification/marker colors, VS light defaults to match the recompiled Roslyn
// ClassificationTypeFormatDefinitions (which uses VS light colors) and the light-themed demo host.
//
// Roslyn's ClassificationTypeFormatDefinitions provides a per-type format definition for every
// semantic C# classification (types, members, xml doc comment, regex, json, xml literal, ...), so
// those are NOT redefined here. What remains are the *standard* classifications that neither Roslyn
// nor the vendored editor supplies a format for (keyword, comment, string, character, preprocessor
// keyword, excluded code) plus "url", and the marker formats. A host that applies a theme overrides
// all of these (ThemeClassificationFormats); the colors here only surface in the un-themed demo.

#pragma warning disable CA1812 // Instantiated by the composition container.

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Keyword)]
[Name("Morgania/Keyword")]
[Order(After = Priority.Default)]
public sealed class KeywordFormat : ClassificationFormatDefinition
{
    public KeywordFormat() => ForegroundColor = Color.FromRgb(0x00, 0x00, 0xFF);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Comment)]
[Name("Morgania/Comment")]
[Order(After = Priority.Default)]
public sealed class CommentFormat : ClassificationFormatDefinition
{
    public CommentFormat() => ForegroundColor = Color.FromRgb(0x00, 0x80, 0x00);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.String)]
[Name("Morgania/String")]
[Order(After = Priority.Default)]
public sealed class StringFormat : ClassificationFormatDefinition
{
    public StringFormat() => ForegroundColor = Color.FromRgb(0xA3, 0x15, 0x15);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Character)]
[Name("Morgania/Character")]
[Order(After = Priority.Default)]
public sealed class CharacterFormat : ClassificationFormatDefinition
{
    public CharacterFormat() => ForegroundColor = Color.FromRgb(0xA3, 0x15, 0x15);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PreprocessorKeyword)]
[Name("Morgania/PreprocessorKeyword")]
[Order(After = Priority.Default)]
public sealed class PreprocessorKeywordFormat : ClassificationFormatDefinition
{
    public PreprocessorKeywordFormat() => ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.ExcludedCode)]
[Name("Morgania/ExcludedCode")]
[Order(After = Priority.Default)]
public sealed class ExcludedCodeFormat : ClassificationFormatDefinition
{
    public ExcludedCodeFormat() => ForegroundColor = Color.FromRgb(0x80, 0x80, 0x80);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = "url")]
[Name("Morgania/Url")]
[Order(After = Priority.Default)]
public sealed class UrlFormat : ClassificationFormatDefinition
{
    public UrlFormat() => ForegroundColor = Color.FromRgb(0x00, 0x00, 0xFF);
}

// The marker drawn on a symbol's references when the caret is on one of them (rendered by
// TextMarkerAdornmentManager, produced by Roslyn's ReferenceHighlightingViewTaggerProvider).
// The definition/written-reference formats come from the recompiled Roslyn tag definitions,
// but the read-reference format belongs to the closed-source editor, so the host supplies it;
// the theme feeds live colors over all of them
// (ThemeClassificationFormats.ApplyReferenceHighlighting).
[Export(typeof(EditorFormatDefinition))]
[Name(Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.ReferenceHighlightTag.TagId)]
public sealed class ReferenceHighlightMarkerFormat : MarkerFormatDefinition
{
    public ReferenceHighlightMarkerFormat()
    {
        DisplayName = "Highlighted Reference";
        Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x88, 0x88, 0x88));
    }
}

// The standard VS Fonts-and-Colors entry recompiled Roslyn code reads for the editor
// background (e.g. inline diagnostics); the host overlays the theme's actual color.
[Export(typeof(EditorFormatDefinition))]
[Name("TextView Background")]
public sealed class TextViewBackgroundFormat : EditorFormatDefinition
{
    public TextViewBackgroundFormat()
    {
        DisplayName = "TextView Background";
        BackgroundColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
    }
}

#pragma warning restore CA1812
