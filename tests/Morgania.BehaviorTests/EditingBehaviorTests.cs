using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// Scripted behavior tests from documented VS semantics: editing goes
/// through IEditorOperations against a live view; caret and selection are asserted through
/// their legacy contracts, which shim the multi-selection broker.
/// </summary>
[TestClass]
public sealed class EditingBehaviorTests
{
    private static IEditorOperations GetOperations(IWpfTextView view)
        => HeadlessEditor.Container.GetExport<IEditorOperationsFactoryService>().GetEditorOperations(view);

    [TestMethod]
    public async Task TypingInsertsAtCaretAndAdvancesIt()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("world");
            var operations = GetOperations(view);

            operations.InsertText("hello ");
            Assert.AreEqual("hello world", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(6, view.Caret.Position.BufferPosition.Position);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task BackspaceAndDeleteRemoveAroundTheCaret()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("abcdef");
            var operations = GetOperations(view);

            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 3));
            operations.Backspace();
            Assert.AreEqual("abdef", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(2, view.Caret.Position.BufferPosition.Position);

            operations.Delete();
            Assert.AreEqual("abef", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(2, view.Caret.Position.BufferPosition.Position);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task WordMovementFollowsVsWordSemantics()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("hello world foo");
            var operations = GetOperations(view);

            operations.MoveToNextWord(false);
            Assert.AreEqual(6, view.Caret.Position.BufferPosition.Position, "Next word from 0 lands on 'world'.");
            operations.MoveToNextWord(false);
            Assert.AreEqual(12, view.Caret.Position.BufferPosition.Position, "Next word lands on 'foo'.");
            operations.MoveToPreviousWord(false);
            Assert.AreEqual(6, view.Caret.Position.BufferPosition.Position);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NaturalLanguageNavigatorRequestedForAnyContentTypeHasWordSemantics()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // Language navigators (e.g. Roslyn's) request their natural-language fallback —
            // used for comment and string interiors — with the "any" content type, which
            // only a provider declared on "any" itself can satisfy; anything less specific
            // falls back to the per-character DefaultTextNavigator (double-click in a
            // comment then selects a single letter).
            var contentTypes = HeadlessEditor.Container.GetExport<IContentTypeRegistryService>();
            var buffer = HeadlessEditor.Container.GetExport<ITextBufferFactoryService>()
                .CreateTextBuffer("// alpha bravo", contentTypes.GetContentType("text"));
            var navigator = HeadlessEditor.Container.GetExport<ITextStructureNavigatorSelectorService>()
                .CreateTextStructureNavigator(buffer, contentTypes.GetContentType("any"));

            var extent = navigator.GetExtentOfWord(new SnapshotPoint(buffer.CurrentSnapshot, 5));
            Assert.AreEqual("alpha", extent.Span.GetText());
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ShiftMovementExtendsTheSelection()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("abcdef");
            var operations = GetOperations(view);

            for (int i = 0; i < 4; i++)
            {
                operations.MoveToNextCharacter(true);
            }

            Assert.IsFalse(view.Selection.IsEmpty);
            Assert.AreEqual(0, view.Selection.Start.Position.Position);
            Assert.AreEqual(4, view.Selection.End.Position.Position);
            Assert.IsFalse(view.Selection.IsReversed);
            Assert.AreEqual(4, view.Caret.Position.BufferPosition.Position);

            // Collapsing: a plain movement clears the selection.
            operations.MoveToNextCharacter(false);
            Assert.IsTrue(view.Selection.IsEmpty);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LineUpDownPreservesTheColumn()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("0123456789\nabcdefghij\nklmnopqrst");
            var operations = GetOperations(view);

            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 7));
            operations.MoveLineDown(false);
            var line1 = view.TextSnapshot.GetLineFromLineNumber(1);
            Assert.AreEqual(line1.Start.Position + 7, view.Caret.Position.BufferPosition.Position);

            operations.MoveLineDown(false);
            var line2 = view.TextSnapshot.GetLineFromLineNumber(2);
            Assert.AreEqual(line2.Start.Position + 7, view.Caret.Position.BufferPosition.Position);

            operations.MoveLineUp(false);
            operations.MoveLineUp(false);
            Assert.AreEqual(7, view.Caret.Position.BufferPosition.Position);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SelectAllThenTypingReplacesTheDocument()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("line one\nline two\nline three");
            var operations = GetOperations(view);

            operations.SelectAll();
            Assert.AreEqual(view.TextSnapshot.Length, view.Selection.StreamSelectionSpan.Length);

            operations.InsertText("gone");
            Assert.AreEqual("gone", view.TextBuffer.CurrentSnapshot.GetText());

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task UndoAndRedoRoundTrip()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("original");
            var operations = GetOperations(view);
            var history = HeadlessEditor.Container.GetExport<ITextBufferUndoManagerProvider>()
                .GetTextBufferUndoManager(view.TextBuffer).TextBufferUndoHistory;

            operations.InsertText("XYZ");
            Assert.AreEqual("XYZoriginal", view.TextBuffer.CurrentSnapshot.GetText());

            Assert.IsTrue(history.CanUndo);
            history.Undo(1);
            Assert.AreEqual("original", view.TextBuffer.CurrentSnapshot.GetText());

            Assert.IsTrue(history.CanRedo);
            history.Redo(1);
            Assert.AreEqual("XYZoriginal", view.TextBuffer.CurrentSnapshot.GetText());

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MultipleCaretsTypeAtEveryInsertionPoint()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("first\nsecond\nthird");
            var operations = GetOperations(view);
            var broker = view.GetMultiSelectionBroker();

            var line1 = view.TextSnapshot.GetLineFromLineNumber(1);
            var line2 = view.TextSnapshot.GetLineFromLineNumber(2);
            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 0));
            broker.AddSelection(new Selection(new VirtualSnapshotPoint(line1.Start)));
            broker.AddSelection(new Selection(new VirtualSnapshotPoint(line2.Start)));
            Assert.IsTrue(broker.HasMultipleSelections);

            operations.InsertText("// ");
            Assert.AreEqual("// first\n// second\n// third", view.TextBuffer.CurrentSnapshot.GetText());

            broker.ClearSecondarySelections();
            Assert.IsFalse(broker.HasMultipleSelections);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task BoxSelectionSurvivesTypingWithACaretPerLine()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("aaaa\nbbbb\ncccc");
            var operations = GetOperations(view);
            var broker = view.GetMultiSelectionBroker();

            // A box over column 1..3 of all three lines.
            var anchor = new VirtualSnapshotPoint(new SnapshotPoint(view.TextSnapshot, 1));
            var active = new VirtualSnapshotPoint(view.TextSnapshot.GetLineFromLineNumber(2).Start + 3);
            broker.SetBoxSelection(new Selection(anchor, active));
            Assert.AreEqual(3, broker.AllSelections.Count);

            // Typing replaces the box on every line and leaves a caret per line (the box
            // reshapes to zero width instead of collapsing to a single stream selection).
            operations.InsertText("X");
            Assert.AreEqual("aXa\nbXb\ncXc", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.IsTrue(broker.IsBoxSelection, "typing over a box keeps box mode");
            Assert.AreEqual(3, broker.AllSelections.Count, "one caret per line survives the edit");

            operations.InsertText("Y");
            Assert.AreEqual("aXYa\nbXYb\ncXYc", view.TextBuffer.CurrentSnapshot.GetText());

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task BoxSelectionReachesThroughVirtualSpaceOnShortLines()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("wide line\nab\nwide line");
            var operations = GetOperations(view);
            var broker = view.GetMultiSelectionBroker();

            // A zero-width box at column 6; the middle line is 2 characters long, so its
            // caret sits 4 columns into virtual space.
            var anchor = new VirtualSnapshotPoint(new SnapshotPoint(view.TextSnapshot, 6));
            var lastLine = view.TextSnapshot.GetLineFromLineNumber(2);
            var active = new VirtualSnapshotPoint(lastLine.Start + 6);
            broker.SetBoxSelection(new Selection(anchor, active));

            Assert.AreEqual(3, broker.AllSelections.Count);
            var middle = broker.AllSelections[1];
            Assert.IsTrue(middle.InsertionPoint.IsInVirtualSpace, "the short line's caret is virtual");
            Assert.AreEqual(4, middle.InsertionPoint.VirtualSpaces);

            // Typing materializes the virtual space as real whitespace on the short line.
            operations.InsertText("X");
            Assert.AreEqual("wide lXine\nab    X\nwide lXine", view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(3, broker.AllSelections.Count, "the box survives the edit");

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HebrewTextEditsAndNavigatesInLogicalOrder()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView(string.Empty);
            var operations = GetOperations(view);

            const string hebrew = "שלום עולם";
            operations.InsertText(hebrew);
            Assert.AreEqual(hebrew, view.TextBuffer.CurrentSnapshot.GetText());
            Assert.AreEqual(hebrew.Length, view.Caret.Position.BufferPosition.Position);

            // Logical navigation: previous-character moves logically back through RTL text.
            operations.MoveToPreviousCharacter(false);
            Assert.AreEqual(hebrew.Length - 1, view.Caret.Position.BufferPosition.Position);

            // Caret geometry sits on RTL bounds.
            var line = view.GetTextViewLineContainingBufferPosition(view.Caret.Position.BufferPosition);
            var bounds = line.GetCharacterBounds(view.Caret.Position.BufferPosition);
            Assert.IsTrue(bounds.IsRightToLeft);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CutAndPasteRoundTripThroughTheClipboardSeam()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("keep MOVE end");
            var operations = GetOperations(view);

            view.Selection.Select(new SnapshotSpan(view.TextSnapshot, 5, 5), false);
            Assert.IsTrue(operations.CutSelection());
            Assert.AreEqual("keep end", view.TextBuffer.CurrentSnapshot.GetText());

            operations.MoveToEndOfDocument(false);
            Assert.IsTrue(operations.Paste());
            Assert.AreEqual("keep endMOVE ", view.TextBuffer.CurrentSnapshot.GetText());

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CaretEnsureVisibleScrollsTheViewport()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join('\n', Enumerable.Range(0, 300).Select(i => $"line {i}"));
            var view = HeadlessEditor.CreateView(text, height: 300.0);

            var farLine = view.TextSnapshot.GetLineFromLineNumber(200);
            view.Caret.MoveTo(farLine.Start);
            view.Caret.EnsureVisible();

            Assert.IsTrue(view.TextViewLines.ContainsBufferPosition(view.Caret.Position.BufferPosition));
            var containing = view.TextViewLines.GetTextViewLineContainingBufferPosition(view.Caret.Position.BufferPosition);
            Assert.IsTrue(containing.Top >= view.ViewportTop - 0.01 && containing.Bottom <= view.ViewportBottom + 0.01);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InsertNewLineSplitsTheLine()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("headtail");
            var operations = GetOperations(view);

            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 4));
            operations.InsertNewLine();

            Assert.AreEqual(2, view.TextBuffer.CurrentSnapshot.LineCount);
            Assert.AreEqual("head", view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(0).GetText());
            Assert.AreEqual("tail", view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(1).GetText());
            Assert.AreEqual(view.TextBuffer.CurrentSnapshot.GetLineFromLineNumber(1).Start.Position, view.Caret.Position.BufferPosition.Position);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SelectionRendersAsBoundsOnTheViewLines()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("select some text here");
            var operations = GetOperations(view);

            view.Selection.Select(new SnapshotSpan(view.TextSnapshot, 7, 4), false);
            var line = view.TextViewLines[0];
            var selectionOnLine = view.Selection.GetSelectionOnTextViewLine(line);
            Assert.IsNotNull(selectionOnLine);
            Assert.AreEqual(7, selectionOnLine.Value.Start.Position.Position);
            Assert.AreEqual(11, selectionOnLine.Value.End.Position.Position);

            var bounds = line.GetNormalizedTextBounds(selectionOnLine.Value.SnapshotSpan);
            Assert.IsTrue(bounds.Count > 0 && bounds.Sum(b => Math.Abs(b.Width)) > 0.0);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CaretPositionChangedFiresWhenALayoutHandlerReadsTheCaretDuringBrokerCreation()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // Field repro: broker creation forces the initial layout (the selection
            // transformer captures its preferred x-coordinate from the view lines), and an
            // extension's LayoutChanged handler reading Caret.Position re-enters the lazy
            // broker getter. The caret must end up subscribed to the broker the view keeps,
            // or PositionChanged never fires again.
            var container = HeadlessEditor.Container;
            var buffer = container.GetExport<ITextBufferFactoryService>().CreateTextBuffer(
                "hello world",
                container.GetExport<IContentTypeRegistryService>().GetContentType("text"));
            var view = container.GetExport<ITextEditorFactoryService>().CreateTextView(buffer);
            view.LayoutChanged += (_, _) => _ = view.Caret.Position;

            // The first caret read arrives before any layout, so it triggers the
            // broker-creation-forces-layout-re-enters-getter sequence above.
            _ = view.Caret.Position;

            bool moved = false;
            view.Caret.PositionChanged += (_, _) => moved = true;
            view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 3));

            Assert.AreEqual(3, view.Caret.Position.BufferPosition.Position);
            Assert.IsTrue(moved, "Caret.PositionChanged fired for the move.");

            view.Close();
        }).ConfigureAwait(false);
    }
}
