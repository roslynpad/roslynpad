using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.VisualStudio.Text;
using RoslynPad.Editor;
using RoslynPad.UI;

namespace RoslynPad;

/// <summary>
/// The build output pane: a read-only editor view streaming the selected phase's output.
/// Writes arrive on background threads; updates are coalesced into single pulls on the UI
/// thread that append only the new text to the buffer.
/// </summary>
partial class BuildOutputView : UserControl
{
    private readonly CodeEditorView _editor;
    private BuildOutputViewModel? _buildOutput;
    private BuildOutputDocument? _document;
    private BuildOutputDocument.ReadPosition _position;
    private int _updateScheduled;

    static BuildOutputView()
    {
        ViewModelProperty.Changed.AddClassHandler<BuildOutputView>((view, _) => view.ScheduleUpdate());
    }

    public BuildOutputView()
    {
        InitializeComponent();

        _editor = this.FindControl<CodeEditorView>("Editor") ?? throw new InvalidOperationException("Missing Editor");
        DataContextChanged += (_, _) => OnDataContextChanged();
    }

    public static readonly StyledProperty<MainViewModel?> ViewModelProperty =
        AvaloniaProperty.Register<BuildOutputView, MainViewModel?>(nameof(ViewModel));

    public MainViewModel? ViewModel
    {
        get => GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private void OnDataContextChanged()
    {
        if (_buildOutput is { } previous)
        {
            previous.PropertyChanged -= OnBuildOutputPropertyChanged;
        }

        _buildOutput = (DataContext as OpenDocumentViewModel)?.BuildOutput;
        if (_buildOutput is { } current)
        {
            current.PropertyChanged += OnBuildOutputPropertyChanged;
        }

        SetDocument(_buildOutput?.SelectedDocument);
    }

    private void OnBuildOutputPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BuildOutputViewModel.SelectedDocument))
        {
            SetDocument(_buildOutput?.SelectedDocument);
        }
    }

    private void SetDocument(BuildOutputDocument? document)
    {
        if (ReferenceEquals(_document, document))
        {
            return;
        }

        if (_document is { } previous)
        {
            previous.Changed -= ScheduleUpdate;
        }

        _document = document;
        _position = default; // unknown generation forces a full render
        if (document is { } current)
        {
            current.Changed += ScheduleUpdate;
        }

        ScheduleUpdate();
    }

    private void ScheduleUpdate()
    {
        if (Interlocked.Exchange(ref _updateScheduled, 1) == 0)
        {
            Dispatcher.UIThread.Post(Update);
        }
    }

    private void Update()
    {
        Interlocked.Exchange(ref _updateScheduled, 0);

        if (_document is not { } document)
        {
            _position = default;
            _editor.Buffer?.Replace(new Span(0, _editor.Buffer.CurrentSnapshot.Length), string.Empty);
            return;
        }

        if (_editor.Buffer is null)
        {
            // The editor is created lazily once the Roslyn host is up (same pattern as ILViewer).
            if (ViewModel is not { IsInitialized: true } viewModel)
            {
                return;
            }

            _editor.CreateBuffer(viewModel, string.Empty, BuildOutputClassificationDefinitions.ContentType);
            _editor.CreateView(isReadOnly: true, setFocus: false, showLineNumbers: false);
        }

        var buffer = _editor.Buffer!;
        var (text, restarted, position) = document.ReadFrom(_position);
        _position = position;

        if (restarted)
        {
            buffer.Replace(new Span(0, buffer.CurrentSnapshot.Length), text);
            ScrollToEnd();
        }
        else if (text.Length > 0)
        {
            // Stick to the tail only while the user is already at the bottom
            var atBottom = IsAtBottom();
            buffer.Insert(buffer.CurrentSnapshot.Length, text);
            if (atBottom)
            {
                ScrollToEnd();
            }
        }
    }

    private bool IsAtBottom()
    {
        if (_editor.TextView is not { } textView || textView.ViewportHeight == 0)
        {
            return true;
        }

        try
        {
            return textView.TextViewLines is { } lines &&
                lines.LastVisibleLine.EndIncludingLineBreak.Position >= textView.TextSnapshot.Length;
        }
        catch (InvalidOperationException)
        {
            // No layout yet
            return true;
        }
    }

    private void ScrollToEnd()
    {
        if (_editor.TextView is not { } textView || textView.ViewportHeight == 0)
        {
            return;
        }

        var snapshot = textView.TextBuffer.CurrentSnapshot;
        textView.ViewScroller.EnsureSpanVisible(new SnapshotSpan(snapshot, snapshot.Length, 0));
    }
}
