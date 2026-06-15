using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using SoundSync.Models;

namespace SoundSync.Mac.Views;

public partial class SpatialView : UserControl
{
    public SpatialView()
    {
        InitializeComponent();
        Loaded += (_, _) => RefreshSpeakerList();
        AppState.Instance.Devices.CollectionChanged += (_, _) => RefreshSpeakerList();
    }

    private void SpatialToggle_Changed(object? sender, RoutedEventArgs e)
    {
        bool isOn = SpatialToggle.IsChecked == true;
        AppState.Instance.SpatialEnabled = isOn;
        SpatialToggleLabel.Text = isOn ? "On" : "Off";
        DisabledOverlay.IsVisible = !isOn;
        SpatialContent.IsVisible = isOn;
        if (isOn) RefreshSpeakerList();
    }

    private void RefreshSpeakerList()
    {
        SpeakerPositionList.Children.Clear();
        foreach (var dev in AppState.Instance.Devices)
        {
            var card = new Border
            {
                CornerRadius = new Avalonia.CornerRadius(10),
                BorderThickness = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(12, 10),
                Margin = new Avalonia.Thickness(0, 0, 0, 0)
            };
            var bg = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative)
            };
            bg.GradientStops.Add(new GradientStop(Color.Parse("#1E2230"), 0));
            bg.GradientStops.Add(new GradientStop(Color.Parse("#141820"), 1));
            card.Background = bg;

            var border = new LinearGradientBrush
            {
                StartPoint = new Avalonia.RelativePoint(0, 0, Avalonia.RelativeUnit.Relative),
                EndPoint = new Avalonia.RelativePoint(0, 1, Avalonia.RelativeUnit.Relative)
            };
            border.GradientStops.Add(new GradientStop(Color.Parse("#28FFFFFF"), 0));
            border.GradientStops.Add(new GradientStop(Color.Parse("#06FFFFFF"), 1));
            card.BorderBrush = border;

            float pan = dev.Pan;
            string channelColor = pan <= -0.4f ? "#4F7CFF" : pan >= 0.4f ? "#9B7FFF" : "#8B8FA8";

            var content = new StackPanel { Spacing = 2 };
            content.Children.Add(new TextBlock
            {
                Text = dev.Name,
                FontSize = 13,
                FontWeight = FontWeight.SemiBold,
                Foreground = new SolidColorBrush(Colors.White)
            });
            content.Children.Add(new TextBlock
            {
                Text = dev.ChannelName,
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.Parse(channelColor))
            });
            card.Child = content;
            SpeakerPositionList.Children.Add(card);
        }
    }
}
