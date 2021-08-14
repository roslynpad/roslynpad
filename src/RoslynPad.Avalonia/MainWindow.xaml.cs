using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RoslynPad.UI;
using System;
using System.Composition.Hosting;
using System.Reflection;
using Avalonia.Controls.Primitives;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace RoslynPad
{
    class MainWindow : Window
    {
        private readonly MainViewModelBase _viewModel;

        public MainWindow()
        {
            var services = new ServiceCollection();
            services.AddLogging(l => l.AddSimpleConsole().AddDebug());

            var container = new ContainerConfiguration()
                .WithProvider(new ServiceCollectionExportDescriptorProvider(services))
                .WithAssembly(Assembly.Load(new AssemblyName("RoslynPad.Common.UI")))
                .WithAssembly(Assembly.GetEntryAssembly());
            var locator = container.CreateContainer().GetExport<IServiceProvider>();

            _viewModel = locator.GetRequiredService<MainViewModelBase>();
            
            DataContext = _viewModel;

            if (_viewModel.Settings.WindowFontSize.HasValue)
            {
                FontSize = _viewModel.Settings.WindowFontSize.Value;
            }

            AvaloniaXamlLoader.Load(this);
        }

        protected override async void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            
            await _viewModel.Initialize().ConfigureAwait(true);
        }
    }
}
