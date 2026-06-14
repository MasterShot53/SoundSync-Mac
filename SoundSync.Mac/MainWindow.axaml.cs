using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SoundSync.Mac;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ShowPage("Devices");
    }

    private void ShowPage(string name)
    {
        PageDevices.IsVisible  = name == "Devices";
        PageSync.IsVisible     = name == "Sync";
        PageSettings.IsVisible = name == "Settings";
    }

    private void NavDevices_Click(object? sender, RoutedEventArgs e)  => ShowPage("Devices");
    private void NavSync_Click(object? sender, RoutedEventArgs e)     => ShowPage("Sync");
    private void NavSettings_Click(object? sender, RoutedEventArgs e) => ShowPage("Settings");
}
