using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace SoundSync.Mac.Controls;

public class SyncSlider : Slider
{
    private Grid? _trackGrid;
    private bool _dragging;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _trackGrid = e.NameScope.Find<Grid>("PART_TrackGrid");
        RefreshFill();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ValueProperty ||
            change.Property == MinimumProperty ||
            change.Property == MaximumProperty)
            RefreshFill();
    }

    private void RefreshFill()
    {
        if (_trackGrid == null || _trackGrid.ColumnDefinitions.Count < 3) return;
        var range = Maximum - Minimum;
        var ratio = range > 0 ? Math.Clamp((Value - Minimum) / range, 0, 1) : 0;
        _trackGrid.ColumnDefinitions[0].Width = new GridLength(ratio, GridUnitType.Star);
        _trackGrid.ColumnDefinitions[2].Width = new GridLength(1 - ratio, GridUnitType.Star);
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
        if (w <= 0) return;
        SetCurrentValue(ValueProperty, Minimum + Math.Clamp(x / w, 0, 1) * (Maximum - Minimum));
    }
}
