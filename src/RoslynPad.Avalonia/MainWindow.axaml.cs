using System.Collections.Specialized;
using System.Composition.Hosting;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoslynPad.Themes;
using RoslynPad.UI;

namespace RoslynPad;

partial class MainWindow : Window
{
    public const string DialogHostIdentifier = "Main";

    private readonly MainViewModel _viewModel;
    private ThemeDictionary? _themeDictionary;

    public MainViewModel ViewModel => _viewModel;

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
            .WithAssembly(Assembly.Load(new AssemblyName("RoslynPad.Common.UI")))
            .WithAssembly(Assembly.GetEntryAssembly());
        var locator = container.CreateContainer().GetExport<IServiceProvider>();

        _viewModel = locator.GetRequiredService<MainViewModel>();
        _viewModel.OpenDocuments.CollectionChanged += OpenDocuments_CollectionChanged;
        _viewModel.ThemeChanged += OnViewModelThemeChanged;
        _viewModel.InitializeTheme();

        DataContext = _viewModel;

        InitializeComponent();

        if (_viewModel.Settings.WindowFontSize.HasValue)
        {
            FontSize = _viewModel.Settings.WindowFontSize.Value;
        }
    }

    private void OnActiveDockableChanged(object sender, ActiveDockableChangedEventArgs e)
    {
        if (e.Dockable is Document document)
        {
            ViewModel.ActiveContent = document.DataContext;
        }
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

        _themeDictionary = new ThemeDictionary(_viewModel.Theme);
    }

    private async void OnDockableClosedAsync(object? sender, DockableClosedEventArgs e)
    {
        if (e.Dockable is Document document && document.DataContext is OpenDocumentViewModel viewModel)
        {
            await _viewModel.CloseDocument(viewModel).ConfigureAwait(true);
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
            foreach (var item in e.OldItems.OfType<OpenDocumentViewModel>())
            {
                if (factory.FindDockable(DocumentsPane, d => d.Id == item.Id) is { } dockable)
                {
                    factory.RemoveDockable(dockable, collapse: false);
                }

            }
        }
        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<OpenDocumentViewModel>())
            {
                var document = new Document
                {
                    Id = item.Id,
                    Title = item.Title,
                    DataContext = item,
                    Content = DocumentsPane.DocumentTemplate?.Content
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

        await _viewModel.Initialize().ConfigureAwait(true);
    }
}
