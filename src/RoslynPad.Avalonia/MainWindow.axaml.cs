using System.Collections.Specialized;
using System.Composition.Hosting;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Dock.Model.Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RoslynPad.UI;

namespace RoslynPad;

partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        var services = new ServiceCollection();
        services.AddLogging(l => l.AddSimpleConsole().AddDebug());

        var container = new ContainerConfiguration()
            .WithProvider(new ServiceCollectionExportDescriptorProvider(services))
            .WithAssembly(Assembly.Load(new AssemblyName("RoslynPad.Common.UI")))
            .WithAssembly(Assembly.GetEntryAssembly());
        var locator = container.CreateContainer().GetExport<IServiceProvider>();

        _viewModel = locator.GetRequiredService<MainViewModel>();
        _viewModel.OpenDocuments.CollectionChanged += OpenDocuments_CollectionChanged;

        DataContext = _viewModel;

        InitializeComponent();

        if (_viewModel.Settings.WindowFontSize.HasValue)
        {
            FontSize = _viewModel.Settings.WindowFontSize.Value;
        }

        if (DocumentsPane.Factory is { } factory)
        {
            factory.DockableClosed += Factory_DockableClosedAsync;
        }
    }

    private async void Factory_DockableClosedAsync(object? sender, Dock.Model.Core.Events.DockableClosedEventArgs e)
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
