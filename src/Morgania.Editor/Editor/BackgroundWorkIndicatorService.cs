#nullable enable

using System.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Morgania.Text.Editor.Implementation;

/// <summary>
/// A minimal <see cref="IBackgroundWorkIndicatorService"/> implementation that provides the
/// cancellation semantics (cancel on edit / focus lost / explicit dismissal) without any visual
/// indicator yet.
/// </summary>
[Export(typeof(IBackgroundWorkIndicatorService))]
[Shared]
internal sealed class BackgroundWorkIndicatorService : IBackgroundWorkIndicatorService
{
    public IBackgroundWorkIndicator Create(ITextView textView, SnapshotSpan applicableToSpan, string description, BackgroundWorkIndicatorOptions options)
        => new BackgroundWorkIndicator(textView, options);

    private sealed class BackgroundWorkIndicator : IBackgroundWorkIndicator
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ITextView _textView;
        private readonly BackgroundWorkIndicatorOptions _options;
        private int _suppressAutoCancelCount;
        private bool _disposed;

        public BackgroundWorkIndicator(ITextView textView, BackgroundWorkIndicatorOptions options)
        {
            _textView = textView;
            _options = options ?? new BackgroundWorkIndicatorOptions();

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
}
