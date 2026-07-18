#nullable enable

using System.Composition;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Implementation;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.Text.Editor.Implementation;

/// <summary>
/// An <see cref="IBackgroundWorkIndicatorService"/> implementation providing the cancellation
/// semantics (cancel on edit / focus lost / explicit dismissal) plus a visual indicator: an
/// indeterminate progress bar at the top of the view (the VS Code look) while work is in flight.
/// </summary>
[Export(typeof(IBackgroundWorkIndicatorService))]
[Shared]
[method: ImportingConstructor]
internal sealed class BackgroundWorkIndicatorService(IEditorFormatMapService editorFormatMaps) : IBackgroundWorkIndicatorService
{
    public IBackgroundWorkIndicator Create(ITextView textView, SnapshotSpan applicableToSpan, string description, BackgroundWorkIndicatorOptions options)
        => new BackgroundWorkIndicator(textView, options, ProgressIndicator.Get(textView, editorFormatMaps));

    private sealed class BackgroundWorkIndicator : IBackgroundWorkIndicator
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ITextView _textView;
        private readonly BackgroundWorkIndicatorOptions _options;
        private readonly ProgressIndicator _progress;
        private int _suppressAutoCancelCount;
        private bool _disposed;

        public BackgroundWorkIndicator(ITextView textView, BackgroundWorkIndicatorOptions options, ProgressIndicator progress)
        {
            _textView = textView;
            _options = options ?? new BackgroundWorkIndicatorOptions();
            _progress = progress;
            _progress.Show();

            if (_options.CancelOnEdit)
            {
                _textView.TextBuffer.Changed += OnTextBufferChanged;
            }

            if (_options.CancelOnFocusLost)
            {
                _textView.LostAggregateFocus += OnLostAggregateFocus;
            }
        }

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public BackgroundWorkOperationScope AddScope(string description) => new Scope(description);

        public IDisposable SuppressAutoCancel()
        {
            Interlocked.Increment(ref _suppressAutoCancelCount);
            return new SuppressAutoCancelDisposer(this);
        }

        public void Dispose()
        {
            lock (_cancellationTokenSource)
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
            }

            if (_options.CancelOnEdit)
            {
                _textView.TextBuffer.Changed -= OnTextBufferChanged;
            }

            if (_options.CancelOnFocusLost)
            {
                _textView.LostAggregateFocus -= OnLostAggregateFocus;
            }

            // Dispose may be called from any thread; the visual is UI-thread-affine.
            Dispatcher.UIThread.Post(_progress.Hide);
        }

        private void OnTextBufferChanged(object? sender, TextContentChangedEventArgs e) => AutoCancel();

        private void OnLostAggregateFocus(object? sender, EventArgs e) => AutoCancel();

        private void AutoCancel()
        {
            if (Volatile.Read(ref _suppressAutoCancelCount) == 0)
            {
                _cancellationTokenSource.Cancel();
            }
        }

        private sealed class Scope : BackgroundWorkOperationScope
        {
            public Scope(string description) => Description = description;

            public override string Description { get; set; }

            public override void Dispose()
            {
            }
        }

        private sealed class SuppressAutoCancelDisposer(BackgroundWorkIndicator owner) : IDisposable
        {
            private int _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, 1) == 0)
                {
                    Interlocked.Decrement(ref owner._suppressAutoCancelCount);
                }
            }
        }
    }

    /// <summary>
    /// The per-view visual: a thin indeterminate progress bar overlaying the top edge of the
    /// view, refcounted across concurrent indicators and shown only after a short delay so
    /// fast operations never flash it. All members run on the UI thread.
    /// </summary>
    private sealed class ProgressIndicator
    {
        private static readonly SolidColorBrush s_defaultForeground = new(Color.FromRgb(0x0E, 0x70, 0xC0));

        private readonly ITextView _view;
        private readonly IEditorFormatMapService _editorFormatMaps;
        private readonly DispatcherTimer _showDelay;
        private ProgressBar? _bar;
        private int _count;

        public static ProgressIndicator Get(ITextView view, IEditorFormatMapService editorFormatMaps)
            => view.Properties.GetOrCreateSingletonProperty(() => new ProgressIndicator(view, editorFormatMaps));

        private ProgressIndicator(ITextView view, IEditorFormatMapService editorFormatMaps)
        {
            _view = view;
            _editorFormatMaps = editorFormatMaps;
            _showDelay = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _showDelay.Tick += (_, _) =>
            {
                _showDelay.Stop();
                ShowCore();
            };
        }

        public void Show()
        {
            if (_count++ == 0)
            {
                _showDelay.Start();
            }
        }

        public void Hide()
        {
            if (--_count == 0)
            {
                _showDelay.Stop();
                if (_bar is not null)
                {
                    _bar.IsVisible = false;
                    _bar.IsIndeterminate = false;
                }
            }
        }

        private void ShowCore()
        {
            if (_count == 0 || _view.IsClosed)
            {
                return;
            }

            if (_bar is null)
            {
                if (!_view.Properties.TryGetProperty(typeof(IWpfTextViewHost), out IWpfTextViewHost host)
                    || host is not WpfTextViewHost hostImplementation)
                {
                    return;
                }

                _bar = new ProgressBar
                {
                    Height = 3.0,
                    MinHeight = 3.0,
                    MinWidth = 0.0,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Background = Brushes.Transparent,
                    BorderThickness = default,
                    CornerRadius = default,
                    IsHitTestVisible = false,
                };
                hostImplementation.AddViewOverlay(_bar);
            }

            var properties = _editorFormatMaps.GetEditorFormatMap(_view).GetProperties(BackgroundWorkIndicatorFormatNames.Name);
            _bar.Foreground = properties.TryGetValue(BackgroundWorkIndicatorFormatNames.Foreground, out var value) && value is IBrush brush
                ? brush
                : s_defaultForeground;
            _bar.IsVisible = true;
            _bar.IsIndeterminate = true;
        }
    }
}
