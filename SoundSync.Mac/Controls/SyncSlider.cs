using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace SoundSync.Mac.Controls;

/// <summary>
/// Slider subclass that manually syncs Track.Minimum/Maximum/Value in code,
/// bypassing Avalonia 11's compiled-binding path which silently breaks
/// TwoWay TemplateBindings on Track inside StyleInclude files.
/// </summary>
public class SyncSlider : Slider
{
    private Track? _track;
    private bool _syncingFromTrack;
    private bool _syncingFromSlider;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_track != null)
            _track.PropertyChanged -= Track_PropertyChanged;

        base.OnApplyTemplate(e);

        _track = e.NameScope.Find<Track>("PART_Track");
        if (_track == null) return;

        // Force initial values so the first layout pass has correct track width
        _track.Minimum = Minimum;
        _track.Maximum = Maximum;
        _track.Value   = Value;

        // Keep track in sync if Slider.OnApplyTemplate's own bindings don't fire
        _track.PropertyChanged += Track_PropertyChanged;

        _track.InvalidateMeasure();
        _track.InvalidateArrange();
    }

    private void Track_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Track.ValueProperty || _syncingFromSlider) return;
        _syncingFromTrack = true;
        Value = (double)e.NewValue!;
        _syncingFromTrack = false;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (_track == null || _syncingFromTrack) return;

        _syncingFromSlider = true;
        try
        {
            if (change.Property == MinimumProperty)
                _track.Minimum = (double)change.NewValue!;
            else if (change.Property == MaximumProperty)
                _track.Maximum = (double)change.NewValue!;
            else if (change.Property == ValueProperty)
                _track.Value = (double)change.NewValue!;
        }
        finally
        {
            _syncingFromSlider = false;
        }
    }
}
