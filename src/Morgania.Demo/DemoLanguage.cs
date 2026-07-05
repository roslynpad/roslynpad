namespace Microsoft.VisualStudio.Demo;

using System.Composition;
using System.Text.RegularExpressions;

using Avalonia.Media;

using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The standard classification types used by the demo. The VS platform's
/// IStandardClassificationService (which normally defines these) was never open-sourced,
/// so the demo registers the definitions itself, the way a language package would.
/// </summary>
public sealed class DemoClassificationTypes
{
    [Export]
    [Name(PredefinedClassificationTypeNames.Keyword)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? KeywordType { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Comment)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? CommentType { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.String)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? StringType { get; }

    [Export]
    [Name(PredefinedClassificationTypeNames.Number)]
    [BaseDefinition("text")]
    public ClassificationTypeDefinition? NumberType { get; }
}

/// <summary>
/// A deliberately simple regex highlighter over the standard classification types — just
/// enough language smarts to exercise the M1 rendering pipeline. Real classification comes
/// from Roslyn once the editor layers are complete.
/// </summary>
[Export(typeof(IClassifierProvider))]
[ContentType("code")]
public sealed partial class DemoClassifierProvider : IClassifierProvider
{
    private readonly IClassificationTypeRegistryService _classificationTypes;

    [ImportingConstructor]
    public DemoClassifierProvider(IClassificationTypeRegistryService classificationTypes)
    {
        _classificationTypes = classificationTypes;
    }

    public IClassifier GetClassifier(ITextBuffer textBuffer)
    {
        ArgumentNullException.ThrowIfNull(textBuffer);
        return textBuffer.Properties.GetOrCreateSingletonProperty(() => new DemoClassifier(_classificationTypes));
    }

    private sealed partial class DemoClassifier : IClassifier
    {
        private readonly IClassificationType _keyword;
        private readonly IClassificationType _comment;
        private readonly IClassificationType _string;
        private readonly IClassificationType _number;

        public DemoClassifier(IClassificationTypeRegistryService registry)
        {
            _keyword = registry.GetClassificationType(PredefinedClassificationTypeNames.Keyword);
            _comment = registry.GetClassificationType(PredefinedClassificationTypeNames.Comment);
            _string = registry.GetClassificationType(PredefinedClassificationTypeNames.String);
            _number = registry.GetClassificationType(PredefinedClassificationTypeNames.Number);
        }

#pragma warning disable CS0067 // The demo classification never changes after load.
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
                ClassifyLine(line, result);
            }

            return result;
        }

        private void ClassifyLine(ITextSnapshotLine line, List<ClassificationSpan> result)
        {
            string text = line.GetText();
            int lineStart = line.Start.Position;

            // Comments and strings win over everything that overlaps them.
            var taken = new List<(int Start, int End)>();
            void Add(int start, int end, IClassificationType type)
            {
                result.Add(new ClassificationSpan(new SnapshotSpan(line.Snapshot, lineStart + start, end - start), type));
                taken.Add((start, end));
            }

            bool IsFree(int start, int end) => taken.All(range => end <= range.Start || start >= range.End);

            var commentMatch = CommentPattern().Match(text);
            foreach (Match match in StringPattern().Matches(text))
            {
                if (!commentMatch.Success || match.Index < commentMatch.Index)
                {
                    Add(match.Index, match.Index + match.Length, _string);
                }
            }

            if (commentMatch.Success && IsFree(commentMatch.Index, text.Length))
            {
                Add(commentMatch.Index, text.Length, _comment);
            }

            foreach (Match match in KeywordPattern().Matches(text))
            {
                if (IsFree(match.Index, match.Index + match.Length))
                {
                    Add(match.Index, match.Index + match.Length, _keyword);
                }
            }

            foreach (Match match in NumberPattern().Matches(text))
            {
                if (IsFree(match.Index, match.Index + match.Length))
                {
                    Add(match.Index, match.Index + match.Length, _number);
                }
            }

            result.Sort((left, right) => left.Span.Start.Position.CompareTo(right.Span.Start.Position));
        }

        [GeneratedRegex(@"//.*$")]
        private static partial Regex CommentPattern();

        [GeneratedRegex("\"[^\"]*\"?")]
        private static partial Regex StringPattern();

        [GeneratedRegex(@"\b(abstract|as|async|await|base|bool|break|case|catch|char|class|const|continue|default|do|double|else|enum|false|finally|for|foreach|get|if|in|int|interface|internal|is|let|namespace|new|null|out|override|private|protected|public|readonly|record|ref|return|sealed|set|static|string|struct|switch|this|throw|true|try|using|var|void|while)\b")]
        private static partial Regex KeywordPattern();

        [GeneratedRegex(@"\b\d+(\.\d+)?\b")]
        private static partial Regex NumberPattern();
    }
}

#pragma warning disable CA1812 // Instantiated by the composition container.

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Keyword)]
[Name("Demo/Keyword")]
[UserVisible(true)]
[Order(After = Priority.Default)]
public sealed class DemoKeywordFormat : ClassificationFormatDefinition
{
    public DemoKeywordFormat()
    {
        DisplayName = "Keyword";
        ForegroundColor = Color.FromRgb(0x56, 0x9C, 0xD6);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Comment)]
[Name("Demo/Comment")]
[UserVisible(true)]
[Order(After = Priority.Default)]
public sealed class DemoCommentFormat : ClassificationFormatDefinition
{
    public DemoCommentFormat()
    {
        DisplayName = "Comment";
        ForegroundColor = Color.FromRgb(0x6A, 0x99, 0x55);
        IsItalic = true;
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.String)]
[Name("Demo/String")]
[UserVisible(true)]
[Order(After = Priority.Default)]
public sealed class DemoStringFormat : ClassificationFormatDefinition
{
    public DemoStringFormat()
    {
        DisplayName = "String";
        ForegroundColor = Color.FromRgb(0xCE, 0x91, 0x78);
    }
}

[Export(typeof(EditorFormatDefinition))]
[ClassificationType(ClassificationTypeNames = PredefinedClassificationTypeNames.Number)]
[Name("Demo/Number")]
[UserVisible(true)]
[Order(After = Priority.Default)]
public sealed class DemoNumberFormat : ClassificationFormatDefinition
{
    public DemoNumberFormat()
    {
        DisplayName = "Number";
        ForegroundColor = Color.FromRgb(0xB5, 0xCE, 0xA8);
    }
}

#pragma warning restore CA1812
