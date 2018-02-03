using System;
using System.Collections.Generic;
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

        protected override IEnumerable<Assembly> CompositionAssemblies => ImmutableArray.Create(
            Assembly.Load(new AssemblyName("RoslynPad.Roslyn.Avalonia")),
            Assembly.Load(new AssemblyName("RoslynPad.Editor.Avalonia")));
    }
}