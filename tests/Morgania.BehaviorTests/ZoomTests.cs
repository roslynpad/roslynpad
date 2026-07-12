using System.Runtime.InteropServices;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

using Microsoft.VisualStudio.GeometryTests;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.BehaviorTests;

/// <summary>
/// M5: zoom is a render transform over the view (geometry stays in logical coordinates),
/// driven by the ZoomLevel option, clamped to the Min/MaxZoomLevel options, and reachable
/// through the Ctrl+wheel gesture on Zoomable views.
/// </summary>
[TestClass]
public sealed class ZoomTests
{
    [TestMethod]
    public async Task ZoomLevelScalesTheViewAndTracksTheOption()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("line one\nline two");
            var element = view.VisualElement;

            double? raised = null;
            view.ZoomLevelChanged += (_, e) => raised = e.NewZoomLevel;

            view.ZoomLevel = 200.0;
            Assert.AreEqual(200.0, view.ZoomLevel);
            Assert.AreEqual(200.0, view.Options.GetOptionValue(DefaultTextViewOptions.ZoomLevelId));
            Assert.AreEqual(200.0, raised);
            var transform = (ScaleTransform)element.RenderTransform!;
            Assert.AreEqual(2.0, transform.ScaleX, 0.001);
            Assert.AreEqual(2.0, transform.ScaleY, 0.001);

            // Geometry answers stay in logical (pre-zoom) coordinates.
            var line = view.TextViewLines[0];
            var boundsAt100 = line.GetCharacterBounds(new SnapshotPoint(view.TextSnapshot, 1));
            Assert.IsTrue(boundsAt100.Width < 20.0, "Character bounds are not scaled by zoom.");

            // The option is the source of truth: setting it zooms the view.
            view.Options.SetOptionValue(DefaultTextViewOptions.ZoomLevelId, 100.0);
            Assert.AreEqual(100.0, view.ZoomLevel);
            Assert.IsNull(element.RenderTransform);

            // Clamped to the Min/MaxZoomLevel options (defaults 20%..400%).
            view.ZoomLevel = 1000.0;
            Assert.AreEqual(400.0, view.ZoomLevel);
            view.ZoomLevel = 1.0;
            Assert.AreEqual(20.0, view.ZoomLevel);

            view.Close();
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CtrlWheelZoomsAZoomableView()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            var view = HeadlessEditor.CreateView("line one\nline two");
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var window = new Window { Width = 800, Height = 600, Content = host.HostControl };
            window.Show();

            try
            {
                Dispatcher.UIThread.RunJobs();
                var center = new Point(400, 300);

                window.MouseWheel(center, new Vector(0, 1), RawInputModifiers.Control);
                Assert.AreEqual(100.0 * ZoomConstants.ScalingFactor, view.ZoomLevel, 0.001);

                window.MouseWheel(center, new Vector(0, -1), RawInputModifiers.Control);
                Assert.AreEqual(100.0, view.ZoomLevel, 0.001);

                // Without the modifier the wheel scrolls, not zooms.
                window.MouseWheel(center, new Vector(0, -1), RawInputModifiers.None);
                Assert.AreEqual(100.0, view.ZoomLevel, 0.001);

                // The gesture respects the option.
                view.Options.SetOptionValue(DefaultTextViewOptions.EnableMouseWheelZoomId, false);
                window.MouseWheel(center, new Vector(0, 1), RawInputModifiers.Control);
                Assert.AreEqual(100.0, view.ZoomLevel, 0.001);
            }
            finally
            {
                window.Close();
                host.Close();
            }
        }).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ZoomingOutFillsTheEntireViewSlot()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            // Field repro: below 100% the logical viewport is larger than the arranged
            // slot, and anything sized or clipped to the slot in logical (pre-zoom)
            // coordinates — the view's own clip, slot-arranged layers, the panel
            // background — shrinks with the render transform, leaving the bottom of the
            // window unpainted while the (unzoomed) line-number margin runs on.
            var background = Color.FromRgb(0x12, 0x34, 0x56);
            var view = HeadlessEditor.CreateView(
                string.Join('\n', Enumerable.Range(0, 400).Select(i => $"line {i}")));
            view.Background = new SolidColorBrush(background);
            var factory = HeadlessEditor.Container.GetExport<ITextEditorFactoryService>();
            var host = factory.CreateTextViewHost(view, setFocus: false);
            var window = new Window { Width = 800, Height = 600, Content = host.HostControl };
            window.Show();

            try
            {
                Dispatcher.UIThread.RunJobs();
                view.ZoomLevel = 50.0;
                Dispatcher.UIThread.RunJobs();

                using var frame = window.CaptureRenderedFrame();
                Assert.IsNotNull(frame);

                // A pixel near the bottom of the view's cell (well below slot × scale,
                // above the horizontal scrollbar) must carry the view's background.
                Assert.AreEqual(
                    background,
                    GetPixel(frame, 400, 540),
                    "The zoomed-out view paints its whole slot.");

                // And the formatted lines must cover the grown logical viewport.
                Assert.IsTrue(view.ViewportHeight > 900.0, $"Viewport grew to {view.ViewportHeight}.");
                Assert.IsTrue(
                    view.TextViewLines[^1].Bottom >= view.ViewportBottom,
                    "Lines fill down to the logical viewport bottom.");
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
