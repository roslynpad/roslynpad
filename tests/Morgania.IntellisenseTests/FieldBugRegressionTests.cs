using Avalonia;
using Avalonia.Headless;
using Avalonia.Input;

using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.IntellisenseTests;

/// <summary>
/// Repro tests for field-reported bugs, driven through real (headless) window input:
/// hovering never showed Quick Info because plain <see cref="ITextViewCreationListener"/>
/// exports were never invoked (only the Wpf flavor was), double-click did not select the
/// word under the pointer, and a click past the end of a line put the caret in virtual
/// space even though the UseVirtualSpace option (default off) says it must not.
/// </summary>
[TestClass]
public sealed class FieldBugRegressionTests
{
    /// <summary>Window-relative point over the given buffer position (mid-character).</summary>
    private static Point PointOver(IWpfTextView view, Avalonia.Controls.Window window, int bufferPosition)
    {
        var line = view.TextViewLines.GetTextViewLineContainingBufferPosition(
            new SnapshotPoint(view.TextSnapshot, bufferPosition));
        var bounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, bufferPosition));
        var local = new Point(
            bounds.Left - view.ViewportLeft + (bounds.Width / 2.0),
            bounds.Top - view.ViewportTop + (bounds.Height / 2.0));
        var translated = view.VisualElement.TranslatePoint(local, window);
        Assert.IsNotNull(translated, "the view must be connected to the window's visual tree");
        return translated.Value;
    }

    [TestMethod]
    public async Task HoveringAWordTriggersQuickInfoThroughTheRealMouseHoverPipeline()
    {
        await IntellisenseTestHost.RunAsync(async () =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("alpha bravo charlie\n");
            try
            {
                var broker = IntellisenseTestHost.Container.GetExport<IAsyncQuickInfoBroker>();
                Assert.IsNull(broker.GetSession(view));

                // Rest the pointer mid-"bravo"; the hover cycle (a real DispatcherTimer)
                // must reach the vendored QuickInfoController, which subscribed via the
                // plain ITextViewCreationListener contract at view creation.
                int hoverPosition = "alpha bravo charlie".IndexOf("bravo", StringComparison.Ordinal) + 2;
                window.MouseMove(PointOver(view, window, hoverPosition));

                IAsyncQuickInfoSession? session = null;
                for (int i = 0; i < 100 && (session = broker.GetSession(view)) is null; i++)
                {
                    // Yielding lets the session's dispatcher loop fire due timers.
                    await Task.Delay(20).ConfigureAwait(true);
                }

                Assert.IsNotNull(session, "hover must trigger a Quick Info session");
                for (int i = 0; i < 100 && session.State is QuickInfoSessionState.Created or QuickInfoSessionState.Calculating; i++)
                {
                    await Task.Delay(20).ConfigureAwait(true);
                }

                Assert.AreEqual(QuickInfoSessionState.Visible, session.State);
                await session.DismissAsync().ConfigureAwait(true);
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DoubleClickSelectsTheWordUnderThePointer()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("alpha bravo charlie\n");
            try
            {
                int clickPosition = "alpha bravo charlie".IndexOf("bravo", StringComparison.Ordinal) + 2;
                var point = PointOver(view, window, clickPosition);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);

                Assert.AreEqual("bravo", view.Selection.StreamSelectionSpan.SnapshotSpan.GetText());
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ClickPastTheEndOfALineHonorsTheVirtualSpaceOption()
    {
        await IntellisenseTestHost.RunAsync(() =>
        {
            var (view, window) = IntellisenseTestHost.CreateHostedView("short\n");
            try
            {
                var line = view.TextViewLines[0];
                var origin = view.VisualElement.TranslatePoint(default, window);
                Assert.IsNotNull(origin);
                double y = origin.Value.Y + line.Top - view.ViewportTop + (line.Height / 2.0);
                double endX = origin.Value.X + line.TextRight - view.ViewportLeft;

                // Virtual space defaults off: a click far past the end of the line lands
                // the caret at the end of the line, with no virtual spaces.
                Assert.IsFalse(view.Options.GetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId));
                var pastEnd = new Point(endX + 200.0, y);
                window.MouseDown(pastEnd, MouseButton.Left);
                window.MouseUp(pastEnd, MouseButton.Left);
                Assert.AreEqual(0, view.Caret.Position.VirtualSpaces);
                Assert.AreEqual(line.End.Position, view.Caret.Position.BufferPosition.Position);

                // With the option on, the same click is allowed into virtual space.
                view.Options.SetOptionValue(DefaultTextViewOptions.UseVirtualSpaceId, true);
                var fartherPast = new Point(endX + 400.0, y);
                window.MouseDown(fartherPast, MouseButton.Left);
                window.MouseUp(fartherPast, MouseButton.Left);
                Assert.IsTrue(view.Caret.Position.VirtualSpaces > 0, "virtual space is reachable when the option is on");
            }
            finally
            {
                window.Close();
            }
        }).ConfigureAwait(false);
    }
}
