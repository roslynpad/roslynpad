#nullable enable

namespace Microsoft.VisualStudio.Text.Editor.Implementation;

using Avalonia.Threading;

using Microsoft.VisualStudio.Text;

/// <summary>How a line differs from the reference text.</summary>
internal enum LineChangeKind : byte
{
    None,
    Added,
    Modified,
}

/// <summary>Which lines of a text were added, and which changed, against a reference text.</summary>
internal static class LineDiff
{
    /// <summary>
    /// The most edits the diff looks for between the lines the two texts share at their start and end; a larger change
    /// is marked changed as a whole, since the diff's work grows with the square of its edits.
    /// </summary>
    private const int MaxEdits = 1000;

    /// <summary>
    /// The change of each line of <paramref name="current"/>: added where a run of new lines replaces nothing, changed
    /// where it replaces lines of <paramref name="reference"/>. Lines are compared exactly; removed lines mark nothing.
    /// </summary>
    public static LineChangeKind[] Compute(IReadOnlyList<string> reference, IReadOnlyList<string> current)
    {
        var kinds = new LineChangeKind[current.Count];
        int prefix = 0;
        while (prefix < reference.Count && prefix < current.Count && string.Equals(reference[prefix], current[prefix], StringComparison.Ordinal))
        {
            prefix++;
        }

        int suffix = 0;
        while (suffix < reference.Count - prefix && suffix < current.Count - prefix
            && string.Equals(reference[reference.Count - 1 - suffix], current[current.Count - 1 - suffix], StringComparison.Ordinal))
        {
            suffix++;
        }

        int removed = reference.Count - prefix - suffix;
        int inserted = current.Count - prefix - suffix;
        if (inserted == 0)
        {
            return kinds;
        }

        if (removed == 0)
        {
            Array.Fill(kinds, LineChangeKind.Added, prefix, inserted);
        }
        else if (!Diff(reference, current, prefix, removed, inserted, kinds))
        {
            Array.Fill(kinds, LineChangeKind.Modified, prefix, inserted);
        }

        return kinds;
    }

    /// <summary>Myers' diff of the middles, marking the lines of every hunk; false when it takes more than <see cref="MaxEdits"/> edits.</summary>
    private static bool Diff(IReadOnlyList<string> a, IReadOnlyList<string> b, int start, int n, int m, LineChangeKind[] kinds)
    {
        int max = Math.Min(n + m, MaxEdits);
        var v = new int[(2 * max) + 3];
        int offset = max + 1;

        // What each round of the search reached, kept for walking the path back: round d spans the diagonals -d..d.
        var trace = new List<int[]>();
        int found = -1;
        for (int d = 0; d <= max && found < 0; d++)
        {
            var reached = new int[(2 * d) + 1];
            for (int k = -d; k <= d; k += 2)
            {
                bool down = k == -d || (k != d && v[offset + k - 1] < v[offset + k + 1]);
                int x = down ? v[offset + k + 1] : v[offset + k - 1] + 1;
                int y = x - k;
                while (x < n && y < m && string.Equals(a[start + x], b[start + y], StringComparison.Ordinal))
                {
                    x++;
                    y++;
                }

                v[offset + k] = x;
                reached[k + d] = x;
                if (x >= n && y >= m)
                {
                    found = d;
                }
            }

            trace.Add(reached);
        }

        if (found < 0)
        {
            return false;
        }

        // Walking back: the lines of b a diagonal passes are kept, with the line of a each stands for.
        var partner = new int[m];
        Array.Fill(partner, -1);
        int px = n;
        int py = m;
        for (int d = found; d > 0; d--)
        {
            int k = px - py;
            int[] previous = trace[d - 1];
            int Reached(int diagonal) => previous[diagonal + (d - 1)];
            bool down = k == -d || (k != d && Reached(k - 1) < Reached(k + 1));
            int previousK = down ? k + 1 : k - 1;
            int previousX = Reached(previousK);
            int snakeStart = down ? previousX : previousX + 1;
            while (px > snakeStart)
            {
                px--;
                py--;
                partner[py] = px;
            }

            px = previousX;
            py = previousX - previousK;
        }

        while (px > 0 && py > 0)
        {
            px--;
            py--;
            partner[py] = px;
        }

        // A run of new lines between two kept ones is a hunk: changed where it replaces lines, added where it replaces none.
        int lastA = -1;
        int lastB = -1;
        for (int j = 0; j <= m; j++)
        {
            if (j < m && partner[j] < 0)
            {
                continue;
            }

            int nextA = j == m ? n : partner[j];
            if (j - lastB - 1 > 0)
            {
                Array.Fill(kinds, nextA - lastA - 1 > 0 ? LineChangeKind.Modified : LineChangeKind.Added, start + lastB + 1, j - lastB - 1);
            }

            if (j < m)
            {
                lastA = partner[j];
                lastB = j;
            }
        }

        return true;
    }
}

