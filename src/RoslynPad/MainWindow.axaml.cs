using System.Collections.Specialized;
using System.Composition.Hosting;
using System.Globalization;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Dock.Model;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Model.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoslynPad.Themes;
using RoslynPad.UI;
using SourceCodeKind = Microsoft.CodeAnalysis.SourceCodeKind;

namespace RoslynPad;

partial class MainWindow : Window
{
    public const string DialogHostIdentifier = "Main";
    private readonly DockState _dockState = new();
    private ThemeDictionary? _themeDictionary;
    private bool _isClosing;
    private bool _isClosed;

    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        var services = new ServiceCollection();
        services.AddLogging(
#if DEBUG    
        l => l.AddDebug()
#endif
        );

        var container = new ContainerConfiguration()
            .WithProvider(new ServiceCollectionExportDescriptorProvider(services))
            .WithAssembly(Assembly.GetEntryAssembly());
        var locator = container.CreateContainer().GetExport<IServiceProvider>();

        ViewModel = locator.GetRequiredService<MainViewModel>();
        ViewModel.OpenDocuments.CollectionChanged += OpenDocuments_CollectionChanged;
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.ActiveContent))
            {
                ActivateDockableForActiveContent();
            }
        };
        ViewModel.ThemeChanged += OnViewModelThemeChanged;
        ViewModel.InitializeTheme();

        DataContext = ViewModel;

        InitializeComponent();
        InitializeKeyBindings();
        LoadWindowLayout();
        LoadDockLayout();

        ResultPane.GetObservable(global::Dock.Model.Avalonia.Core.DockBase.ActiveDockableProperty)
            .Subscribe(_ => SetShowIL());

        if (ViewModel.Settings.WindowFontSize.HasValue)
        {
            FontSize = ViewModel.Settings.WindowFontSize.Value;
        }
    }

    private void InitializeKeyBindings()
    {
        this.AddKeyBinding(KeyBindingCommands.NewDocument, ViewModel.NewDocumentCommand, SourceCodeKind.Regular);
        this.AddKeyBinding(KeyBindingCommands.NewScript, ViewModel.NewDocumentCommand, SourceCodeKind.Script);
        this.AddKeyBinding(KeyBindingCommands.OpenFile, ViewModel.OpenFileCommand);
        this.AddKeyBinding(KeyBindingCommands.CloseCurrentFile, ViewModel.CloseCurrentDocumentCommand);
        this.AddKeyBinding(KeyBindingCommands.ToggleOptimization, ViewModel.ToggleOptimizationCommand);
    }

    private void OnErrorButtonClick(object sender, RoutedEventArgs e)
    {
        new Window
        {
            Title = "Error Details",
            Width = 600,
            Height = 400,
            Content = new TextBox
            {
                Text = ViewModel.LastError?.ToString(),
                IsReadOnly = true,
                AcceptsReturn = true
            }
        }.ShowDialog(this);
    }

    private void OnActiveDockableChanged(object sender, ActiveDockableChangedEventArgs e)
    {
        if (e.Dockable is Document document && document.DataContext is IDocumentContent content)
        {
            ViewModel.ActiveContent = content;
        }

        SetShowIL();
    }

    private void ActivateDockableForActiveContent()
    {
        if (ViewModel.ActiveContent is { } content &&
            DocumentsPane.Factory is { } factory &&
            factory.FindDockable(DocumentsPane, d => d.Id == content.Id) is { } dockable &&
            DocumentsPane.ActiveDockable != dockable)
        {
            factory.SetActiveDockable(dockable);
            factory.SetFocusedDockable(DocumentsPane, dockable);
        }
    }

    private void SetShowIL()
    {
        if (ViewModel.CurrentOpenDocument is not { } currentDocument) return;
        currentDocument.ShowIL = ResultPane.ActiveDockable == IL;
    }

    private void OnViewModelThemeChanged(object? sender, EventArgs e)
    {
        if (Application.Current is not { } app)
        {
            return;
        }

        if (!ViewModel.UseSystemTheme)
        {
            app.RequestedThemeVariant = ViewModel.ThemeType switch
            {
                ThemeType.Light => ThemeVariant.Light,
                ThemeType.Dark => ThemeVariant.Dark,
                _ => null
            };
        }

        if (_themeDictionary is not null)
        {
            app.Resources.MergedDictionaries.Remove(_themeDictionary);
        }

        _themeDictionary = new ThemeDictionary(ViewModel.Theme);
        app.Resources.MergedDictionaries.Add(_themeDictionary);
    }

    private async void OnDockableClosedAsync(object? sender, DockableClosedEventArgs e)
    {
        if (e.Dockable is Document document && document.DataContext is IDocumentContent content)
        {
            await ViewModel.CloseTab(content).ConfigureAwait(true);
            (document.Content as IDisposable)?.Dispose();
        }
    }

    private void OpenDocuments_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (DocumentsPane.Factory is not { } factory)
        {
            return;
        }

        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<IDocumentContent>())
            {
                if (factory.FindDockable(DocumentsPane, d => d.Id == item.Id) is { } dockable)
                {
                    factory.RemoveDockable(dockable, collapse: false);
                }
            }
        }
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<IDocumentContent>())
            {
                var content = item switch
                {
                    HomeViewModel => new NewDocumentView { DataContext = ViewModel },
                    SettingsViewModel => new SettingsView { DataContext = item },
                    SecretsViewModel => new SecretsView { DataContext = item },
                    _ => (object)new DocumentView { DataContext = item },
                };

                var document = new Document
                {
                    Id = item.Id,
                    Title = item.Title,
                    DataContext = item,
                    Content = content,
                    CanClose = item is not HomeViewModel,
                };

                factory.AddDockable(DocumentsPane, document);
                factory.SetActiveDockable(document);
                factory.SetFocusedDockable(DocumentsPane, document);
            }
        }
    }

    protected override async void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        ViewModel.ResultsAvailable += OnResultsAvailable;

        await ViewModel.Initialize().ConfigureAwait(true);
    }

    private void OnResultsAvailable()
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Don't steal the tab while the user is watching the IL view.
            if (ResultPane.Factory is { } factory && ResultPane.ActiveDockable != IL)
            {
                if (factory.IsDockablePinned(Results))
                {
                    factory.PreviewPinnedDockable(Results);
                }
                else
                {
                    factory.SetActiveDockable(Results);
                }
            }
        });
    }

    private void SaveDockLayout()
    {
        if (Dock.Layout is not RootDock layout)
        {
            return;
        }

        try
        {
            ViewModel.Settings.DockLayout = DockLayoutSerializer.Serialize(layout);
        }
        catch
        {
            // never block shutdown on layout serialization
        }
    }

    private void LoadDockLayout()
    {
        if (ViewModel.Settings.DockLayout is not { Length: > 0 } layoutJson ||
            Dock.Factory is not { } factory ||
            Dock.Layout is not { } defaultLayout)
        {
            return;
        }

        RootDock? layout;
        try
        {
            layout = DockLayoutSerializer.Deserialize(layoutJson);
        }
        catch
        {
            return;
        }

        // The code-behind relies on these panes; if any is missing (e.g. corrupted or
        // incompatible layout), keep the default XAML layout.
        if (layout is null ||
            FindDockable(factory, layout, "DocumentsPane") is not DocumentDock documentsPane ||
            FindDockable(factory, layout, "ResultPane") is not ToolDock resultPane ||
            FindDockable(factory, layout, "Results") is not Tool results ||
            FindDockable(factory, layout, "IL") is not Tool il)
        {
            return;
        }

        // The document pane must never collapse when empty, otherwise the Home tab the
        // view model adds once the last document closes would have nowhere to dock.
        documentsPane.IsCollapsable = false;

        // Documents from the previous session are reopened by the view model,
        // so drop their stale dockables from the saved layout.
        if (documentsPane.VisibleDockables is { } documentDockables)
        {
            for (var i = documentDockables.Count - 1; i >= 0; i--)
            {
                if (documentDockables[i] is Document staleDocument)
                {
                    documentDockables.Remove(staleDocument);
                }
            }

            documentsPane.ActiveDockable = documentDockables.FirstOrDefault();
            documentsPane.FocusedDockable = documentsPane.ActiveDockable;
        }

        if (layout.FocusedDockable is Document)
        {
            layout.FocusedDockable = null;
        }

        // Capture the tool/document contents created in XAML by Id, swap in the
        // restored layout, then re-attach the contents to it.
        _dockState.Save(defaultLayout);
        Dock.Layout = layout;
        _dockState.Restore(layout);

        DocumentsPane = documentsPane;
        ResultPane = resultPane;
        Results = results;
        IL = il;
    }

    private static IDockable? FindDockable(IFactory factory, RootDock layout, string id) =>
        factory.FindDockable(layout, d => d.Id == id)
        ?? layout.LeftPinnedDockables?.FirstOrDefault(d => d.Id == id)
        ?? layout.RightPinnedDockables?.FirstOrDefault(d => d.Id == id)
        ?? layout.TopPinnedDockables?.FirstOrDefault(d => d.Id == id)
        ?? layout.BottomPinnedDockables?.FirstOrDefault(d => d.Id == id);

    private void SaveWindowLayout()
    {
        var position = Position;
        var size = ClientSize;
        var bounds = new Rect(position.X, position.Y, size.Width, size.Height);
        ViewModel.Settings.WindowBounds = FormattableString.Invariant($"{bounds.X},{bounds.Y},{bounds.Width},{bounds.Height}");
        ViewModel.Settings.WindowState = WindowState.ToString();
    }

    private void LoadWindowLayout()
    {
        var boundsString = ViewModel.Settings.WindowBounds;

        if (!string.IsNullOrEmpty(boundsString))
        {
            var parts = boundsString.Split(',').Select(p => double.TryParse(p, CultureInfo.InvariantCulture, out var result) ? result : double.NaN).Where(d => !double.IsNaN(d)).ToArray();
            if (parts.Length == 4)
            {
                Position = new PixelPoint((int)parts[0], (int)parts[1]);
                Width = parts[2];
                Height = parts[3];
            }
        }

        if (Enum.TryParse(ViewModel.Settings.WindowState, out WindowState state) &&
            state != WindowState.Minimized)
        {
            WindowState = state;
        }
    }

    protected override async void OnClosing(Avalonia.Controls.WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        if (!_isClosing)
        {
            SaveWindowLayout();
            SaveDockLayout();

            _isClosing = true;
            IsEnabled = false;
            e.Cancel = true;

            try
            {
                await Task.Run(ViewModel.OnExit).ConfigureAwait(true);
            }
            catch
            {
                // ignored
            }

            _isClosed = true;
            Close();
        }
        else
        {
            e.Cancel = !_isClosed;
        }
    }

    private void OnNewDocumentClick(object? sender, EventArgs e)
    {
        ViewModel.NewDocumentCommand.Execute(SourceCodeKind.Regular);
    }

    private void OnNewScriptClick(object? sender, EventArgs e)
    {
        ViewModel.NewDocumentCommand.Execute(SourceCodeKind.Script);
    }

    private void OnSaveClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.SaveCommand.Execute(null);
        }
    }

    private void OnFormatDocumentClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.FormatDocumentCommand.Execute(null);
        }
    }

    private void OnCommentSelectionClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.CommentSelectionCommand.Execute(null);
        }
    }

    private void OnUncommentSelectionClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.UncommentSelectionCommand.Execute(null);
        }
    }

    private void OnRenameSymbolClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.RenameSymbolCommand.Execute(null);
        }
    }

    private void OnRunClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.RunCommand.Execute(null);
        }
    }

    private void OnTerminateClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.TerminateCommand.Execute(null);
        }
    }

    private void OnToggleLiveModeClick(object? sender, EventArgs e)
    {
        if (ViewModel.CurrentOpenDocument is { } doc)
        {
            doc.ToggleLiveModeCommand.Execute(null);
        }
    }

    private void OnFindClick(object? sender, EventArgs e)
    {
        ViewModel.CurrentOpenDocument?.RequestFind();
    }

    private void OnReplaceClick(object? sender, EventArgs e)
    {
        ViewModel.CurrentOpenDocument?.RequestReplace();
    }
}
