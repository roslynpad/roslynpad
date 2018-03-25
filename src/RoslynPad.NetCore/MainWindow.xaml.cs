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
            using (var stream = typeof(App).Assembly.GetManifestResourceStream("RoslynPad.Resources.Icons.xaml"))
            {
                var dictionary = (ResourceDictionary)new AvaloniaXamlLoader().Load(stream);
                Resources = dictionary;
            }

            var container = new ContainerConfiguration()
                .WithAssembly(Assembly.Load(new AssemblyName("RoslynPad.Common.UI")))
                .WithAssembly(Assembly.GetEntryAssembly());
            var locator = container.CreateContainer().GetExport<IServiceProvider>();

            _viewModel = locator.GetService<MainViewModelBase>();
            
            DataContext = _viewModel;

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
