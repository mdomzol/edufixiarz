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

        SetSetupOperationState(true);

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
            SetupView.DomainPasswordBox.Clear();
            SetSetupOperationState(false);
        }
    }

    private void SetSetupOperationState(bool isRunning)
    {
        SetupView.IsEnabled = !isRunning;
        AppsView.IsEnabled = !isRunning;

        ReportMenuButton.IsEnabled = !isRunning;
        SetupMenuButton.IsEnabled = !isRunning;
        AppsMenuButton.IsEnabled = !isRunning;
        LogMenuButton.IsEnabled = !isRunning;
    }

    private void ResetSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        SetupView.HostnameCheck.IsChecked = false;
        SetupView.BloatwareCheck.IsChecked = false;
        SetupView.OfficeCheck.IsChecked = false;
        SetupView.AppsCheck.IsChecked = false;

        SetupView.HostnameBox.Text = Environment.MachineName;

        _joinDomainRequested = false;
        SetupView.DomainCredentialsExpander.IsExpanded = false;
        SetupView.DomainCredentialsExpander.Visibility = Visibility.Collapsed;
        SetupView.DomainCheck.Content = "DOŁĄCZ DO DOMENY AD";
        SetupView.DomainCheck.Background = FindResource("InputBrush") as Brush;
        SetupView.DomainCheck.BorderBrush = FindResource("BorderBrush") as Brush;
        SetupView.DomainBox.Clear();
        SetupView.DomainUserBox.Clear();
        SetupView.DomainPasswordBox.Clear();

        SetAppSelection();
        SetupView.AppsPreviewPanel.Visibility = Visibility.Collapsed;
        SetupView.PreviewAppsButton.Content = "POKAŻ WYBRANE APLIKACJE  ›";
        SetupView.AppsPreviewSummaryText.Text = "Brak aplikacji wskazanych do instalacji.";

        Log("Wybór zadań i aplikacji został wyzerowany.");
    }
}