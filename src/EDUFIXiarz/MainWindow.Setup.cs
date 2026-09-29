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

            OperationProgressBar.Value = 0;
            OperationProgressText.Text = "PRZYGOTOWANIE W TOKU…";
            await _setupService.RunAsync(options, GetSelectedApps(), LogOutput, LogError, UpdateOperationProgress);
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
        OperationProgressBar.Value = isRunning ? 0 : OperationProgressBar.Value;
        OperationProgressText.Text = isRunning ? "PRZYGOTOWANIE W TOKU…" : "GOTOWY";
        AppsView.IsEnabled = !isRunning;

        ReportMenuButton.IsEnabled = !isRunning;
        SetupMenuButton.IsEnabled = !isRunning;
        AppsMenuButton.IsEnabled = !isRunning;
        LogMenuButton.IsEnabled = !isRunning;
    }

    private void UpdateOperationProgress(int completed, int total, string label)
    {
        var percentage = total == 0 ? 0 : completed * 100.0 / total;
        OperationProgressBar.Value = percentage;
        OperationProgressText.Text = $"{completed:00} / {total:00} · {label.ToUpperInvariant()}";
    }

    private void ApplyProfile(
        bool removeBloatware,
        bool removeOffice,
        params string[] applications)
    {
        SetupView.HostnameCheck.IsChecked = true;
        SetupView.BloatwareCheck.IsChecked = removeBloatware;
        SetupView.OfficeCheck.IsChecked = removeOffice;
        SetAppSelection(applications);
        SetupView.AppsPreviewPanel.Visibility = Visibility.Collapsed;
        SetupView.PreviewAppsButton.Content = "POKAŻ WYBRANE APLIKACJE  ›";
    }

    private void SchoolProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyProfile(true, false, "Adobe", "Everything", "Chrome", "7zip", "VLC", "LibreOffice", "Notepad++");
        Log("Zastosowano profil: Szkoła.");
    }

    private void OfficeProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyProfile(true, true, "Adobe", "Everything", "Chrome", "7zip", "VLC", "LibreOffice", "Notepad++");
        Log("Zastosowano profil: Biuro.");
    }

    private void DeveloperProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyProfile(true, false, "Everything", "Chrome", "Firefox", "7zip", "VSCode", "PuTTY", "Notepad++");
        Log("Zastosowano profil: Developer.");
    }

    private void FullProfileButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyProfile(true, true, AppSelectionAliases.Keys.ToArray());
        Log("Zastosowano profil: Pełne przygotowanie.");
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