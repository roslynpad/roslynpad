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
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoslynPad.Editor;
using Morgania.CodeAnalysis.Editor.Classification;
using RoslynPad.Themes;
using Avalonia.Media;
using RoslynPad.UI;
using SourceCodeKind = Microsoft.CodeAnalysis.SourceCodeKind;

namespace RoslynPad;

partial class MainWindow : Window
{
    public const string DialogHostIdentifier = "Main";
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
        ViewModel.ThemeChanged += OnViewModelThemeChanged;
        ViewModel.InitializeTheme();

        DataContext = ViewModel;

        InitializeComponent();
        InitializeKeyBindings();
        LoadWindowLayout();

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
                var content = item is SettingsViewModel
                    ? (object)new SettingsView { DataContext = item }
                    : item is SecretsViewModel
                    ? new SecretsView { DataContext = item }
                    : new DocumentView { DataContext = item };

                var document = new Document
                {
                    Id = item.Id,
                    Title = item.Title,
                    DataContext = item,
                    Content = content
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
