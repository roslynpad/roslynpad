using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Composition;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.IntellisenseTests;

/// <summary>
/// The scripted fake language service of the M6 acceptance: deterministic completion,
/// Quick Info, and signature help sources that drive the real brokers.
/// </summary>
public static class FakeLanguage
{
    public static readonly CompletionFilter MethodFilter = new("Methods", "M", new ImageElement(default));
    public static readonly CompletionFilter FieldFilter = new("Fields", "F", new ImageElement(default));

    public static readonly string[] Words = ["Morgania", "Morning", "Morsel", "Zebra"];

    /// <summary>Set by tests to make the completion source request suggestion mode.</summary>
    public static bool UseSuggestionMode { get; set; }

    internal static SnapshotSpan GetWordSpanAt(SnapshotPoint point)
    {
        int start = point.Position;
        var snapshot = point.Snapshot;
        while (start > 0 && char.IsLetter(snapshot[start - 1]))
        {
            start--;
        }

        int end = point.Position;
        while (end < snapshot.Length && char.IsLetter(snapshot[end]))
        {
            end++;
        }

        return new SnapshotSpan(snapshot, start, end - start);
    }
}

[Export(typeof(IAsyncCompletionSourceProvider))]
[Name("fake completion source")]
[ContentType("text")]
public sealed class FakeCompletionSourceProvider : IAsyncCompletionSourceProvider
{
    public IAsyncCompletionSource GetOrCreate(ITextView textView)
        => textView.Properties.GetOrCreateSingletonProperty(
            typeof(FakeCompletionSource),
            static () => new FakeCompletionSource());
}

internal sealed class FakeCompletionSource : IAsyncCompletionSource
{
    public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        => new(CompletionParticipation.ProvidesItems, FakeLanguage.GetWordSpanAt(triggerLocation));

    public Task<CompletionContext> GetCompletionContextAsync(
        IAsyncCompletionSession session,
        CompletionTrigger trigger,
        SnapshotPoint triggerLocation,
        SnapshotSpan applicableToSpan,
        CancellationToken token)
    {
        var items = FakeLanguage.Words
            .Select(word => new CompletionItem(
                word,
                this,
                new ImageElement(default),
                [word[0] == 'M' ? FakeLanguage.MethodFilter : FakeLanguage.FieldFilter],
                suffix: word.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            .ToImmutableArray();
        var context = FakeLanguage.UseSuggestionMode
            ? new CompletionContext(items, suggestionItemOptions: new SuggestionItemOptions("(new symbol)", "Type a new name"))
            : new CompletionContext(items);
        return Task.FromResult(context);
    }

    public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        => Task.FromResult<object>(new ClassifiedTextElement(new ClassifiedTextRun("text", $"Docs for {item.DisplayText}")));
}

[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name("fake quick info source")]
[ContentType("text")]
[Order]
public sealed class FakeQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
{
    public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) => new FakeQuickInfoSource(textBuffer);
}

internal sealed class FakeQuickInfoSource(ITextBuffer buffer) : IAsyncQuickInfoSource
{
    public Task<QuickInfoItem?> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
    {
        if (session.GetTriggerPoint(buffer.CurrentSnapshot) is not { } point)
        {
            return Task.FromResult<QuickInfoItem?>(null);
        }

        var wordSpan = FakeLanguage.GetWordSpanAt(point);
        if (wordSpan.IsEmpty)
        {
            return Task.FromResult<QuickInfoItem?>(null);
        }

        var applicableToSpan = point.Snapshot.CreateTrackingSpan(wordSpan, SpanTrackingMode.EdgeInclusive);
        var content = new ContainerElement(
            ContainerElementStyle.Stacked,
            new ClassifiedTextElement(new ClassifiedTextRun("text", $"Info about {wordSpan.GetText()}")),
            new ClassifiedTextElement(new ClassifiedTextRun("text", "From the fake language service.")));
        return Task.FromResult<QuickInfoItem?>(new QuickInfoItem(applicableToSpan, content));
    }

    public void Dispose()
    {
    }
}

[Export(typeof(ISignatureHelpSourceProvider))]
[Name("fake signature help source")]
[ContentType("text")]
[Order]
public sealed class FakeSignatureHelpSourceProvider : ISignatureHelpSourceProvider
{
    public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer) => new FakeSignatureHelpSource(textBuffer);
}

internal sealed class FakeSignatureHelpSource(ITextBuffer buffer) : ISignatureHelpSource
{
    public static int DisposeCount;

    public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
    {
        if (session.GetTriggerPoint(buffer.CurrentSnapshot) is not { } point)
        {
            return;
        }

        var line = point.GetContainingLine();
        var applicableToSpan = point.Snapshot.CreateTrackingSpan(
            Span.FromBounds(line.Start, line.End), SpanTrackingMode.EdgeInclusive);
        signatures.Add(FakeSignature.Create(
            applicableToSpan,
            "Greet(string name)",
            "Greets someone once.",
            ("name", "Who to greet.")));
        signatures.Add(FakeSignature.Create(
            applicableToSpan,
            "Greet(string name, int times)",
            "Greets someone repeatedly.",
            ("name", "Who to greet."),
            ("times", "How many times.")));
    }

    // The second overload is always the "best match": the acceptance asserts the session
    // routed selection through the highest-priority source's answer.
    public ISignature? GetBestMatch(ISignatureHelpSession session)
        => session.Signatures.Count > 1 ? session.Signatures[1] : null;

    public void Dispose() => DisposeCount++;
}

internal sealed class FakeSignature : ISignature
{
    private IParameter? _currentParameter;

    public static FakeSignature Create(ITrackingSpan applicableToSpan, string content, string documentation, params (string Name, string Documentation)[] parameters)
    {
        var signature = new FakeSignature
        {
            ApplicableToSpan = applicableToSpan,
            Content = content,
            PrettyPrintedContent = content,
            Documentation = documentation,
        };
        var list = new List<IParameter>();
        foreach (var (name, doc) in parameters)
        {
            int start = content.IndexOf(name, StringComparison.Ordinal);
            list.Add(new FakeParameter(signature, name, doc, new Span(start, name.Length)));
        }

        signature.Parameters = new ReadOnlyCollection<IParameter>(list);
        signature._currentParameter = list.FirstOrDefault();
        return signature;
    }

    public ITrackingSpan ApplicableToSpan { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;

    public string PrettyPrintedContent { get; private set; } = string.Empty;

    public string Documentation { get; private set; } = string.Empty;

    public ReadOnlyCollection<IParameter> Parameters { get; private set; } = new([]);

    public IParameter? CurrentParameter
    {
        get => _currentParameter;
        set
        {
            if (!ReferenceEquals(_currentParameter, value))
            {
                var previous = _currentParameter;
                _currentParameter = value;
                CurrentParameterChanged?.Invoke(this, new CurrentParameterChangedEventArgs(previous!, value!));
            }
        }
    }

    public event EventHandler<CurrentParameterChangedEventArgs>? CurrentParameterChanged;
}

internal sealed class FakeParameter(ISignature signature, string name, string documentation, Span locus) : IParameter
{
    public ISignature Signature => signature;

    public string Name => name;

    public string Documentation => documentation;

    public Span Locus => locus;

    public Span PrettyPrintedLocus => locus;
}
