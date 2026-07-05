using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.VisualStudio.Composition;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Morgania.CodeAnalysis.Editor;

namespace Morgania.Demo.EditorFeatures;

/// <summary>
/// Drives the real pipeline headlessly: waits for Roslyn's classification taggers to produce
/// tags, then invokes completion at a member-access position through the async completion
/// broker (editor broker → Roslyn completion source). Exits 0 on success.
/// </summary>
internal static class SmokeTest
{
    public static async Task RunAsync(
        IClassicDesktopStyleApplicationLifetime desktop,
        ExportProvider exportProvider,
        IWpfTextView view,
        ITextBuffer buffer)
    {
        int exitCode = 1;
        try
        {
            await VerifyClassificationAsync(exportProvider, view, buffer).ConfigureAwait(true);
            await VerifyCompletionAsync(exportProvider, view, buffer).ConfigureAwait(true);
            VerifyReturnKeyCommand(exportProvider, view, buffer);
            VerifyBraceCompletion(exportProvider, view, buffer);
            await VerifySnippetCommitAsync(exportProvider, view, buffer).ConfigureAwait(true);
            await VerifySignatureHelpAsync(exportProvider, view, buffer).ConfigureAwait(true);
            await VerifyDiagnosticsAsync(exportProvider, view, buffer).ConfigureAwait(true);
            await VerifyQuickFixAsync(exportProvider, view, buffer).ConfigureAwait(true);
            VerifyMultiCaret(exportProvider, view, buffer);
            await VerifyBraceMatchingAsync(exportProvider, view, buffer).ConfigureAwait(true);
            Console.WriteLine("SMOKE PASSED");
            exitCode = 0;
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"SMOKE FAILED: {e}").ConfigureAwait(true);
        }

