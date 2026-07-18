using System.Composition;
using Avalonia.Media;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.CodeAnalysis.Editor;

// Classification colors (VS dark theme). Roslyn's own format definitions live in
// ClassificationTypeFormatDefinitions.cs, which is excluded from the recompile because it is
// WPF-based; these Avalonia equivalents cover the common C# classifications. Types without a
// definition inherit from their base classification, ultimately the view's default text
// properties.

#pragma warning disable CA1812 // Instantiated by the composition container.

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Keyword)]
[Name("Morgania/Keyword")]
[Order(After = Priority.Default)]
public sealed class KeywordFormat : ClassificationFormatDefinition
{
    public KeywordFormat() => ForegroundColor = Color.FromRgb(0x56, 0x9C, 0xD6);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ControlKeyword)]
[Name("Morgania/ControlKeyword")]
[Order(After = Priority.Default)]
public sealed class ControlKeywordFormat : ClassificationFormatDefinition
{
    public ControlKeywordFormat() => ForegroundColor = Color.FromRgb(0xC5, 0x86, 0xC0);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Comment)]
[Name("Morgania/Comment")]
[Order(After = Priority.Default)]
public sealed class CommentFormat : ClassificationFormatDefinition
{
    public CommentFormat() => ForegroundColor = Color.FromRgb(0x6A, 0x99, 0x55);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.String)]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Character)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.VerbatimStringLiteral)]
[Name("Morgania/String")]
[Order(After = Priority.Default)]
public sealed class StringFormat : ClassificationFormatDefinition
{
    public StringFormat() => ForegroundColor = Color.FromRgb(0xCE, 0x91, 0x78);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.StringEscapeCharacter)]
[Name("Morgania/StringEscape")]
[Order(After = Priority.Default)]
public sealed class StringEscapeFormat : ClassificationFormatDefinition
{
    public StringEscapeFormat() => ForegroundColor = Color.FromRgb(0xD7, 0xBA, 0x7D);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Number)]
[Name("Morgania/Number")]
[Order(After = Priority.Default)]
public sealed class NumberFormat : ClassificationFormatDefinition
{
    public NumberFormat() => ForegroundColor = Color.FromRgb(0xB5, 0xCE, 0xA8);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ClassName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RecordClassName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.DelegateName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ModuleName)]
[Name("Morgania/ClassName")]
[Order(After = Priority.Default)]
public sealed class ClassNameFormat : ClassificationFormatDefinition
{
    public ClassNameFormat() => ForegroundColor = Color.FromRgb(0x4E, 0xC9, 0xB0);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.StructName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RecordStructName)]
[Name("Morgania/StructName")]
[Order(After = Priority.Default)]
public sealed class StructNameFormat : ClassificationFormatDefinition
{
    public StructNameFormat() => ForegroundColor = Color.FromRgb(0x86, 0xC6, 0x91);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.InterfaceName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.EnumName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.TypeParameterName)]
[Name("Morgania/InterfaceName")]
[Order(After = Priority.Default)]
public sealed class InterfaceNameFormat : ClassificationFormatDefinition
{
    public InterfaceNameFormat() => ForegroundColor = Color.FromRgb(0xB8, 0xD7, 0xA3);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.MethodName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ExtensionMethodName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.OperatorOverloaded)]
[Name("Morgania/MethodName")]
[Order(After = Priority.Default)]
public sealed class MethodNameFormat : ClassificationFormatDefinition
{
    public MethodNameFormat() => ForegroundColor = Color.FromRgb(0xDC, 0xDC, 0xAA);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.LocalName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.ParameterName)]
[Name("Morgania/LocalName")]
[Order(After = Priority.Default)]
public sealed class LocalNameFormat : ClassificationFormatDefinition
{
    public LocalNameFormat() => ForegroundColor = Color.FromRgb(0x9C, 0xDC, 0xFE);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.PreprocessorKeyword)]
[Name("Morgania/PreprocessorKeyword")]
[Order(After = Priority.Default)]
public sealed class PreprocessorKeywordFormat : ClassificationFormatDefinition
{
    public PreprocessorKeywordFormat() => ForegroundColor = Color.FromRgb(0x9B, 0x9B, 0x9B);
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
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentText)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentDelimiter)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentAttributeName)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentAttributeQuotes)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentAttributeValue)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentComment)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentCDataSection)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentEntityReference)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.XmlDocCommentProcessingInstruction)]
[Name("Morgania/XmlDocComment")]
[Order(After = Priority.Default)]
public sealed class XmlDocCommentFormat : ClassificationFormatDefinition
{
    public XmlDocCommentFormat() => ForegroundColor = Color.FromRgb(0x60, 0x8B, 0x4E);
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexText)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexCharacterClass)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexAnchor)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexQuantifier)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexGrouping)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexAlternation)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexOtherEscape)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexSelfEscapedCharacter)]
[ClassificationType(ClassificationTypeNames = ClassificationTypeNames.RegexComment)]
[Name("Morgania/Regex")]
[Order(After = Priority.Default)]
public sealed class RegexFormat : ClassificationFormatDefinition
{
    public RegexFormat() => ForegroundColor = Color.FromRgb(0xD1, 0x69, 0x69);
}

// The marker drawn on matching braces (rendered by TextMarkerAdornmentManager, produced by
// Roslyn's BraceHighlightingViewTaggerProvider). Roslyn's own definition
// (BraceMatchingTypeFormatDefinitions) is excluded from the recompile because it is WPF-based;
// the theme feeds live colors over this fallback (ThemeClassificationFormats.ApplyBraceMatching).
[Export(typeof(EditorFormatDefinition))]
[Name(Microsoft.CodeAnalysis.BraceMatching.ClassificationTypeDefinitions.BraceMatchingName)]
public sealed class BraceMatchingMarkerFormat : MarkerFormatDefinition
{
    public BraceMatchingMarkerFormat()
    {
        DisplayName = "Brace Matching";
        Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x88, 0x88, 0x88));
        Border = new Pen(new SolidColorBrush(Color.FromArgb(0xA0, 0x88, 0x88, 0x88)));
    }
}

// The markers drawn on a symbol's definition and references when the caret is on one of them
// (rendered by TextMarkerAdornmentManager, produced by Roslyn's
// ReferenceHighlightingViewTaggerProvider). Roslyn's definition/written-reference formats are
// WPF-based and excluded from the recompile, and the read-reference format belongs to the
// closed-source editor, so the host exports all three; the theme feeds live colors over these
// fallbacks (ThemeClassificationFormats.ApplyReferenceHighlighting).
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

[Export(typeof(EditorFormatDefinition))]
[Name(Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.DefinitionHighlightTag.TagId)]
public sealed class DefinitionHighlightMarkerFormat : MarkerFormatDefinition
{
    public DefinitionHighlightMarkerFormat()
    {
        DisplayName = "Highlighted Definition";
        Fill = new SolidColorBrush(Color.FromArgb(0x30, 0x88, 0x88, 0x88));
    }
}

[Export(typeof(EditorFormatDefinition))]
[Name(Microsoft.CodeAnalysis.Editor.ReferenceHighlighting.WrittenReferenceHighlightTag.TagId)]
public sealed class WrittenReferenceHighlightMarkerFormat : MarkerFormatDefinition
{
    public WrittenReferenceHighlightMarkerFormat()
    {
        DisplayName = "Highlighted Written Reference";
        Fill = new SolidColorBrush(Color.FromArgb(0x48, 0x88, 0x88, 0x88));
    }
}

#pragma warning restore CA1812
