using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace SoundSync.Mac.Controls;

public class SyncSlider : Slider
{
    // Set this to enable double-click-to-reset behaviour (NaN = disabled)
    public static readonly StyledProperty<double> ResetValueProperty =
        AvaloniaProperty.Register<SyncSlider, double>(nameof(ResetValue), double.NaN);

    public double ResetValue
    {
        get => GetValue(ResetValueProperty);
        set => SetValue(ResetValueProperty, value);
    }

    private Grid?   _trackGrid;
    private Border? _fillBorder;
    private bool    _dragging;

    public SyncSlider()
    {
        // Prevent single/double tap events from bubbling to parent card/container handlers
        Tapped       += (_, e) => e.Handled = true;
        DoubleTapped += OnDoubleTapped;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (!double.IsNaN(ResetValue))
            SetCurrentValue(ValueProperty, Math.Clamp(ResetValue, Minimum, Maximum));
        e.Handled = true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _trackGrid  = e.NameScope.Find<Grid>("PART_TrackGrid");
        _fillBorder = e.NameScope.Find<Border>("PART_Fill");
        RefreshFill();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty  ||
            change.Property == MinimumProperty ||
            change.Property == MaximumProperty)
            RefreshFill();
    }

    private void RefreshFill()
    {
        if (_trackGrid == null || _trackGrid.ColumnDefinitions.Count < 3) return;
        var range = Maximum - Minimum;
        var ratio = range > 0 ? Math.Clamp((Value - Minimum) / range, 0, 1) : 0;

        // Stars control thumb position (col0 = fill area, col1 = 28px thumb, col2 = empty)
        _trackGrid.ColumnDefinitions[0].Width = new GridLength(ratio,     GridUnitType.Star);
        _trackGrid.ColumnDefinitions[2].Width = new GridLength(1 - ratio, GridUnitType.Star);

        // Fill spans col0+col1. Right margin trims it so visual fill width = ratio × totalWidth:
        //   fill_available = col0_px + 28  →  visual_fill = fill_available − 28×(1−ratio) = ratio×totalWidth
        if (_fillBorder != null)
            _fillBorder.Margin = new Thickness(0, 0, 28 * (1 - ratio), 0);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _dragging = true;
            e.Pointer.Capture(this);
            SetValueFromX(e.GetPosition(this).X);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging)
        {
            SetValueFromX(e.GetPosition(this).X);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragging)
        {
            _dragging = false;
            e.Pointer.Capture(null);
        }
    }

    private void SetValueFromX(double x)
    {
        var w = Bounds.Width;
        // The thumb column is 28px; offset by half (14px) so the thumb centre
        // tracks the pointer exactly rather than jumping on first click.
        const double thumbHalf = 14.0;
        var trackW = w - thumbHalf * 2;
        if (trackW <= 0) return;
        var ratio = Math.Clamp((x - thumbHalf) / trackW, 0, 1);
        SetCurrentValue(ValueProperty, Minimum + ratio * (Maximum - Minimum));
    }
}
