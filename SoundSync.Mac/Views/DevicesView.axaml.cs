using Avalonia.Controls;
using Avalonia.Interactivity;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class DevicesView : UserControl
{
    public DevicesView()
    {
        InitializeComponent();
        // TODO: populate DevicePicker with Core Audio render endpoints (BlackHole excluded)
    }

    private void BtnAdd_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: create SpeakerDevice from selected ComboBox item, add to AppState
        RebuildDeviceList();
    }

    private void BtnScan_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: re-enumerate Core Audio endpoints → refresh DevicePicker
    }

    private void RebuildDeviceList()
    {
        DeviceList.Children.Clear();
        var devices = AppState.Instance.Devices;
        EmptyState.IsVisible = devices.Count == 0;

        foreach (var dev in devices)
        {
            // TODO: add a DeviceCard control per device
            var card = new TextBlock { Text = dev.Name };
            DeviceList.Children.Add(card);
        }
    }
}
