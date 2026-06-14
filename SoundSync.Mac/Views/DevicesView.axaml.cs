using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class DevicesView : UserControl
{
    public DevicesView()
    {
        InitializeComponent();
        Loaded += (_, _) => RebuildDeviceList();
        AppState.Instance.Devices.CollectionChanged += (_, _) => RebuildDeviceList();
    }

    private void BtnAdd_Click(object? sender, RoutedEventArgs e)
    {
        var name = TxtNewDeviceName.Text?.Trim();
        if (string.IsNullOrEmpty(name)) return;

        AppState.Instance.Devices.Add(new SpeakerDevice
        {
            Name        = name,
            IsConnected = true
        });

        TxtNewDeviceName.Text = string.Empty;
    }

    private void RebuildDeviceList()
    {
        DeviceList.Children.Clear();
        var devices = AppState.Instance.Devices;
        EmptyState.IsVisible     = devices.Count == 0;
        SubtitleText.Text = devices.Count == 0
            ? "Add the speakers you want to sync."
            : $"{devices.Count} speaker{(devices.Count == 1 ? "" : "s")} connected";

        foreach (var dev in devices)
            DeviceList.Children.Add(BuildDeviceCard(dev));
    }

    private Control BuildDeviceCard(SpeakerDevice dev)
    {
        var card = new Border { Padding = new Avalonia.Thickness(16, 14) };
        card.Classes.Add("Card");

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        // Status dot
        var dot = new Avalonia.Controls.Shapes.Ellipse
        {
            Width = 8, Height = 8,
            Fill = new SolidColorBrush(Color.Parse(dev.IsConnected ? "#3DDC84" : "#FF453A")),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Avalonia.Thickness(0, 0, 12, 0)
        };
        Grid.SetColumn(dot, 0);

        // Name + status text
        var info = new StackPanel { Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        var nameText = new TextBlock
        {
            FontSize = 14, FontWeight = FontWeight.SemiBold,
            Foreground = new SolidColorBrush(Colors.White)
        };
        nameText.Text = dev.Name;
        dev.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(dev.Name)) nameText.Text = dev.Name; };

        var statusText = new TextBlock { FontSize = 12, Foreground = new SolidColorBrush(Color.Parse("#8B8FA8")), Text = dev.StatusText };
        dev.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(dev.StatusText)) statusText.Text = dev.StatusText; };

        info.Children.Add(nameText);
        info.Children.Add(statusText);
        Grid.SetColumn(info, 1);

        // Remove button
        var removeBtn = new Button
        {
            Content = "Remove",
            FontSize = 12,
            Padding = new Avalonia.Thickness(10, 5, 10, 5),
            VerticalAlignment = VerticalAlignment.Center
        };
        removeBtn.Classes.Add("Destructive");
        removeBtn.Click += (_, _) =>
        {
            AppState.Instance.Devices.Remove(dev);
        };
        Grid.SetColumn(removeBtn, 2);

        grid.Children.Add(dot);
        grid.Children.Add(info);
        grid.Children.Add(removeBtn);
        card.Child = grid;
        return card;
    }
}
