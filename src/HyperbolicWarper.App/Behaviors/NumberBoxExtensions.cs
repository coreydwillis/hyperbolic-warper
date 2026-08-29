using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace HyperbolicWarper.App.Behaviors;

// NumberBox clamps its Value on commit (Enter/blur) but has no built-in way to cap how many
// characters can be typed while editing, so e.g. "999" fits in the Seconds box even though only
// 0-59 is valid. This reaches into the NumberBox's template for its inner TextBox and rejects
// edits that would exceed MaxLength, using TextBox.BeforeTextChanging since it's the only WinUI3
// hook that can reject an edit before it's applied (KeyDown fires too early / doesn't stop it).
public static class NumberBoxExtensions
{
    public static readonly DependencyProperty MaxLengthProperty =
        DependencyProperty.RegisterAttached(
            "MaxLength",
            typeof(int),
            typeof(NumberBoxExtensions),
            new PropertyMetadata(0, OnMaxLengthChanged));

    public static int GetMaxLength(NumberBox numberBox) => (int)numberBox.GetValue(MaxLengthProperty);

    public static void SetMaxLength(NumberBox numberBox, int value) => numberBox.SetValue(MaxLengthProperty, value);

    private static void OnMaxLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not NumberBox numberBox || e.NewValue is not int maxLength || maxLength <= 0)
        {
            return;
        }

        void Hook()
        {
            if (FindDescendantTextBox(numberBox) is { } textBox)
            {
                textBox.BeforeTextChanging += (_, args) =>
                {
                    if (args.NewText.Length > maxLength)
                    {
                        args.Cancel = true;
                    }
                };
            }
        }

        if (numberBox.IsLoaded)
        {
            Hook();
        }
        else
        {
            numberBox.Loaded += (_, _) => Hook();
        }
    }

    private static TextBox? FindDescendantTextBox(DependencyObject root)
    {
        var childCount = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is TextBox textBox)
            {
                return textBox;
            }

            if (FindDescendantTextBox(child) is { } found)
            {
                return found;
            }
        }

        return null;
    }
}
