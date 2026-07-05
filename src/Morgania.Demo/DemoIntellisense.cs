namespace Microsoft.VisualStudio.Demo;

using System.Collections.Immutable;
using System.Composition;
using System.Text.RegularExpressions;

using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

/// <summary>
/// The demo's toy language service for the M6 IntelliSense tour: word completion over the
/// document (Ctrl+Space), Quick Info on hover, and signature help for call-looking text
/// (triggered by typing '(' or Ctrl+Shift+Space). Real language smarts arrive with Roslyn;
/// this only exercises the brokers and presenters.
/// </summary>
internal static partial class DemoWords
{
    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]{2,}")]
    public static partial Regex Identifier { get; }

    public static SnapshotSpan WordSpanAt(SnapshotPoint point)
    {
        static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';
        var snapshot = point.Snapshot;
        int start = point.Position;
        while (start > 0 && IsWordChar(snapshot[start - 1]))
        {
            start--;
        }

        int end = point.Position;
        while (end < snapshot.Length && IsWordChar(snapshot[end]))
        {
            end++;
        }

        return new SnapshotSpan(snapshot, start, end - start);
    }
}

[Export(typeof(IAsyncCompletionSourceProvider))]
[Name("demo completion")]
[ContentType("code")]
public sealed class DemoCompletionSourceProvider : IAsyncCompletionSourceProvider
{
    public IAsyncCompletionSource GetOrCreate(ITextView textView)
        => textView.Properties.GetOrCreateSingletonProperty(
            typeof(DemoCompletionSource),
            static () => new DemoCompletionSource());
}

internal sealed class DemoCompletionSource : IAsyncCompletionSource
{
    private static readonly string[] Keywords =
        ["namespace", "public", "private", "static", "class", "struct", "interface", "return", "string", "double", "int", "bool", "void", "var", "new", "true", "false"];

    private static readonly CompletionFilter KeywordFilter = new("Keywords", "K", new ImageElement(default));
    private static readonly CompletionFilter IdentifierFilter = new("Identifiers", "I", new ImageElement(default));

