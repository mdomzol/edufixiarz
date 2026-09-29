using System.Windows;
using System.Windows.Media;
using EDUFIXiarz.Models;
namespace EDUFIXiarz;

public partial class MainWindow : Window
{

private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdministrator())
        {
            MessageBox.Show(
                "EDUFIXiarz musi być uruchomiony jako administrator.",
                "Wymagane uprawnienia",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        SetupView.RunButton.IsEnabled = false;
        try
        {
            var options = new SetupOptions
            {
                ChangeHostname = SetupView.HostnameCheck.IsChecked == true,
                Hostname = SetupView.HostnameBox.Text.Trim(),
                JoinDomain = _joinDomainRequested,
                Domain = SetupView.DomainBox.Text.Trim(),
                DomainUser = SetupView.DomainUserBox.Text.Trim(),
                DomainPassword = SetupView.DomainPasswordBox.SecurePassword,
                RemoveBloatware = SetupView.BloatwareCheck.IsChecked == true,
                RemoveOffice = SetupView.OfficeCheck.IsChecked == true,
                InstallApplications = SetupView.AppsCheck.IsChecked == true
            };

            await _setupService.RunAsync(options, GetSelectedApps(), LogOutput, LogError);
            SetupView.DomainPasswordBox.Clear();

            Log("Zakończono wybrane operacje.");
            ShowPage(LogView);
            MessageBox.Show(
                "Przygotowanie stanowiska zakończone. Niektóre zmiany mogą wymagać ponownego uruchomienia.",
                "EDUFIXiarz",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log("BŁĄD: " + ex.Message);
            ShowPage(LogView);
            MessageBox.Show(ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetupView.RunButton.IsEnabled = true;
        }
    }

    private void ResetSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        SetupView.HostnameCheck.IsChecked = false;
        _joinDomainRequested = false;
        SetupView.DomainCredentialsExpander.IsExpanded = false;
        SetupView.DomainCredentialsExpander.Visibility = Visibility.Collapsed;
        SetupView.DomainCheck.Content = "DOŁĄCZ DO DOMENY AD";
        SetupView.DomainCheck.Background = FindResource("InputBrush") as Brush;
        SetupView.DomainCheck.BorderBrush = FindResource("BorderBrush") as Brush;
        SetupView.DomainPasswordBox.Clear();
        SetupView.BloatwareCheck.IsChecked = false;
        SetupView.OfficeCheck.IsChecked = false;
        ClearAllAppsButton_Click(sender, e);
        SetupView.AppsPreviewPanel.Visibility = Visibility.Collapsed;
        SetupView.PreviewAppsButton.Content = "POKAŻ WYBRANE APLIKACJE  ›";
        SetupView.AppsPreviewSummaryText.Text = "Brak aplikacji wskazanych do instalacji.";
        Log("Wybór zadań i aplikacji został wyzerowany.");
    }

}
