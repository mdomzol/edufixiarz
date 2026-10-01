using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace EDUFIXiarz.Views;

public partial class SetupView : UserControl
{
    private bool _passwordVisible;

    public SetupView() => InitializeComponent();

    private void DomainPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DomainPasswordPreviewBox.Text != DomainPasswordBox.Password)
            DomainPasswordPreviewBox.Text = DomainPasswordBox.Password;
    }

    private void DomainPasswordPreviewBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_passwordVisible)
            return;

        if (DomainPasswordBox.Password != DomainPasswordPreviewBox.Text)
            DomainPasswordBox.Password = DomainPasswordPreviewBox.Text;
    }

    private void TogglePasswordVisibilityButton_Click(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;

        if (_passwordVisible)
        {
            DomainPasswordPreviewBox.Text = DomainPasswordBox.Password;
            DomainPasswordBox.Visibility = Visibility.Collapsed;
            DomainPasswordPreviewBox.Visibility = Visibility.Visible;
            PasswordVisibilityIcon.Source = new BitmapImage(
                new Uri("pack://application:,,,/EDUFIXiarz;component/Assets/Icons/eye-look-or.png"));
            TogglePasswordVisibilityButton.ToolTip = "Ukryj hasło";
        }
        else
        {
            DomainPasswordBox.Password = DomainPasswordPreviewBox.Text;
            DomainPasswordPreviewBox.Visibility = Visibility.Collapsed;
            DomainPasswordBox.Visibility = Visibility.Visible;
            PasswordVisibilityIcon.Source = new BitmapImage(
                new Uri("pack://application:,,,/EDUFIXiarz;component/Assets/Icons/eye-nolook-or.png"));
            TogglePasswordVisibilityButton.ToolTip = "Pokaż hasło";
        }
    }
}
