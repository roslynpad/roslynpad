using System.Composition;
using RoslynPad.UI;
using Avalonia;
using Avalonia.Styling;

namespace RoslynPad;

[Export(typeof(MainViewModel)), Shared]
[method: ImportingConstructor]
public class MainViewModelAvalonia(IServiceProvider serviceProvider, IErrorReporter errorReporter, ICommandProvider commands, IApplicationSettings settings, NuGetViewModel nugetViewModel, DocumentFileWatcher documentWatcher) : MainViewModel(serviceProvider, errorReporter, commands, settings, nugetViewModel, documentWatcher)
{
    protected override bool IsSystemDarkTheme() => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;

    protected override void ListenToSystemThemeChanges(Action onChange)
    {
        if (Application.Current is { } app)
        {
            app.ActualThemeVariantChanged += (_, _) =>
            {
                if (app.RequestedThemeVariant is null)
                {
                    onChange();
                }
            };
        }
    }
}
