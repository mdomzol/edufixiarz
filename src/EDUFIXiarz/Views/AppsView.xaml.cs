using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace EDUFIXiarz.Views;

public partial class AppsView : UserControl
{
    public AppsView() => InitializeComponent();

    private static void UpdateCardBorder(CheckBox checkBox, bool selected)
    {
        if (VisualTreeHelper.GetParent(checkBox) is Border card)
        {
            card.BorderBrush = selected
                ? (Brush)Application.Current.FindResource("AccentBrush")
                : (Brush)Application.Current.FindResource("BorderBrush");
        }
    }

    private void AppSelectionCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            UpdateCardBorder(checkBox, true);
    }

    private void AppSelectionCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
            UpdateCardBorder(checkBox, false);
    }
}
