using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using SoundSync.Mac.Audio;

namespace SoundSync.Mac;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // If the app crashed while BlackHole was active, restore the real device
        BlackHoleManager.RecoverFromCrash();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            desktop.Exit += (_, _) =>
            {
                // Always restore system default audio on clean exit
                BlackHoleManager.Release();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
