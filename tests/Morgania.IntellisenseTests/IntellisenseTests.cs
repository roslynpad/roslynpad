using System.Collections.Immutable;

using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Threading;
using Avalonia.VisualTree;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Language.Intellisense.Implementation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.IntellisenseTests;

/// <summary>
/// The M6 acceptance: a scripted fake language service (this assembly's exports) drives
/// completion, Quick Info, and signature help through the real brokers headlessly, and the
/// presenters behave per the wiki specs (popup placement through space reservation, filters,
/// soft selection, suggestion mode, overload selection).
/// </summary>
[TestClass]
public sealed class IntellisenseTests
{
    /// <summary>Pumps the dispatcher until <paramref name="condition"/> holds (or fails the test).</summary>
    private static void PumpUntil(Func<bool> condition, string reason)
    {
        for (int i = 0; i < 200; i++)
        {
            Dispatcher.UIThread.RunJobs();
            if (condition())
            {
                return;
            }

            Thread.Sleep(5);
        }

        Assert.Fail($"Timed out pumping the dispatcher: {reason}");
    }

    private static TextBlock[] OverlayTextBlocks(IWpfTextView view)
        => OverlayLayer.GetOverlayLayer(view.VisualElement)!
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .ToArray();

    /// <summary>Text of a block whether it uses Text or classified-run Inlines.</summary>
    private static string BlockText(TextBlock block)
        => block.Text ?? string.Concat(
            (block.Inlines ?? []).OfType<Avalonia.Controls.Documents.Run>().Select(run => run.Text));

