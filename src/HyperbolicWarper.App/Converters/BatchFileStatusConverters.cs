using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using HyperbolicWarper.App.ViewModels;
using Windows.UI;

namespace HyperbolicWarper.App.Converters;

public sealed class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var color = value switch
        {
            BatchFileStatus.Done => Color.FromArgb(255, 16, 124, 16),
            BatchFileStatus.Warning => Color.FromArgb(255, 157, 93, 0),
            BatchFileStatus.Error => Color.FromArgb(255, 196, 43, 28),
            BatchFileStatus.Processing => Color.FromArgb(255, 0, 95, 184),
            _ => Color.FromArgb(255, 96, 96, 96),
        };

        return new SolidColorBrush(color);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>True only while the row is actively processing, driving the ProgressRing's IsActive.</summary>
public sealed class StatusToProcessingBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is BatchFileStatus.Processing;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Visible only while the row is actively processing (the spinner).</summary>
public sealed class StatusToProcessingVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is BatchFileStatus.Processing ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Visible except while the row is actively processing (the status glyph).</summary>
public sealed class StatusToIdleVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is BatchFileStatus.Processing ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
