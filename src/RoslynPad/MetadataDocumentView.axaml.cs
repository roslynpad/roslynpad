using Avalonia.Controls;
using Microsoft.CodeAnalysis.MetadataAsSource;
using Microsoft.CodeAnalysis.Text;
using RoslynPad.UI;

namespace RoslynPad;

partial class MetadataDocumentView : UserControl, IDisposable
{
    private readonly CodeEditorView _editor;

    private MetadataDocumentViewModel? _viewModel;
    private IMetadataAsSourceFileService? _metadataAsSourceFileService;

    public MetadataDocumentView()
    {
        InitializeComponent();

        _editor = this.FindControl<CodeEditorView>("Editor") ?? throw new InvalidOperationException("Missing Editor");

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs args)
    {
        if (DataContext is not MetadataDocumentViewModel viewModel || _viewModel is not null)
        {
            return;
        }

        _viewModel = viewModel;

        try
        {
            var buffer = _editor.CreateBuffer(viewModel.MainViewModel, File.ReadAllText(viewModel.FilePath));

            _metadataAsSourceFileService = viewModel.MainViewModel.RoslynHost.GetService<IMetadataAsSourceFileService>();
            _metadataAsSourceFileService.TryAddDocumentToWorkspace(viewModel.FilePath, buffer.AsTextContainer(), out var documentId);

            _editor.CreateView(isReadOnly: true);

            viewModel.NavigationRequested += OnNavigationRequested;
            viewModel.OnViewInitialized(documentId);
        }
        catch
        {
            // Never leave a navigation awaiting a view that failed to materialize.
            viewModel.OnViewInitialized(documentId: null);
            throw;
        }
    }

    private void OnNavigationRequested(TextSpan span) => _editor.NavigateToSpan(span);

    public void Dispose()
    {
        if (_viewModel is { } viewModel)
        {
            viewModel.NavigationRequested -= OnNavigationRequested;
            _metadataAsSourceFileService?.TryRemoveDocumentFromWorkspace(viewModel.FilePath);
        }

        _editor.Dispose();
    }
}
