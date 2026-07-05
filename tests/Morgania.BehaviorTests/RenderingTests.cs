using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// Minimal rendering test: a hosted view (margins enabled, mirroring the demo) goes through
/// Avalonia's real compositor render pass. Regression coverage for "Visual was invalidated
/// during the render pass": Render overrides must never trigger a text layout or create the
/// selection broker, in particular on the first frame, before any layout has been published.
/// </summary>
[TestClass]
public sealed class RenderingTests
{
    [TestMethod]
    public async Task FirstFrameRendersBeforeAnyTextLayoutWithoutInvalidating()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var bufferFactory = HeadlessEditor.Container.GetExport<ITextBufferFactoryService>();
            var contentTypes = HeadlessEditor.Container.GetExport<IContentTypeRegistryService>();
            var editorFactory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();

            string text = string.Join('\n', Enumerable.Range(0, 60).Select(i => $"line {i}"));
            var buffer = bufferFactory.CreateTextBuffer(text, contentTypes.GetContentType("text"));
            var view = editorFactory.CreateTextView(buffer);
            view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            var host = editorFactory.CreateTextViewHost(view, setFocus: false);

            var window = new Window { Width = 800, Height = 600, Content = host.HostControl };
            window.Show();

            try
            {
                // Show ran Avalonia's layout pass (which sizes the viewport), but the view's
                // first text layout is queued on the dispatcher and has not run yet — the
                // demo's first frame. The render pass must tolerate the missing layout.
                Assert.IsFalse(
                    ((ITextView2)view).TryGetTextViewLines(out _),
                    "The render pass must run before any text layout is published for this test to cover the regression.");

                AvaloniaHeadlessPlatform.ForceRenderTimerTick();

                // Let the queued initial layout run, then render a complete frame.
                Dispatcher.UIThread.RunJobs();
                using var frame = window.CaptureRenderedFrame();
                Assert.IsNotNull(frame);
                Assert.IsTrue(
                    ((ITextView2)view).TryGetTextViewLines(out var lines) && lines.Count > 0,
                    "The initial text layout runs through the normal (non-render) dispatcher path.");
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FrameDrawsAdornmentsCaretsAndSelections()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var bufferFactory = HeadlessEditor.Container.GetExport<ITextBufferFactoryService>();
            var contentTypes = HeadlessEditor.Container.GetExport<IContentTypeRegistryService>();
            var editorFactory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();

            // "@@" is replaced by the test swatch tagger, so the first formatted line
            // carries an embedded adornment run through TextLineImpl.Draw (regression:
            // a drawable run without run properties throws from the draw pass).
            string text = "before@@after\n" + string.Join('\n', Enumerable.Range(0, 60).Select(i => $"line {i}"));
            var buffer = bufferFactory.CreateTextBuffer(text, contentTypes.GetContentType("text"));
            var view = editorFactory.CreateTextView(buffer);
            view.Options.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, true);
            var host = editorFactory.CreateTextViewHost(view, setFocus: false);

            var window = new Window { Width = 800, Height = 600, Content = host.HostControl };
            window.Show();

            try
            {
                // A caret and a selection bring the broker-backed layers into the pass.
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 3));
                view.Selection.Select(new SnapshotSpan(view.TextSnapshot, 0, 10), isReversed: false);

                Dispatcher.UIThread.RunJobs();
                using var frame = window.CaptureRenderedFrame();
                Assert.IsNotNull(frame);
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CaretColorFollowsTheEditorFormatMap()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("abc");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var window = new Window { Width = 400, Height = 300, Content = host.HostControl };
            window.Show();

            try
            {
                view.Caret.MoveTo(new SnapshotPoint(view.TextSnapshot, 0));
                Dispatcher.UIThread.RunJobs();

                // The host themes the caret through the format map entry (the app feeds the
                // VS Code theme's editorCursor.foreground the same way).
                var caretColor = Color.FromRgb(0xFF, 0x00, 0x00);
                var formatMap = HeadlessEditor.Container.GetExport<IEditorFormatMapService>().GetEditorFormatMap(view);
                formatMap.SetProperties(CaretFormatNames.Primary, new ResourceDictionary
                {
                    [EditorFormatDefinition.ForegroundBrushId] = new SolidColorBrush(caretColor),
                });
                Dispatcher.UIThread.RunJobs();

                // Sample the middle of the primary caret rectangle (an unfocused view draws
                // a solid, non-blinking caret) in window coordinates.
                var line = view.TextViewLines[0];
                var bounds = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 0));
                var caretPoint = view.VisualElement.TranslatePoint(
                    new Point(bounds.Left - view.ViewportLeft + 1.0, line.TextTop - view.ViewportTop + line.TextHeight / 2.0),
                    window);
                Assert.IsNotNull(caretPoint);

                using var frame = window.CaptureRenderedFrame();
                Assert.IsNotNull(frame);
                Assert.AreEqual(
                    caretColor,
                    GetPixel(frame, (int)caretPoint.Value.X, (int)caretPoint.Value.Y),
                    "The caret draws with the format-map color.");
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    private static Color GetPixel(WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        nint address = buffer.Address + y * buffer.RowBytes + x * 4;
        byte b0 = Marshal.ReadByte(address);
        byte b1 = Marshal.ReadByte(address + 1);
        byte b2 = Marshal.ReadByte(address + 2);
        byte b3 = Marshal.ReadByte(address + 3);
        return buffer.Format == PixelFormat.Rgba8888
            ? Color.FromArgb(b3, b0, b1, b2)
            : Color.FromArgb(b3, b2, b1, b0);
    }
}
