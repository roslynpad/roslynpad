using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RoslynPad.UI;
using System;
using System.Composition.Hosting;
using System.Reflection;

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
            _viewModel.Initialize();

            DataContext = _viewModel;

            AvaloniaXamlLoader.Load(this);
            this.AttachDevTools();
        }
    }
}