    public CompletionStartData InitializeCompletion(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
        => new(CompletionParticipation.ProvidesItems, DemoWords.WordSpanAt(triggerLocation));

    public Task<CompletionContext> GetCompletionContextAsync(
        IAsyncCompletionSession session,
        CompletionTrigger trigger,
        SnapshotPoint triggerLocation,
        SnapshotSpan applicableToSpan,
        CancellationToken token)
    {
        // Every identifier in the document plus a keyword list — the flavor of a word
        // completion provider, enough to see filtering, selection, and commit at work.
        var words = new SortedSet<string>(Keywords, StringComparer.Ordinal);
        var identifiers = new SortedSet<string>(StringComparer.Ordinal);
        foreach (Match match in DemoWords.Identifier.Matches(triggerLocation.Snapshot.GetText()))
        {
            identifiers.Add(match.Value);
        }

        var items = ImmutableArray.CreateBuilder<CompletionItem>();
        foreach (string keyword in Keywords)
        {
            items.Add(new CompletionItem(keyword, this, new ImageElement(default), [KeywordFilter], suffix: "keyword"));
        }

        foreach (string identifier in identifiers)
        {
            items.Add(new CompletionItem(identifier, this, new ImageElement(default), [IdentifierFilter]));
        }

        return Task.FromResult(new CompletionContext(items.ToImmutable()));
    }

    public Task<object> GetDescriptionAsync(IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
        => Task.FromResult<object>(new ClassifiedTextElement(
            new ClassifiedTextRun("keyword", item.DisplayText),
            new ClassifiedTextRun("text", " — a word from the demo document.")));
}

[Export(typeof(IAsyncQuickInfoSourceProvider))]
[Name("demo quick info")]
[ContentType("code")]
[Order]
public sealed class DemoQuickInfoSourceProvider : IAsyncQuickInfoSourceProvider
{
    public IAsyncQuickInfoSource TryCreateQuickInfoSource(ITextBuffer textBuffer) => new DemoQuickInfoSource(textBuffer);
}

internal sealed class DemoQuickInfoSource(ITextBuffer buffer) : IAsyncQuickInfoSource
{
    public Task<QuickInfoItem?> GetQuickInfoItemAsync(IAsyncQuickInfoSession session, CancellationToken cancellationToken)
    {
        if (session.GetTriggerPoint(buffer.CurrentSnapshot) is not { } point)
        {
            return Task.FromResult<QuickInfoItem?>(null);
        }

        var wordSpan = DemoWords.WordSpanAt(point);
        if (wordSpan.IsEmpty)
        {
            return Task.FromResult<QuickInfoItem?>(null);
        }

        string word = wordSpan.GetText();
        int occurrences = DemoWords.Identifier.Matches(point.Snapshot.GetText())
            .Count(match => match.Value == word);
        var content = new ContainerElement(
            ContainerElementStyle.Stacked,
            new ClassifiedTextElement(
                new ClassifiedTextRun("keyword", word, ClassifiedTextRunStyle.Bold),
                new ClassifiedTextRun("text", $"  (line {point.GetContainingLine().LineNumber + 1})")),
            new ClassifiedTextElement(
                new ClassifiedTextRun("comment", occurrences > 0
                    ? $"{occurrences} occurrence(s) in this document."
                    : "Hover a word to see Quick Info — content flows through the Modern ToolTip API.")));
        var applicableToSpan = point.Snapshot.CreateTrackingSpan(wordSpan, SpanTrackingMode.EdgeInclusive);
        return Task.FromResult<QuickInfoItem?>(new QuickInfoItem(applicableToSpan, content));
    }

    public void Dispose()
    {
    }
}

[Export(typeof(ISignatureHelpSourceProvider))]
[Name("demo signature help")]
[ContentType("code")]
[Order]
public sealed class DemoSignatureHelpSourceProvider : ISignatureHelpSourceProvider
{
    public ISignatureHelpSource TryCreateSignatureHelpSource(ITextBuffer textBuffer) => new DemoSignatureHelpSource(textBuffer);
}

internal sealed class DemoSignatureHelpSource(ITextBuffer buffer) : ISignatureHelpSource
{
    public void AugmentSignatureHelpSession(ISignatureHelpSession session, IList<ISignature> signatures)
    {
        if (session.GetTriggerPoint(buffer.CurrentSnapshot) is not { } point)
        {
            return;
        }

        // A call looks like "name(" before the trigger point on the same line.
        var line = point.GetContainingLine();
        string lineText = line.GetText();
        int caretColumn = point.Position - line.Start.Position;
        int open = lineText.LastIndexOf('(', Math.Max(0, Math.Min(caretColumn, lineText.Length) - 1));
        if (open <= 0)
        {
            return;
        }

        int nameStart = open;
        while (nameStart > 0 && (char.IsLetterOrDigit(lineText[nameStart - 1]) || lineText[nameStart - 1] == '_'))
        {
            nameStart--;
        }

        if (nameStart == open)
        {
            return;
        }

        string name = lineText[nameStart..open];
        var applicableToSpan = point.Snapshot.CreateTrackingSpan(
            line.Start.Position + nameStart, line.Length - nameStart, SpanTrackingMode.EdgeInclusive);
        int currentParameter = lineText[open..Math.Min(caretColumn, lineText.Length)].Count(static c => c == ',');
        signatures.Add(DemoSignature.Create(
            applicableToSpan,
            $"{name}(string text)",
            "Demo overload with a single parameter.",
            Math.Min(currentParameter, 0),
            ("text", "The text argument.")));
        signatures.Add(DemoSignature.Create(
            applicableToSpan,
            $"{name}(string text, int count)",
            "Demo overload with two parameters.",
            Math.Min(currentParameter, 1),
            ("text", "The text argument."),
            ("count", "How many times.")));
    }

    public ISignature? GetBestMatch(ISignatureHelpSession session) => session.Signatures.FirstOrDefault();

    public void Dispose()
    {
    }
}

internal sealed class DemoSignature : ISignature
{
    private IParameter? _currentParameter;

    public static DemoSignature Create(
        ITrackingSpan applicableToSpan,
        string content,
        string documentation,
        int currentParameterIndex,
        params (string Name, string Documentation)[] parameters)
    {
        var signature = new DemoSignature
        {
            ApplicableToSpan = applicableToSpan,
            Content = content,
            PrettyPrintedContent = content,
            Documentation = documentation,
        };
        var list = new List<IParameter>();
        foreach (var (name, documentationText) in parameters)
        {
            int start = content.IndexOf(name, StringComparison.Ordinal);
            list.Add(new DemoParameter(signature, name, documentationText, new Span(start, name.Length)));
        }

        signature.Parameters = new System.Collections.ObjectModel.ReadOnlyCollection<IParameter>(list);
        signature._currentParameter = currentParameterIndex >= 0 && currentParameterIndex < list.Count
            ? list[currentParameterIndex]
            : list.FirstOrDefault();
        return signature;
    }

    public ITrackingSpan ApplicableToSpan { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;

    public string PrettyPrintedContent { get; private set; } = string.Empty;

    public string Documentation { get; private set; } = string.Empty;

    public System.Collections.ObjectModel.ReadOnlyCollection<IParameter> Parameters { get; private set; } = new([]);

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

internal sealed class DemoParameter(ISignature signature, string name, string documentation, Span locus) : IParameter
{
    public ISignature Signature => signature;

    public string Name => name;

    public string Documentation => documentation;

    public Span Locus => locus;

    public Span PrettyPrintedLocus => locus;
}

/// <summary>
/// Keyboard/typing gestures for the IntelliSense tour. The Modern Commanding chain is the
/// real dispatch surface in the Roslyn tier; until then the demo drives the
/// brokers directly: Ctrl+Space completion, Tab/Enter commit, Esc dismiss, arrows navigate,
/// '(' or Ctrl+Shift+Space signature help (arrows cycle overloads), hover Quick Info.
/// </summary>
[Export(typeof(IWpfTextViewCreationListener))]
[ContentType("code")]
[TextViewRole(PredefinedTextViewRoles.Interactive)]
public sealed class DemoIntellisenseKeyHandler : IWpfTextViewCreationListener
{
    private readonly IAsyncCompletionBroker _completionBroker;
    private readonly ISignatureHelpBroker _signatureHelpBroker;

    [ImportingConstructor]
    public DemoIntellisenseKeyHandler(IAsyncCompletionBroker completionBroker, ISignatureHelpBroker signatureHelpBroker)
    {
        _completionBroker = completionBroker;
        _signatureHelpBroker = signatureHelpBroker;
    }

    public void TextViewCreated(IWpfTextView textView)
    {
        // Tunneling handlers run before the editor's own key handling, so completion can
        // claim Enter/Tab/arrows while a session is open.
        textView.VisualElement.AddHandler(
            InputElement.KeyDownEvent,
            (_, e) => OnKeyDown(textView, e),
            RoutingStrategies.Tunnel);
        textView.VisualElement.AddHandler(
            InputElement.TextInputEvent,
            (_, e) => OnTextInput(textView, e),
            RoutingStrategies.Tunnel);
    }

    private void OnKeyDown(IWpfTextView view, KeyEventArgs e)
    {
        if (view.IsClosed)
        {
            return;
        }

        bool command = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
        var completionSession = _completionBroker.GetSession(view);
        bool completionActive = completionSession is { IsDismissed: false };
        bool signatureHelpActive = _signatureHelpBroker.IsSignatureHelpActive(view);

        if (e.Key == Key.Space && command && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            _signatureHelpBroker.TriggerSignatureHelp(view);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Space && command)
        {
            TriggerCompletion(view, CompletionTriggerReason.Invoke);
            e.Handled = true;
            return;
        }

        if (completionActive)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    completionSession!.Dismiss();
                    e.Handled = true;
                    return;
                case Key.Down when completionSession is IAsyncCompletionSessionOperations down:
                    down.SelectDown();
                    e.Handled = true;
                    return;
                case Key.Up when completionSession is IAsyncCompletionSessionOperations up:
                    up.SelectUp();
                    e.Handled = true;
                    return;
                case Key.PageDown when completionSession is IAsyncCompletionSessionOperations pageDown:
                    pageDown.SelectPageDown();
                    e.Handled = true;
                    return;
                case Key.PageUp when completionSession is IAsyncCompletionSessionOperations pageUp:
                    pageUp.SelectPageUp();
                    e.Handled = true;
                    return;
                case Key.Tab:
                case Key.Enter:
                    completionSession!.Commit(default, CancellationToken.None);
                    completionSession.Dismiss();
                    e.Handled = true;
                    return;
            }
        }
        else if (signatureHelpActive)
        {
            var session = _signatureHelpBroker.GetSessions(view)[0];
            switch (e.Key)
            {
                case Key.Escape:
                    session.Dismiss();
                    e.Handled = true;
                    return;
                case Key.Down or Key.Up when session.Signatures.Count > 1:
                    int index = session.Signatures.IndexOf(session.SelectedSignature);
                    int next = e.Key == Key.Down ? (index + 1) % session.Signatures.Count : (index - 1 + session.Signatures.Count) % session.Signatures.Count;
                    session.SelectedSignature = session.Signatures[next];
                    e.Handled = true;
                    return;
            }
        }
    }

    private void OnTextInput(IWpfTextView view, TextInputEventArgs e)
    {
        if (view.IsClosed || string.IsNullOrEmpty(e.Text))
        {
            return;
        }

        char typed = e.Text[0];

        // Let the editor insert the character first, then react to the new state.
        Dispatcher.UIThread.Post(() =>
        {
            if (view.IsClosed)
            {
                return;
            }

            if (typed == '(')
            {
                _signatureHelpBroker.TriggerSignatureHelp(view);
            }
            else if (typed == ')')
            {
                _signatureHelpBroker.DismissAllSessions(view);
            }
            else if (char.IsLetterOrDigit(typed) || typed == '_')
            {
                // Keep an open completion session filtering as the user types.
                if (_completionBroker.GetSession(view) is { IsDismissed: false })
                {
                    TriggerCompletion(view, CompletionTriggerReason.Insertion);
                }
            }
        }, DispatcherPriority.Input);
    }

    private void TriggerCompletion(IWpfTextView view, CompletionTriggerReason reason)
    {
        var caret = view.Caret.Position.BufferPosition;
        var trigger = new CompletionTrigger(reason, caret.Snapshot);
        var session = _completionBroker.TriggerCompletion(view, trigger, caret, CancellationToken.None);
        session?.OpenOrUpdate(trigger, caret, CancellationToken.None);
    }
}
