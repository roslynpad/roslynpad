using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using RoslynPad.Editor;
using RoslynPad.Roslyn;

namespace RoslynPadAvaloniaReplSample;

/// <summary>
/// Interaction logic for MainWindow.axaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly ObservableCollection<DocumentViewModel> _documents;
    private readonly RoslynHost _host;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        this.AttachDevTools();

        _documents = new ObservableCollection<DocumentViewModel>();
        var items = this.Get<ItemsControl>("Items");
        items.ItemsSource = _documents;

        _host = new CustomRoslynHost(additionalAssemblies: new[]
        {
            Assembly.Load("RoslynPad.Roslyn.Avalonia"),
            Assembly.Load("RoslynPad.Editor.Avalonia")
        }, RoslynHostReferences.NamespaceDefault.With(assemblyReferences: new[]
        {
            typeof(object).Assembly,
            typeof(System.Text.RegularExpressions.Regex).Assembly,
            typeof(Enumerable).Assembly,
        }));

        AddNewDocument();
    }

    private void AddNewDocument(DocumentViewModel? previous = null)
    {
        _documents.Add(new DocumentViewModel(_host, previous));
    }

    private async void OnItemLoaded(object? sender, RoutedEventArgs e)
    {
        if (!(sender is RoslynCodeEditor editor && editor.DataContext is DocumentViewModel viewModel)) return;

        editor.Loaded -= OnItemLoaded;
        editor.Focus();

        var workingDirectory = Directory.GetCurrentDirectory();

        var previous = viewModel.LastGoodPrevious;
        if (previous != null)
        {
            editor.CreatingDocument += (o, args) =>
            {
                args.DocumentId = _host.AddRelatedDocument(previous.Id, new DocumentCreationArgs(
                    args.TextContainer, workingDirectory, SourceCodeKind.Script, args.ProcessDiagnostics,
                    args.TextContainer.UpdateText));
            };
        }
        var documentId = await editor.InitializeAsync(_host, new ClassificationHighlightColors(),
            workingDirectory, string.Empty, SourceCodeKind.Script).ConfigureAwait(true);

        viewModel.Initialize(documentId);
    }

    private async void OnEditorKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (!(sender is RoslynCodeEditor editor && editor.DataContext is DocumentViewModel viewModel)) return;

            if (editor.IsCompletionWindowOpen)
            {
                return;
            }

            e.Handled = true;

            if (viewModel.IsReadOnly) return;

            viewModel.Text = editor.Text;
            if (await viewModel.TrySubmitAsync().ConfigureAwait(true))
            {
                AddNewDocument(viewModel);
            }
        }
    }

    // TODO: workaround for GetSolutionAnalyzerReferences bug (should be added once per Solution)
    private class CustomRoslynHost : RoslynHost
    {
        private bool _addedAnalyzers;

        public CustomRoslynHost(IEnumerable<Assembly>? additionalAssemblies = null, RoslynHostReferences? references = null, ImmutableArray<string>? disabledDiagnostics = null)
            : base(additionalAssemblies, references, disabledDiagnostics)
        {
        }

        protected override IEnumerable<AnalyzerReference> GetSolutionAnalyzerReferences()
        {
            if (!_addedAnalyzers)
            {
                _addedAnalyzers = true;
                return base.GetSolutionAnalyzerReferences();
            }

            return Enumerable.Empty<AnalyzerReference>();
        }
    }
}
