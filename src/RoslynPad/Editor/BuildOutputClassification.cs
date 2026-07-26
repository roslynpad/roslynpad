using System.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace RoslynPad.Editor;

/// <summary>
/// The classification type names for build output lines, matching VSColorOutput's
/// ColorClassifier scheme.
/// </summary>
public static class BuildOutputClassificationTypes
{
    public const string BuildHead = "BuildHead";
    public const string BuildText = "BuildText";
    public const string LogError = "LogError";
    public const string LogWarning = "LogWarn";
    public const string LogInformation = "LogInfo";
    public const string LogCustom1 = "LogCustom1";
    public const string LogCustom2 = "LogCustom2";
    public const string LogCustom3 = "LogCustom3";
    public const string LogCustom4 = "LogCustom4";
}

/// <summary>The content type and classification types for the build output pane.</summary>
public sealed class BuildOutputClassificationDefinitions
{
    public const string ContentType = "BuildOutput";

    [Export]
    [Name(ContentType)]
    [BaseDefinition("text")]
    public ContentTypeDefinition? BuildOutputContentType { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.BuildHead)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? BuildHead { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.BuildText)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? BuildText { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogError)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogError { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogWarning)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogWarning { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogInformation)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogInformation { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogCustom1)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogCustom1 { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogCustom2)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogCustom2 { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogCustom3)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogCustom3 { get; }

    [Export]
    [Name(BuildOutputClassificationTypes.LogCustom4)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? LogCustom4 { get; }
}

// Registration-only format definitions: the classification format map reads explicit text
// properties only for types that have an exported definition, so without these the colors
// ThemeClassificationFormats.ApplyBuildOutput sets are never picked up. They stay colorless —
// the theme is the single source of colors (unthemed definitions get cleared on theme apply).
#pragma warning disable CA1812 // Instantiated by the composition container.

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.BuildHead)]
[Name(BuildOutputClassificationTypes.BuildHead)]
public sealed class BuildHeadFormat : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogError)]
[Name(BuildOutputClassificationTypes.LogError)]
public sealed class LogErrorFormat : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogWarning)]
[Name(BuildOutputClassificationTypes.LogWarning)]
public sealed class LogWarningFormat : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogInformation)]
[Name(BuildOutputClassificationTypes.LogInformation)]
public sealed class LogInformationFormat : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogCustom1)]
[Name(BuildOutputClassificationTypes.LogCustom1)]
public sealed class LogCustom1Format : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogCustom2)]
[Name(BuildOutputClassificationTypes.LogCustom2)]
public sealed class LogCustom2Format : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogCustom3)]
[Name(BuildOutputClassificationTypes.LogCustom3)]
public sealed class LogCustom3Format : ClassificationFormatDefinition;

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = BuildOutputClassificationTypes.LogCustom4)]
[Name(BuildOutputClassificationTypes.LogCustom4)]
public sealed class LogCustom4Format : ClassificationFormatDefinition;

#pragma warning restore CA1812

/// <summary>
/// Classifies build output whole lines by first matching pattern, using VSColorOutput's
/// default patterns.
/// </summary>
[Export(typeof(IClassifierProvider))]
[ContentType(BuildOutputClassificationDefinitions.ContentType)]
public sealed class BuildOutputClassifierProvider : IClassifierProvider
{
    private readonly IClassificationTypeRegistryService _classificationTypes;

    [ImportingConstructor]
    public BuildOutputClassifierProvider(IClassificationTypeRegistryService classificationTypes)
    {
        _classificationTypes = classificationTypes;
    }

    public IClassifier GetClassifier(ITextBuffer textBuffer)
    {
        ArgumentNullException.ThrowIfNull(textBuffer);
        return textBuffer.Properties.GetOrCreateSingletonProperty(() => new BuildOutputClassifier(_classificationTypes));
    }

    private sealed class BuildOutputClassifier(IClassificationTypeRegistryService registry) : IClassifier
    {
        private static readonly TimeSpan s_matchTimeout = TimeSpan.FromMilliseconds(100);

        // VSColorOutput's default patterns, in order; first match wins, no match is plain
        // build text (rendered in the default foreground, so no span is emitted for it).
        private static readonly (Regex Regex, string Type)[] s_patterns =
        [
            (Create(@"\+\+\+\>", ignoreCase: false), BuildOutputClassificationTypes.LogCustom1),
            (Create(@"[t|c]sc\.exe", ignoreCase: false), BuildOutputClassificationTypes.BuildText),
            (Create(@"(=====|-----|Projects build report|Status    \| Project \[Config\|platform\])", ignoreCase: false), BuildOutputClassificationTypes.BuildHead),
            (Create(@"0 error.+0 warning", ignoreCase: true), BuildOutputClassificationTypes.BuildHead),
            (Create(@"^(\d+>)?\s*0 error\(s\)\s*$", ignoreCase: true), BuildOutputClassificationTypes.BuildHead),
            (Create(@"^(\d+>)?\s*0 warning\(s\)\s*$", ignoreCase: true), BuildOutputClassificationTypes.BuildHead),
            (Create(@"0 failed|Succeeded", ignoreCase: true), BuildOutputClassificationTypes.BuildHead),
            (Create(@"(\W|^)^(?!.*warning\s(BC|CS|CA)\d+:).*((?<!/)error|fail|crit|failed|exception)[^\w\.\-\+]", ignoreCase: true), BuildOutputClassificationTypes.LogError),
            (Create(@"(exception:|stack trace:)", ignoreCase: true), BuildOutputClassificationTypes.LogError),
            (Create(@"^\s+at\s", ignoreCase: true), BuildOutputClassificationTypes.LogError),
            (Create(@"(\W|^)(warning|warn)\W", ignoreCase: true), BuildOutputClassificationTypes.LogWarning),
            (Create(@"(\W|^)(information|info)\W", ignoreCase: true), BuildOutputClassificationTypes.LogInformation),
            (Create(@"Could not find file", ignoreCase: true), BuildOutputClassificationTypes.LogError),
            (Create(@"failed", ignoreCase: true), BuildOutputClassificationTypes.LogError),
        ];

        private static Regex Create(string pattern, bool ignoreCase) =>
            new(pattern,
                RegexOptions.Compiled | RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None),
                s_matchTimeout);

#pragma warning disable CS0067 // The classification of a line never changes after it is appended.
        public event EventHandler<ClassificationChangedEventArgs>? ClassificationChanged;
#pragma warning restore CS0067

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            var result = new List<ClassificationSpan>();
            var snapshot = span.Snapshot;
            int firstLine = snapshot.GetLineNumberFromPosition(span.Start);
            int lastLine = snapshot.GetLineNumberFromPosition(span.End);
            for (int lineNumber = firstLine; lineNumber <= lastLine; lineNumber++)
            {
                var line = snapshot.GetLineFromLineNumber(lineNumber);
                if (Classify(line.GetText()) is { } typeName &&
                    registry.GetClassificationType(typeName) is { } type)
                {
                    result.Add(new ClassificationSpan(new SnapshotSpan(line.Start, line.End), type));
                }
            }

            return result;
        }

        private static string? Classify(string text)
        {
            foreach (var (regex, type) in s_patterns)
            {
                try
                {
                    if (regex.IsMatch(text))
                    {
                        // Plain build text renders in the default foreground; the early BuildText
                        // pattern still shields compiler command lines from the error patterns.
                        return type == BuildOutputClassificationTypes.BuildText ? null : type;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                }
            }

            return null;
        }
    }
}
