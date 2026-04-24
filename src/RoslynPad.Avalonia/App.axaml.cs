using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RoslynPad.Resources;

namespace RoslynPad;

class App : Application
{
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public override void Initialize()
    {
        Resources.MergedDictionaries.Add(new Icons());
        AvaloniaXamlLoader.Load(this);
    }

    private void OnSettingsClick(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: MainWindow mainWindow })
        {
            mainWindow.ViewModel.OpenSettingsCommand.Execute(null);
        }
    }
}
