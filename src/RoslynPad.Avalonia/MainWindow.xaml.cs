using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RoslynPad.UI;
using System;
using System.Composition.Hosting;
using System.Reflection;
using Avalonia.Controls.Primitives;

namespace RoslynPad
{
    class MainWindow : Window
    {
        private readonly MainViewModelBase _viewModel;

        public MainWindow()
        {
            var container = new ContainerConfiguration()
                .WithAssembly(Assembly.Load(new AssemblyName("RoslynPad.Common.UI")))
                .WithAssembly(Assembly.GetEntryAssembly());
            var locator = container.CreateContainer().GetExport<IServiceProvider>();

            _viewModel = locator.GetService<MainViewModelBase>();
            
            DataContext = _viewModel;

            if (_viewModel.Settings.WindowFontSize.HasValue)
            {
                FontSize = _viewModel.Settings.WindowFontSize.Value;
            }

            AvaloniaXamlLoader.Load(this);

            this.AttachDevTools();
        }

        protected override async void OnTemplateApplied(TemplateAppliedEventArgs e)
        {
            base.OnTemplateApplied(e);
            
            await _viewModel.Initialize().ConfigureAwait(true);
        }
    }
}