    [TestMethod]
    public async Task SpaceReservationPopupAgentDisplaysInTheOverlayAndReservesItsBounds()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("first line\nsecond line\n");
            try
            {
                var manager = view.GetSpaceReservationManager(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName);
                Assert.AreEqual("completion", manager.Name);

                var span = view.TextSnapshot.CreateTrackingSpan(0, 5, SpanTrackingMode.EdgeInclusive);
                var content = new Border { Width = 120.0, Height = 40.0 };
                var agent = manager.CreatePopupAgent(span, PopupStyles.None, content);
                manager.AddAgent(agent);
                Assert.AreEqual(1, manager.Agents.Count);

                // The refresh is queued (asynchronous per the contract); after it runs the
                // content is in the top-level's overlay, positioned below the first line.
                PumpUntil(() => content.Parent is not null, "popup agent attaches to the overlay");
                var overlay = OverlayLayer.GetOverlayLayer(view.VisualElement)!;
                Assert.IsTrue(overlay.Children.Contains(content));
                double top = Canvas.GetTop(content);
                var firstLine = view.TextViewLines[0];
                Assert.IsTrue(top >= firstLine.Bottom - 0.01, $"popup top {top} must be at/below the anchor line bottom {firstLine.Bottom}");

                Assert.IsTrue(manager.RemoveAgent(agent));
                Assert.IsFalse(overlay.Children.Contains(content), "removal hides the popup");
                Assert.AreEqual(0, manager.Agents.Count);
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task GetSpaceReservationManagerValidatesNames()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("text\n");
            try
            {
                Assert.ThrowsExactly<ArgumentNullException>(() => view.GetSpaceReservationManager(null!));
                Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => view.GetSpaceReservationManager("no such manager"));
                var first = view.GetSpaceReservationManager("quickinfo");
                Assert.AreSame(first, view.GetSpaceReservationManager("quickinfo"), "managers are per-view singletons");
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompletionPresenterRendersItemsFiltersSoftSelectionAndSuggestion()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("Mor\n");
            try
            {
                var presenter = new CompletionPresenter(
                    view,
                    IntellisenseTestHost.Container.GetExport<IViewElementFactoryService>(),
                    IntellisenseTestHost.Container.GetExport<Text.Classification.IEditorFormatMapService>().GetEditorFormatMap(view));
                var source = new FakeCompletionSource();
                var items = FakeLanguage.Words
                    .Select(word => new CompletionItemWithHighlight(
                        new CompletionItem(word, source),
                        [new Span(0, 3)]))
                    .ToImmutableArray();
                var filters = ImmutableArray.Create(
                    new CompletionFilterWithState(FakeLanguage.MethodFilter, isAvailable: true, isSelected: false),
                    new CompletionFilterWithState(FakeLanguage.FieldFilter, isAvailable: true, isSelected: true));
                var presentation = new CompletionPresentationViewModel(
                    items,
                    filters,
                    selectedItemIndex: 1,
                    applicableToSpan: view.TextSnapshot.CreateTrackingSpan(0, 3, SpanTrackingMode.EdgeInclusive),
                    useSoftSelection: true,
                    displaySuggestionItem: true,
                    selectSuggestionItem: false,
                    suggestionItem: null,
                    suggestionItemOptions: new SuggestionItemOptions("(new symbol)", "tooltip"));

                presenter.Open(new StubCompletionSession(view), presentation);
                PumpUntil(() => presenter.SurfaceElement.Parent is not null, "completion popup attaches");

                // The rendered list: every item with its highlight, the soft-selected row
                // marked by outline (not fill), the suggestion row, and both filter toggles.
                Assert.AreEqual(FakeLanguage.Words.Length, presenter.VisibleItems.Count);
                Assert.AreEqual(1, presenter.SelectedIndex);
                Assert.IsTrue(presenter.IsSoftSelection);
                Assert.IsTrue(presenter.IsSuggestionRowVisible);

                var toggles = presenter.SurfaceElement.GetVisualDescendants().OfType<ToggleButton>().ToArray();
                Assert.AreEqual(2, toggles.Length);
                Assert.IsFalse(toggles[0].IsChecked == true);
                Assert.IsTrue(toggles[1].IsChecked == true);

                // Toggling a filter reports the full updated state set through FiltersChanged.
                CompletionFilterChangedEventArgs? filterArgs = null;
                presenter.FiltersChanged += (_, e) => filterArgs = e;
                toggles[0].IsChecked = true;
                Assert.IsNotNull(filterArgs);
                Assert.IsTrue(filterArgs.Filters.Single(f => ReferenceEquals(f.Filter, FakeLanguage.MethodFilter)).IsSelected);
                Assert.IsTrue(filterArgs.Filters.Single(f => ReferenceEquals(f.Filter, FakeLanguage.FieldFilter)).IsSelected);

                presenter.Close();
                Assert.IsNull(presenter.SurfaceElement.Parent, "Close removes the popup");
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompletionPresenterShowsTheSelectedItemsDescriptionBesideTheList()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("Mor\n");
            try
            {
                var presenter = new CompletionPresenter(
                    view,
                    IntellisenseTestHost.Container.GetExport<IViewElementFactoryService>(),
                    IntellisenseTestHost.Container.GetExport<Text.Classification.IEditorFormatMapService>().GetEditorFormatMap(view));
                var source = new FakeCompletionSource();
                var items = FakeLanguage.Words
                    .Select(word => new CompletionItemWithHighlight(new CompletionItem(word, source), [new Span(0, 3)]))
                    .ToImmutableArray();
                var filters = ImmutableArray<CompletionFilterWithState>.Empty;
                CompletionPresentationViewModel MakePresentation(int selectedItemIndex) => new(
                    items,
                    filters,
                    selectedItemIndex,
                    applicableToSpan: view.TextSnapshot.CreateTrackingSpan(0, 3, SpanTrackingMode.EdgeInclusive),
                    useSoftSelection: false,
                    displaySuggestionItem: false,
                    selectSuggestionItem: false,
                    suggestionItem: null,
                    suggestionItemOptions: new SuggestionItemOptions("(new symbol)", "tooltip"));

                presenter.Open(new StubCompletionSession(view), MakePresentation(0));
                PumpUntil(() => presenter.SurfaceElement.Parent is not null, "completion popup attaches");

                // The description pane appears beside the list after the debounce, with the
                // selected item's docs from IAsyncCompletionSource.GetDescriptionAsync.
                PumpUntil(
                    () => OverlayTextBlocks(view).Any(t => BlockText(t) == $"Docs for {FakeLanguage.Words[0]}"),
                    "description pane shows the selected item's docs");

                // Moving the selection re-fetches for the newly selected item.
                presenter.Update(new StubCompletionSession(view), MakePresentation(2));
                PumpUntil(
                    () => OverlayTextBlocks(view).Any(t => BlockText(t) == $"Docs for {FakeLanguage.Words[2]}"),
                    "description pane follows the selection");

                presenter.Close();
                Dispatcher.UIThread.RunJobs();
                Assert.IsFalse(
                    OverlayTextBlocks(view).Any(t => BlockText(t).StartsWith("Docs for", StringComparison.Ordinal)),
                    "closing the presenter removes the description pane");
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompletionOpensThroughTheRealBrokerFiltersAndCommits()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            FakeLanguage.UseSuggestionMode = false;
            var (view, window) = IntellisenseTestHost.CreateHostedView("Mor\n");
            try
            {
                var broker = IntellisenseTestHost.Container.GetExport<IAsyncCompletionBroker>();
                var caret = view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 3)).BufferPosition;
                var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, caret.Snapshot);
                var session = broker.TriggerCompletion(view, trigger, caret, CancellationToken.None);
                Assert.IsNotNull(session, "the fake source participates, so a session must start");
                session.OpenOrUpdate(trigger, caret, CancellationToken.None);

                var computed = session.GetComputedItems(CancellationToken.None);
                Assert.IsTrue(computed.Items.Any(static i => i.DisplayText == "Morgania"));
                Assert.IsFalse(computed.Items.Any(static i => i.DisplayText == "Zebra"), "items are filtered by the typed prefix");

                // The presenter opened through the "completion" space reservation manager.
                var manager = view.GetSpaceReservationManager(IntellisenseSpaceReservationManagerNames.CompletionSpaceReservationManagerName);
                PumpUntil(() => manager.Agents.Count == 1, "completion popup opens");
                var presenter = view.Properties.GetProperty<CompletionPresenter>(typeof(CompletionPresenter));
                PumpUntil(() => presenter.VisibleItems.Count > 0, "presenter receives the items");
                Assert.IsTrue(presenter.VisibleItems.All(static i => i.CompletionItem.DisplayText.StartsWith("Mor", StringComparison.Ordinal)));
                Assert.IsTrue(presenter.SelectedIndex >= 0);

                // Commit replaces the applicable span with the selected item.
                CompletionItem? committed = null;
                session.ItemCommitted += (_, e) => committed = e.Item;
                session.Commit(default, CancellationToken.None);
                Assert.IsNotNull(committed);
                string lineText = view.TextSnapshot.GetLineFromLineNumber(0).GetText();
                Assert.AreEqual(committed.InsertText, lineText);

                session.Dismiss();
                PumpUntil(() => manager.Agents.Count == 0, "dismiss closes the popup");
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompletionSuggestionModeShowsTheSuggestionItemSoftSelected()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            FakeLanguage.UseSuggestionMode = true;
            var (view, window) = IntellisenseTestHost.CreateHostedView("Mor\n");
            try
            {
                var broker = IntellisenseTestHost.Container.GetExport<IAsyncCompletionBroker>();
                var caret = view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 3)).BufferPosition;
                var trigger = new CompletionTrigger(CompletionTriggerReason.Invoke, caret.Snapshot);
                var session = broker.TriggerCompletion(view, trigger, caret, CancellationToken.None);
                Assert.IsNotNull(session);
                session.OpenOrUpdate(trigger, caret, CancellationToken.None);
                session.GetComputedItems(CancellationToken.None);

