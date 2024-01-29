using System.Composition;
using System.Reflection;
using RoslynPad.UI;
using System.Collections.Immutable;
using Microsoft.Win32;

namespace RoslynPad;

[Export(typeof(MainViewModel)), Shared]
[method: ImportingConstructor]
public class MainViewModelWindows(IServiceProvider serviceProvider, ITelemetryProvider telemetryProvider, ICommandProvider commands, IApplicationSettings settings, NuGetViewModel nugetViewModel, DocumentFileWatcher documentFileWatcher) : MainViewModel(serviceProvider, telemetryProvider, commands, settings, nugetViewModel, documentFileWatcher)
{
    protected override ImmutableArray<Assembly> CompositionAssemblies => base.CompositionAssemblies
        .Add(Assembly.Load(new AssemblyName("RoslynPad.Roslyn.Windows")))
        .Add(Assembly.Load(new AssemblyName("RoslynPad.Editor.Windows")));

    protected override bool IsSystemDarkTheme()
    {
        using var personalizeKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return personalizeKey?.GetValue("AppsUseLightTheme") as int? == 0;
    }

    protected override void ListenToSystemThemeChanges(Action onChange)
    {
        SystemEvents.UserPreferenceChanging += (_, _) => onChange();
    }
}
