using System;
using System.Composition;
using System.Reflection;
using RoslynPad.UI;
using System.Collections.Immutable;

namespace RoslynPad
{
    [Export(typeof(MainViewModelBase)), Shared]
    public class MainViewModel : MainViewModelBase
    {
        [ImportingConstructor]
        public MainViewModel(IServiceProvider serviceProvider, ITelemetryProvider telemetryProvider, ICommandProvider commands, IApplicationSettings settings, NuGetViewModel nugetViewModel, DocumentFileWatcher documentWatcher) : base(serviceProvider, telemetryProvider, commands, settings, nugetViewModel, documentWatcher)
        {
        }

        protected override ImmutableArray<Assembly> CompositionAssemblies => base.CompositionAssemblies
            .Add(Assembly.Load(new AssemblyName("RoslynPad.Roslyn.Avalonia")))
            .Add(Assembly.Load(new AssemblyName("RoslynPad.Editor.Avalonia")));
    }
}