                var presenter = view.Properties.GetProperty<CompletionPresenter>(typeof(CompletionPresenter));
                PumpUntil(() => presenter.IsSuggestionRowVisible, "suggestion mode shows the suggestion row");

                // Suggestion mode keeps the list soft-selected so typing is never hijacked
                // (the walkthrough's defining suggestion-mode behavior).
                Assert.IsTrue(presenter.IsSoftSelection);

                session.Dismiss();
            }
            finally
            {
                FakeLanguage.UseSuggestionMode = false;
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task QuickInfoPresentsClassifiedContentThroughTheRealBrokerAndToolTipService()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("Morgania rocks\n");
            try
            {
                var broker = IntellisenseTestHost.Container.GetExport<IAsyncQuickInfoBroker>();
                var triggerPoint = view.TextSnapshot.CreateTrackingPoint(2, PointTrackingMode.Negative);
                // Pump rather than block: JTF.Run would inline the broker's background
                // computation onto this thread, defeating its own threading contract.
                var triggerTask = broker.TriggerQuickInfoAsync(view, triggerPoint, QuickInfoSessionOptions.None);
                PumpUntil(() => triggerTask.IsCompleted, "quick info trigger completes");
                var session = triggerTask.GetAwaiter().GetResult();
                Assert.IsNotNull(session, "the fake source returns an item, so a session must present");
                PumpUntil(() => session.State == QuickInfoSessionState.Visible, "quick info becomes visible");

                var manager = view.GetSpaceReservationManager(IntellisenseSpaceReservationManagerNames.QuickInfoSpaceReservationManagerName);
                PumpUntil(() => manager.Agents.Count == 1, "quick info popup opens");
                PumpUntil(
                    () => OverlayTextBlocks(view).Any(static t =>
                        (t.Text ?? string.Join(string.Empty, t.Inlines?.Select(static i => (i as Avalonia.Controls.Documents.Run)?.Text) ?? [])).Contains("Info about Morgania", StringComparison.Ordinal)),
                    "the classified content renders in the popup");

                var dismissTask = session.DismissAsync();
                PumpUntil(() => dismissTask.IsCompleted && manager.Agents.Count == 0, "dismissal removes the popup");
                dismissTask.GetAwaiter().GetResult();
                Assert.AreEqual(QuickInfoSessionState.Dismissed, session.State);
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task QuickInfoWrapsInsideTheTipAndRespectsTheViewportWidth()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("Morgania rocks\n", width: 320.0);
            try
            {
                var broker = IntellisenseTestHost.Container.GetExport<IAsyncQuickInfoBroker>();
                var triggerPoint = view.TextSnapshot.CreateTrackingPoint(2, PointTrackingMode.Negative);
                var triggerTask = broker.TriggerQuickInfoAsync(view, triggerPoint, QuickInfoSessionOptions.None);
                PumpUntil(() => triggerTask.IsCompleted, "quick info trigger completes");
                var session = triggerTask.GetAwaiter().GetResult();
                Assert.IsNotNull(session);
                PumpUntil(() => session.State == QuickInfoSessionState.Visible, "quick info becomes visible");

                var overlay = OverlayLayer.GetOverlayLayer(view.VisualElement)!;
                Border Tip() => overlay.GetVisualDescendants().OfType<Border>().FirstOrDefault(static b => b.Child is StackPanel)!;
                PumpUntil(() => Tip() is { Bounds.Width: > 0.0 }, "the tip attaches and lays out");

                var tip = Tip();
                Assert.IsTrue(
                    tip.Bounds.Width <= view.ViewportWidth,
                    $"tip width {tip.Bounds.Width} exceeds the viewport width {view.ViewportWidth}");

                var signature = tip.GetVisualDescendants().OfType<TextBlock>()
                    .Single(static t => BlockText(t).StartsWith("(extension)", StringComparison.Ordinal));
                Assert.IsTrue(
                    signature.Bounds.Width <= tip.Bounds.Width,
                    $"signature width {signature.Bounds.Width} paints past the tip frame {tip.Bounds.Width}");
                Assert.IsTrue(
                    signature.Bounds.Height > signature.FontSize * 2.0,
                    $"the long signature should wrap to multiple lines (height {signature.Bounds.Height})");

                var dismissTask = session.DismissAsync();
                PumpUntil(() => dismissTask.IsCompleted, "dismissal completes");
                dismissTask.GetAwaiter().GetResult();
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SignatureHelpSelectsTheBestMatchPresentsAndTracksTheCaret()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("Greet(name, 3)\nnext line\n");
            try
            {
                var broker = IntellisenseTestHost.Container.GetExport<ISignatureHelpBroker>();
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 6));
                var session = broker.TriggerSignatureHelp(view);
                Assert.IsNotNull(session);
                Assert.IsTrue(broker.IsSignatureHelpActive(view));
                Assert.AreEqual(2, session.Signatures.Count);

                // The source's GetBestMatch picked the two-parameter overload.
                Assert.AreEqual("Greet(string name, int times)", session.SelectedSignature.Content);

                var popup = (IPopupIntellisensePresenter)session.Presenter;
                Assert.AreEqual(
                    IntellisenseSpaceReservationManagerNames.SignatureHelpSpaceReservationManagerName,
                    popup.SpaceReservationManagerName);
                var manager = view.GetSpaceReservationManager(popup.SpaceReservationManagerName);
                PumpUntil(() => manager.Agents.Count == 1, "signature help popup opens");
                PumpUntil(
                    () => OverlayTextBlocks(view).Any(static t =>
                        string.Join(string.Empty, t.Inlines?.Select(static i => (i as Avalonia.Controls.Documents.Run)?.Text) ?? []).Contains("2 of 2", StringComparison.Ordinal)),
                    "the overload indicator renders");

                // Cycling overloads is a selection change the presenter re-renders for.
                bool selectionChanged = false;
                session.SelectedSignatureChanged += (_, _) => selectionChanged = true;
                session.SelectedSignature = session.Signatures[0];
                Assert.IsTrue(selectionChanged);

                // Caret tracking: leaving the applicability span dismisses the session and
                // disposes the sources.
                int disposeCountBefore = FakeSignatureHelpSource.DisposeCount;
                bool dismissed = false;
                session.Dismissed += (_, _) => dismissed = true;
                view.Caret.MoveTo(view.TextSnapshot.GetLineFromLineNumber(1).Start);
                Assert.IsTrue(dismissed, "moving the caret off the signature's span dismisses");
                Assert.IsFalse(broker.IsSignatureHelpActive(view));
                Assert.IsTrue(FakeSignatureHelpSource.DisposeCount > disposeCountBefore, "dismissal disposes the sources");
                PumpUntil(() => manager.Agents.Count == 0, "dismissal removes the popup");
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    /// <summary>The presenter's unit test only needs a non-null session.</summary>
    private sealed class StubCompletionSession(ITextView view) : IAsyncCompletionSession
    {
        public ITextView TextView => view;

        public ITrackingSpan ApplicableToSpan => throw new NotSupportedException();

        public bool IsDismissed => false;

        public PropertyCollection Properties { get; } = new();

        public event EventHandler<CompletionItemEventArgs>? ItemCommitted
        {
            add { }
            remove { }
        }

        public event EventHandler? Dismissed
        {
            add { }
            remove { }
        }

        public event EventHandler<ComputedCompletionItemsEventArgs>? ItemsUpdated
        {
            add { }
            remove { }
        }

        public void OpenOrUpdate(CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token) => throw new NotSupportedException();

        public void Dismiss()
        {
        }

        public bool ShouldCommit(char typedChar, SnapshotPoint triggerLocation, CancellationToken token) => false;

        public CommitBehavior Commit(char typedChar, CancellationToken token) => default;

        public bool CommitIfUnique(CancellationToken token) => false;

        public ComputedCompletionItems GetComputedItems(CancellationToken token) => throw new NotSupportedException();
    }
}