/// <summary>
/// The line changes of a buffer against its <see cref="TextChangeBaseline"/>, diffed off the UI thread a moment after
/// the text or the baseline changed, and shared by every view of the buffer. It works only while something listens to
/// <see cref="Changed"/>.
/// </summary>
internal sealed class LineChangeTracker
{
    private static readonly TimeSpan s_settle = TimeSpan.FromMilliseconds(250);

    private readonly ITextBuffer _buffer;
    private readonly TextChangeBaseline _baseline;
    private readonly DispatcherTimer _settle;
    private string[] _reference = [];
    private ITextSnapshot? _snapshot;
    private LineChangeKind[] _kinds = [];
    private EventHandler? _changed;
    private int _generation;

    private LineChangeTracker(ITextBuffer buffer)
    {
        _buffer = buffer;
        _baseline = TextChangeBaseline.For(buffer);
        _settle = new DispatcherTimer { Interval = s_settle };
        _settle.Tick += (_, _) =>
        {
            _settle.Stop();
            Compute();
        };
    }

    /// <summary>Raised on the UI thread when <see cref="Snapshot"/> and <see cref="Kinds"/> were computed afresh.</summary>
    public event EventHandler? Changed
    {
        add
        {
            bool first = _changed is null;
            _changed += value;
            if (first && _changed is not null)
            {
                Start();
            }
        }

        remove
        {
            _changed -= value;
            if (_changed is null)
            {
                Stop();
            }
        }
    }

    /// <summary>The snapshot <see cref="Kinds"/> belongs to; null until the first diff is in.</summary>
    public ITextSnapshot? Snapshot => _snapshot;

    /// <summary>The change of each line of <see cref="Snapshot"/>.</summary>
    public IReadOnlyList<LineChangeKind> Kinds => _kinds;

    public static LineChangeTracker For(ITextBuffer buffer)
        => buffer.Properties.GetOrCreateSingletonProperty(typeof(LineChangeTracker), () => new LineChangeTracker(buffer));

    /// <summary>The lines of <paramref name="text"/> as a snapshot has them: split at each line feed, a carriage return before it dropped.</summary>
    internal static string[] SplitLines(string text)
    {
        string[] lines = text.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].EndsWith('\r'))
            {
                lines[i] = lines[i][..^1];
            }
        }

        return lines;
    }

    private void Start()
    {
        _buffer.Changed += OnBufferChanged;
        _baseline.Changed += OnBaselineChanged;
        _reference = SplitLines(_baseline.Text);
        Compute();
    }

    private void Stop()
    {
        _buffer.Changed -= OnBufferChanged;
        _baseline.Changed -= OnBaselineChanged;
        _settle.Stop();
        _generation++;
    }

    private void OnBufferChanged(object? sender, TextContentChangedEventArgs e)
    {
        _settle.Stop();
        _settle.Start();
    }

    private void OnBaselineChanged(object? sender, EventArgs e)
    {
        _reference = SplitLines(_baseline.Text);
        _settle.Stop();
        Compute();
    }

    private void Compute()
    {
        int generation = ++_generation;
        var snapshot = _buffer.CurrentSnapshot;
        string[] reference = _reference;
        _ = Task.Run(() => LineDiff.Compute(reference, LinesOf(snapshot))).ContinueWith(
            task => Dispatcher.UIThread.Post(() =>
            {
                if (generation == _generation && task.IsCompletedSuccessfully)
                {
                    _snapshot = snapshot;
                    _kinds = task.Result;
                    _changed?.Invoke(this, EventArgs.Empty);
                }
            }),
            TaskScheduler.Default);
    }

    private static string[] LinesOf(ITextSnapshot snapshot)
    {
        var lines = new string[snapshot.LineCount];
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = snapshot.GetLineFromLineNumber(i).GetText();
        }

        return lines;
    }
}
