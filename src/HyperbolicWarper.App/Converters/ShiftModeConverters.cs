using Microsoft.UI.Xaml.Data;
using HyperbolicWarper.Core.Models;

namespace HyperbolicWarper.App.Converters;

/// <summary>Shows content only when the bound <see cref="ShiftMode"/> is Relative (the direction toggle has no meaning for SetFirstStart).</summary>
public sealed class ShiftModeToRelativeVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ShiftMode.Relative ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Labels the H/M/S/Ms group differently depending on whether it represents an offset or an absolute target time.</summary>
public sealed class ShiftModeToHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ShiftMode.SetFirstStart ? "New first timecode" : "Shift amount";

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}

/// <summary>Two-way binds a RadioButton's IsChecked to whether a <see cref="ShiftMode"/> equals the converter parameter (the enum member name).</summary>
public sealed class ShiftModeEqualsConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language) =>
        value is ShiftMode mode && parameter is string p && Enum.TryParse<ShiftMode>(p, out var target) && mode == target;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isChecked && isChecked && parameter is string p && Enum.TryParse<ShiftMode>(p, out var target))
        {
            return target;
        }

        return DependencyProperty.UnsetValue;
    }
}