        desktop.Shutdown(exitCode);
    }

    private static async Task VerifyClassificationAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var aggregatorFactory = exportProvider.GetExportedValue<IViewTagAggregatorFactoryService>();
        using var aggregator = aggregatorFactory.CreateTagAggregator<IClassificationTag>(view);

        for (var i = 0; i < 60; i++)
        {
            await Task.Delay(500).ConfigureAwait(true);
            var snapshot = buffer.CurrentSnapshot;
            var tags = aggregator
                .GetTags(new SnapshotSpan(snapshot, 0, snapshot.Length))
                .Select(tag => tag.Tag.ClassificationType.Classification)
                .Distinct()
                .ToList();

            // "keyword" comes from the syntactic classifier; "class name" only appears once
            // semantic classification over the Roslyn document is working.
            if (tags.Contains("keyword") && tags.Contains("class name"))
            {
                Console.WriteLine($"classification OK: {string.Join(", ", tags.Order())}");
                return;
            }

            if (i % 10 == 9)
            {
                Console.WriteLine($"waiting for classification; tags so far: {string.Join(", ", tags.Order())}");
            }
        }

        throw new TimeoutException("classification tags did not appear");
    }

    private static async Task VerifyCompletionAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var broker = exportProvider.GetExportedValue<IAsyncCompletionBroker>();

        var position = buffer.CurrentSnapshot.GetText().IndexOf("Console.", StringComparison.Ordinal) + "Console.".Length;
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, position));

        var caret = view.Caret.Position.BufferPosition;
        var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, caret.Snapshot);
        var session = broker.TriggerCompletion(view, trigger, caret, CancellationToken.None)
            ?? throw new InvalidOperationException("completion session did not start");

        session.OpenOrUpdate(trigger, caret, CancellationToken.None);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var items = (await Task.Run(() => session.GetComputedItems(cancellation.Token).Items.ToList()).ConfigureAwait(true));
        session.Dismiss();

        if (!items.Any(item => item.DisplayText == "WriteLine"))
        {
            throw new InvalidOperationException($"completion returned {items.Count} items without WriteLine");
        }

        Console.WriteLine($"completion OK: {items.Count} items at 'Console.' including WriteLine");
    }

    /// <summary>
    /// Presses Enter through the Modern Commanding chain (Roslyn format handler → editor
    /// completion/brace-completion handlers → editor operations with smart indent), the same
    /// path the keyboard bridge uses. Covers the coding-conventions option and smart indent.
    /// </summary>
    private static void VerifyReturnKeyCommand(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var commanding = exportProvider.GetExportedValue<IEditorCommandHandlerServiceFactory>().GetService(view);
        var operations = exportProvider.GetExportedValue<IEditorOperationsFactoryService>().GetEditorOperations(view);

        var snapshot = buffer.CurrentSnapshot;
        int lineCount = snapshot.LineCount;
        var position = snapshot.GetText().IndexOf("Greet()", StringComparison.Ordinal) + "Greet()".Length;
        view.Caret.MoveTo(new SnapshotPoint(snapshot, position));

        commanding.Execute(
            (v, b) => new ReturnKeyCommandArgs(v, b),
            () => operations.InsertNewLine());

        if (buffer.CurrentSnapshot.LineCount != lineCount + 1)
        {
            throw new InvalidOperationException("return key did not insert a new line through the command chain");
        }

        var caret = view.Caret.Position;
        var caretLine = caret.BufferPosition.GetContainingLine();
        int indent = caret.BufferPosition.Position - caretLine.Start.Position + caret.VirtualBufferPosition.VirtualSpaces;
        Console.WriteLine($"return key OK: caret on line {caretLine.LineNumber + 1}, indent {indent}");
    }

    /// <summary>
    /// Types '(' through the command chain after a method group, expecting the brace completion
    /// stack to insert the closing ')' and highlight it (session push renders the adornment).
    /// </summary>
    private static void VerifyBraceCompletion(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var commanding = exportProvider.GetExportedValue<IEditorCommandHandlerServiceFactory>().GetService(view);
        var operations = exportProvider.GetExportedValue<IEditorOperationsFactoryService>().GetEditorOperations(view);

        var lineStart = buffer.CurrentSnapshot.GetText().IndexOf("for (var i", StringComparison.Ordinal);
        lineStart = buffer.CurrentSnapshot.GetLineFromPosition(lineStart).Start.Position;
        const string statement = "        Name.ToString";
        buffer.Insert(lineStart, statement + "\n");
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, lineStart + statement.Length));

        commanding.Execute(
            (v, b) => new TypeCharCommandArgs(v, b, '('),
            () => operations.InsertText("("));

        var lineText = buffer.CurrentSnapshot.GetLineFromPosition(lineStart).GetText();
        if (!lineText.Contains("Name.ToString()", StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"brace completion did not insert the closing brace: '{lineText}'");
        }

        // Leave a valid statement behind for the later smoke steps.
        buffer.Insert(view.Caret.Position.BufferPosition.Position + 1, ";");
        Console.WriteLine($"brace completion OK: '{lineText.Trim()}'");
    }

    /// <summary>
    /// Commits the foreach *snippet* item (not the keyword) through the async completion session,
    /// which exercises Roslyn's CommitManager snippet path and the host's ILanguageServerSnippetExpander.
    /// </summary>
    private static async Task VerifySnippetCommitAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var broker = exportProvider.GetExportedValue<IAsyncCompletionBroker>();

        // Typing in the previous step may have left a completion session open on the view;
        // TriggerCompletion would return it instead of starting one at the new location.
        broker.GetSession(view)?.Dismiss();

        // Open an empty, indented statement line inside Greet's body and invoke completion there,
        // so the commit inserts at statement start with nothing to replace.
        var forPosition = buffer.CurrentSnapshot.GetText().IndexOf("for (var i", StringComparison.Ordinal);
        var lineStart = buffer.CurrentSnapshot.GetLineFromPosition(forPosition).Start.Position;
        buffer.Insert(lineStart, "        \n");
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, lineStart + 8));

        var caret = view.Caret.Position.BufferPosition;
        var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, caret.Snapshot);
        var session = broker.TriggerCompletion(view, trigger, caret, CancellationToken.None)
            ?? throw new InvalidOperationException("completion session did not start at statement position");

        session.OpenOrUpdate(trigger, caret, CancellationToken.None);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var items = await Task.Run(() => session.GetComputedItems(cancellation.Token).Items.ToList()).ConfigureAwait(true);

        var snippetItem = items.FirstOrDefault(item =>
                item.DisplayText == "foreach" &&
                CompletionItemData.TryGetData(item, out var data) &&
                SnippetCompletionItem.IsSnippet(data.RoslynItem))
            ?? throw new InvalidOperationException(
                $"no foreach snippet item among {items.Count} items: {string.Join(", ", items.Where(i => i.DisplayText.Contains("foreach", StringComparison.Ordinal)).Select(i => i.DisplayText))}");

        // The view repaints the inserted text only if a later layout *reformats* its lines once
        // classification arrives (pull-based GetTags cannot see stale paint, so watch layouts).
        var aggregatorFactory = exportProvider.GetExportedValue<IViewTagAggregatorFactoryService>();
        using var aggregator = aggregatorFactory.CreateTagAggregator<IClassificationTag>(view);
        var repainted = false;

        void OnLayoutChanged(object? sender, TextViewLayoutChangedEventArgs e)
        {
            var snapshot = buffer.CurrentSnapshot;
            var position = snapshot.GetText().IndexOf("foreach (var", StringComparison.Ordinal);
            if (position < 0 ||
                !e.NewOrReformattedLines.Any(line => line.ContainsBufferPosition(new SnapshotPoint(snapshot, position))))
            {
                return;
            }

            // Reformatted before the taggers produced the span's classification does not count.
            var tags = aggregator
                .GetTags(new SnapshotSpan(snapshot, position, "foreach".Length))
                .Select(tag => tag.Tag.ClassificationType.Classification);
            repainted |= tags.Contains("keyword - control");
        }

        view.LayoutChanged += OnLayoutChanged;
        try
        {
            ((IAsyncCompletionSessionOperations)session).SelectCompletionItem(snippetItem);
            session.Commit('\n', CancellationToken.None);

            if (!buffer.CurrentSnapshot.GetText().Contains("foreach (var", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("committing the foreach snippet did not expand it");
            }

            var selection = view.Selection.StreamSelectionSpan.GetText();
            Console.WriteLine($"snippet OK: foreach expanded, first placeholder selected: '{selection}'");

            for (var i = 0; i < 40 && !repainted; i++)
            {
                await Task.Delay(250).ConfigureAwait(true);
            }

            if (!repainted)
            {
                throw new TimeoutException("the view did not reformat the inserted snippet lines after classification");
            }

            Console.WriteLine("snippet classification OK: view reformatted foreach as control keyword");
        }
        finally
        {
            view.LayoutChanged -= OnLayoutChanged;
        }
    }

    /// <summary>
    /// Invokes signature help through the command chain (as the keyboard bridge does for
    /// Ctrl/Cmd+Shift+Space) with the caret inside a call's argument list, and expects
    /// Roslyn's controller to present a session through the editor's signature help broker.
    /// </summary>
    private static async Task VerifySignatureHelpAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var commanding = exportProvider.GetExportedValue<IEditorCommandHandlerServiceFactory>().GetService(view);
        var broker = exportProvider.GetExportedValue<Microsoft.VisualStudio.Language.Intellisense.ISignatureHelpBroker>();

        var position = buffer.CurrentSnapshot.GetText().IndexOf("Console.WriteLine(", StringComparison.Ordinal) + "Console.WriteLine(".Length;
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, position));

        var fellThrough = false;
        commanding.Execute((v, b) => new InvokeSignatureHelpCommandArgs(v, b), () => fellThrough = true);
        if (fellThrough)
        {
            throw new InvalidOperationException("no command handler took InvokeSignatureHelp");
        }

        for (var i = 0; i < 40; i++)
        {
            await Task.Delay(250).ConfigureAwait(true);
            if (broker.IsSignatureHelpActive(view))
            {
                var session = broker.GetSessions(view)[0];
                var signature = session.SelectedSignature ?? session.Signatures.FirstOrDefault();
                Console.WriteLine($"signature help OK: {session.Signatures.Count} overloads, selected: '{signature?.Content}'");

                VerifySignatureHelpColorized(session);

                // Arrow keys must cycle overloads through the key bridge (raise real key events
                // so the bridge's tunneling handler routes them, as the VS shell would).
                var before = session.SelectedSignature;
                RaiseKey(view, Avalonia.Input.Key.Down);
                if (ReferenceEquals(session.SelectedSignature, before))
                {
                    throw new InvalidOperationException("Down did not cycle to the next signature help overload");
                }

                RaiseKey(view, Avalonia.Input.Key.Up);
                if (!ReferenceEquals(session.SelectedSignature, before))
                {
                    throw new InvalidOperationException("Up did not cycle back to the previous signature help overload");
                }

                Console.WriteLine("signature help arrows OK: Down/Up cycle overloads");
                session.Dismiss();
                return;
            }
        }

        throw new TimeoutException("signature help session did not become active");
    }

    /// <summary>
    /// The popup's signature line must be classified: Morgania's presenter routes the content
    /// through a "CSharp Signature Help" buffer that Roslyn's signature help classifier colors.
    /// Checks the rendered runs for the keyword color on 'void'.
    /// </summary>
    private static void VerifySignatureHelpColorized(Microsoft.VisualStudio.Language.Intellisense.ISignatureHelpSession session)
    {
        var popup = (Microsoft.VisualStudio.Language.Intellisense.IPopupIntellisensePresenter)session.Presenter;
        var signatureBlock = ((Avalonia.Controls.Border)popup.SurfaceElement).GetVisualDescendants()
            .OfType<Avalonia.Controls.TextBlock>()
            .First();
        var runs = signatureBlock.Inlines!.OfType<Avalonia.Controls.Documents.Run>().ToList();

        var keywordColor = Avalonia.Media.Color.FromRgb(0x56, 0x9C, 0xD6);
        var voidRun = runs.FirstOrDefault(run => run.Text == "void");
        if (voidRun is null || (voidRun.Foreground as Avalonia.Media.ISolidColorBrush)?.Color != keywordColor)
        {
            var rendered = string.Join(" | ", runs.Select(run => $"'{run.Text}' {(run.Foreground as Avalonia.Media.ISolidColorBrush)?.Color}"));
            throw new InvalidOperationException($"signature help content is not classified; runs: {rendered}");
        }

        Console.WriteLine($"signature help colors OK: {runs.Count} classified runs, 'void' in keyword color");
    }

    /// <summary>
    /// Introduces a compile error and waits for the pull-diagnostics squiggle pipeline: the
    /// demo's error tagger (over Roslyn's AbstractDiagnosticsTaggerProvider machinery) must
    /// produce an error tag over the bad identifier, and the squiggle adornment manager must
    /// draw into the view's Squiggle layer.
    /// </summary>
    private static async Task VerifyDiagnosticsAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var lineStart = buffer.CurrentSnapshot.GetText().IndexOf("for (var i", StringComparison.Ordinal);
        lineStart = buffer.CurrentSnapshot.GetLineFromPosition(lineStart).Start.Position;
        buffer.Insert(lineStart, "        Conosle.WriteLine(\"oops\");\n");

        var aggregatorFactory = exportProvider.GetExportedValue<IViewTagAggregatorFactoryService>();
        using var aggregator = aggregatorFactory.CreateTagAggregator<IErrorTag>(view);

        for (var i = 0; i < 120; i++)
        {
            await Task.Delay(500).ConfigureAwait(true);

            var snapshot = buffer.CurrentSnapshot;
            var errorPosition = snapshot.GetText().IndexOf("Conosle", StringComparison.Ordinal);
            var tags = aggregator
                .GetTags(new SnapshotSpan(snapshot, errorPosition, "Conosle".Length))
                .Where(tag => tag.Tag.ErrorType == Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.SyntaxError)
                .ToList();
            if (tags.Count == 0)
            {
                continue;
            }

            var squiggleLayer = view.GetAdornmentLayer(PredefinedAdornmentLayers.Squiggle);
            for (var j = 0; j < 20 && squiggleLayer.IsEmpty; j++)
            {
                await Task.Delay(250).ConfigureAwait(true);
            }

            if (squiggleLayer.IsEmpty)
            {
                throw new TimeoutException("error tags are present but no squiggle adornment was drawn");
            }

            Console.WriteLine($"diagnostics OK: error squiggle over 'Conosle' ({tags[0].Tag.ToolTipContent}), {squiggleLayer.Elements.Count} squiggle adornments");
            return;
        }

        throw new TimeoutException("no error squiggle tag appeared over the bad identifier");
    }

    /// <summary>
    /// Opens the suggested-actions popup over the misspelled identifier with a real Ctrl+. key
    /// event (bridge → controller → Roslyn's suggested-actions source), invokes the spelling
    /// fix through the popup's Enter handling, and expects the code action edit to land in the
    /// buffer and the error squiggle to disappear.
    /// </summary>
    private static async Task VerifyQuickFixAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var controller = exportProvider.GetExportedValue<SuggestedActionsControllerFactory>().GetOrCreate(view);

        var errorPosition = buffer.CurrentSnapshot.GetText().IndexOf("Conosle", StringComparison.Ordinal);
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, errorPosition + 3));
        // The snippet step left the placeholder selected; the controller would use that
        // selection (in the foreach) as the actions range instead of the caret.
        view.Selection.Clear();

        // The caret move schedules a light bulb update; the error on the caret line must
        // produce the critical (ErrorFix) icon.
        var errorFix = Microsoft.VisualStudio.Language.Intellisense.PredefinedSuggestedActionCategoryNames.ErrorFix;
        for (var i = 0; i < 120 && controller.LightBulbCategory != errorFix; i++)
        {
            await Task.Delay(500).ConfigureAwait(true);
        }

        if (controller.LightBulbCategory != errorFix)
        {
            throw new TimeoutException($"the error light bulb did not appear at the misspelled identifier (category: {controller.LightBulbCategory ?? "none"})");
        }

        Console.WriteLine($"light bulb OK: '{controller.LightBulbCategory}' icon at the error line");

        RaiseKey(view, Avalonia.Input.Key.OemPeriod, Avalonia.Input.KeyModifiers.Control);
        for (var i = 0; i < 120 && !controller.IsOpen; i++)
        {
            await Task.Delay(500).ConfigureAwait(true);
        }

        if (!controller.IsOpen)
        {
            throw new TimeoutException("Ctrl+. did not open the suggested actions popup");
        }

        var actions = controller.Actions.Select(action => action.DisplayText).ToList();
        var fixIndex = actions.FindIndex(text => text.Contains("'Console'", StringComparison.Ordinal));
        if (fixIndex < 0)
        {
            throw new InvalidOperationException($"no spelling fix among {actions.Count} actions: {string.Join("; ", actions)}");
        }

        Console.WriteLine($"suggested actions OK: {actions.Count} actions, including '{actions[fixIndex]}'");

        if (!controller.Invoke(fixIndex))
        {
            throw new InvalidOperationException("the spelling fix could not be invoked");
        }

        if (controller.IsOpen)
        {
            throw new InvalidOperationException("invoking the fix did not dismiss the menu");
        }

        for (var i = 0; i < 120; i++)
        {
            await Task.Delay(500).ConfigureAwait(true);
            if (buffer.CurrentSnapshot.GetText().Contains("Console.WriteLine(\"oops\")", StringComparison.Ordinal))
            {
                Console.WriteLine("quick fix OK: 'Conosle' replaced with 'Console' in the buffer");
                await VerifySquigglesClearedAsync(exportProvider, view, buffer).ConfigureAwait(true);
                return;
            }
        }

        throw new TimeoutException("the spelling fix did not change the buffer");
    }

    private static async Task VerifySquigglesClearedAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var aggregatorFactory = exportProvider.GetExportedValue<IViewTagAggregatorFactoryService>();
        using var aggregator = aggregatorFactory.CreateTagAggregator<IErrorTag>(view);

        for (var i = 0; i < 120; i++)
        {
            var snapshot = buffer.CurrentSnapshot;
            var errorTags = aggregator
                .GetTags(new SnapshotSpan(snapshot, 0, snapshot.Length))
                .Where(tag => tag.Tag.ErrorType == Microsoft.VisualStudio.Text.Adornments.PredefinedErrorTypeNames.SyntaxError)
                .ToList();
            if (errorTags.Count == 0)
            {
                Console.WriteLine("squiggle cleanup OK: no error tags remain after the fix");
                await VerifyLightBulbClearedAsync(view, exportProvider).ConfigureAwait(true);
                return;
            }

            await Task.Delay(500).ConfigureAwait(true);
        }

        throw new TimeoutException("error squiggles did not clear after applying the fix");
    }

    /// <summary>The buffer edit reschedules the light bulb; it must stop showing ErrorFix.</summary>
    private static async Task VerifyLightBulbClearedAsync(IWpfTextView view, ExportProvider exportProvider)
    {
        var controller = exportProvider.GetExportedValue<SuggestedActionsControllerFactory>().GetOrCreate(view);
        var errorFix = Microsoft.VisualStudio.Language.Intellisense.PredefinedSuggestedActionCategoryNames.ErrorFix;

        for (var i = 0; i < 120; i++)
        {
            if (controller.LightBulbCategory != errorFix)
            {
                Console.WriteLine($"light bulb cleanup OK: icon after the fix: '{controller.LightBulbCategory ?? "none"}'");
                return;
            }

            await Task.Delay(500).ConfigureAwait(true);
        }

        throw new TimeoutException("the light bulb still shows ErrorFix after the fix was applied");
    }

    /// <summary>
    /// Ctrl+Alt+Down through the key chain adds a caret on the next line (the view keymap's
    /// add-caret gesture; the commanding bridge lets non-plain arrows fall through), and a
    /// typed character lands at every caret via the editor operations' multi-selection support.
    /// </summary>
    private static void VerifyMultiCaret(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var broker = view.GetMultiSelectionBroker();
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, 0));

        RaiseKey(view, Avalonia.Input.Key.Down, Avalonia.Input.KeyModifiers.Control | Avalonia.Input.KeyModifiers.Alt);
        if (broker.AllSelections.Count != 2)
        {
            throw new InvalidOperationException($"expected 2 carets after Ctrl+Alt+Down, got {broker.AllSelections.Count}");
        }

        view.VisualElement.RaiseEvent(new Avalonia.Input.TextInputEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.TextInputEvent,
            Text = "x",
            Source = view.VisualElement,
        });

        exportProvider.GetExportedValue<IAsyncCompletionBroker>().GetSession(view)?.Dismiss();

        var snapshot = buffer.CurrentSnapshot;
        var firstLine = snapshot.GetLineFromLineNumber(0).GetText();
        var secondLine = snapshot.GetLineFromLineNumber(1).GetText();
        if (!firstLine.StartsWith('x') || !secondLine.StartsWith('x'))
        {
            throw new InvalidOperationException(
                $"typing with 2 carets did not edit both lines: '{firstLine}' / '{secondLine}'");
        }

        broker.ClearSecondarySelections();
        Console.WriteLine("multi-caret OK: Ctrl+Alt+Down added a caret; typing edited both lines");
    }

    /// <summary>
    /// Puts the caret on the for-loop's open brace and waits for Roslyn's
    /// BraceHighlightingViewTaggerProvider to tag both braces ("brace matching" text-marker
    /// tags) and for TextMarkerAdornmentManager to draw them on the TextMarker layer; then
    /// presses Ctrl+] through the key chain (bridge → GoToMatchingBraceCommandHandler) and
    /// expects the caret to land after the matching close brace.
    /// </summary>
    private static async Task VerifyBraceMatchingAsync(ExportProvider exportProvider, IWpfTextView view, ITextBuffer buffer)
    {
        var openBrace = buffer.CurrentSnapshot.GetText().IndexOf("for (var i", StringComparison.Ordinal);
        openBrace = buffer.CurrentSnapshot.GetText().IndexOf('{', openBrace);
        view.Caret.MoveTo(new SnapshotPoint(buffer.CurrentSnapshot, openBrace));

        var aggregatorFactory = exportProvider.GetExportedValue<IViewTagAggregatorFactoryService>();
        using var aggregator = aggregatorFactory.CreateTagAggregator<ITextMarkerTag>(view);

        List<SnapshotSpan> braceSpans = [];
        for (var i = 0; i < 60 && braceSpans.Count < 2; i++)
        {
            await Task.Delay(500).ConfigureAwait(true);
            var snapshot = buffer.CurrentSnapshot;
            braceSpans = aggregator
                .GetTags(new SnapshotSpan(snapshot, 0, snapshot.Length))
                .Where(tag => tag.Tag.Type == "brace matching")
                .SelectMany(tag => tag.Span.GetSpans(snapshot))
                .OrderBy(span => span.Start.Position)
                .ToList();
        }

        if (braceSpans.Count != 2 || braceSpans[0].GetText() != "{" || braceSpans[1].GetText() != "}")
        {
            throw new TimeoutException(
                $"expected open+close brace-matching tags, got {braceSpans.Count}: {string.Join(", ", braceSpans.Select(span => $"'{span.GetText()}'@{span.Start.Position}"))}");
        }

        // The marker manager redraws on a posted callback after the tags change; give it time.
        var markerLayer = view.GetAdornmentLayer(PredefinedAdornmentLayers.TextMarker);
        for (var i = 0; i < 20 && markerLayer.Elements.Count < 2; i++)
        {
            await Task.Delay(250).ConfigureAwait(true);
        }

        if (markerLayer.Elements.Count < 2)
        {
            throw new InvalidOperationException(
                $"expected the brace markers on the TextMarker layer, found {markerLayer.Elements.Count} adornments");
        }

        RaiseKey(view, Avalonia.Input.Key.OemCloseBrackets, Avalonia.Input.KeyModifiers.Control);

        var expected = braceSpans[1].End.TranslateTo(buffer.CurrentSnapshot, PointTrackingMode.Positive);
        var caret = view.Caret.Position.BufferPosition;
        if (caret != expected)
        {
            throw new InvalidOperationException(
                $"Ctrl+] did not jump to the matching brace: caret at {caret.Position}, expected {expected.Position}");
        }

        Console.WriteLine("brace matching OK: both braces tagged and drawn; Ctrl+] jumped past the close brace");
    }

    private static void RaiseKey(IWpfTextView view, Avalonia.Input.Key key, Avalonia.Input.KeyModifiers modifiers = Avalonia.Input.KeyModifiers.None)
        => view.VisualElement.RaiseEvent(new Avalonia.Input.KeyEventArgs
        {
            RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers,
            Source = view.VisualElement,
        });
}
