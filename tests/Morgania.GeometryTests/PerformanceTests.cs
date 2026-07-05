using System.Diagnostics;
using System.Globalization;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.VisualStudio.GeometryTests;

/// <summary>
/// M1 perf smoke against the project budgets: 100k-line file &lt; 500 ms to first frame,
/// steady-state scroll &lt; 16 ms/frame. The budgets here are asserted with headroom for CI
/// machine variance (2x); the strict budgets are enforced when CI perf tracking lands.
/// </summary>
[TestClass]
public sealed class PerformanceTests
{
    [TestMethod]
    public async Task HundredThousandLineFileLaysOutAndScrollsWithinBudget()
    {
        await HeadlessEditor.RunAsync(() =>
        {
            string text = string.Join(
                '\n',
                Enumerable.Range(0, 100_000).Select(static i =>
                    string.Create(CultureInfo.InvariantCulture, $"line {i}: some representative editor content with words to format")));

            // Warm up the formatter/font pipeline on a small view first.
            HeadlessEditor.CreateView("warmup").Close();

            var stopwatch = Stopwatch.StartNew();
            var view = HeadlessEditor.CreateView(text, height: 800.0);
            stopwatch.Stop();
            Assert.IsTrue(
                stopwatch.ElapsedMilliseconds < 1000,
                $"First layout of a 100k-line buffer took {stopwatch.ElapsedMilliseconds} ms (budget 500 ms, asserted at 2x).");

            // Steady-state scrolling, one line per frame.
            const int frames = 200;
            stopwatch.Restart();
            for (int i = 0; i < frames; i++)
            {
                view.ViewScroller.ScrollViewportVerticallyByPixels(-view.LineHeight);
            }

            stopwatch.Stop();
            double perFrame = stopwatch.Elapsed.TotalMilliseconds / frames;
            Assert.IsTrue(perFrame < 32.0, $"Scroll step took {perFrame:F2} ms/frame (budget 16 ms, asserted at 2x).");

            // Jump to the middle of the buffer: still a viewport-sized amount of work.
            stopwatch.Restart();
            view.DisplayTextLineContainingBufferPosition(
                new SnapshotPoint(view.TextSnapshot, view.TextSnapshot.Length / 2),
                0.0,
                ViewRelativePosition.Top);
            stopwatch.Stop();
            Assert.IsTrue(stopwatch.ElapsedMilliseconds < 100, $"Random-access layout took {stopwatch.ElapsedMilliseconds} ms.");

            view.Close();
        }).ConfigureAwait(false);
    }
}
