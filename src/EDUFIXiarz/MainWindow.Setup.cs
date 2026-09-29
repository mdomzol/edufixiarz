using System.IO;
using System.Windows;
using Microsoft.Win32;
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

            if (_currentReport is null)
            {
                Log("Brak aktualnego odczytu sprzętu — wykonuję odczyt bazowy przed przygotowaniem.");
                _hardwareBeforePreparation = await _hardwareService.GetReportAsync(LogError);
                _currentReport = _hardwareBeforePreparation;
            }
            else
            {
                _hardwareBeforePreparation = _currentReport;
            }

            OperationProgressBar.Value = 0;
            OperationProgressText.Text = "PRZYGOTOWANIE W TOKU…";
            _currentPreparation = await _setupService.RunAsync(options, GetSelectedApps(), LogOutput, LogError, UpdateOperationProgress);
            Log("Przygotowanie zakończone — wykonuję odczyt kontrolny stacji.");
            var afterPreparation = await _hardwareService.GetReportAsync(LogError);
            _currentReport = afterPreparation;
            DataContext = afterPreparation;
            Log($"Zakończono wybrane operacje: {_currentPreparation.Steps.Count} etapów.");
            ShowPage(LogView);
            MessageBox.Show(
                "Przygotowanie stanowiska zakończone. Niektóre zmiany mogą wymagać ponownego uruchomienia.",
                "EDUFIXiarz",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("PRZYGOTOWANIA", ex);
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
        AuditView.IsEnabled = !isRunning;

        ReportMenuButton.IsEnabled = !isRunning;
        SetupMenuButton.IsEnabled = !isRunning;
        AppsMenuButton.IsEnabled = !isRunning;
        AuditMenuButton.IsEnabled = !isRunning;
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

    private void SaveProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var profile = new SetupProfile
        {
            ChangeHostname = SetupView.HostnameCheck.IsChecked == true,
            Hostname = SetupView.HostnameBox.Text.Trim(),
            JoinDomain = _joinDomainRequested,
            Domain = SetupView.DomainBox.Text.Trim(),
            DomainUser = SetupView.DomainUserBox.Text.Trim(),
            RemoveBloatware = SetupView.BloatwareCheck.IsChecked == true,
            RemoveOffice = SetupView.OfficeCheck.IsChecked == true,
            InstallApplications = SetupView.AppsCheck.IsChecked == true,
            ApplicationIds = GetSelectedApps().Select(app => app.Id).ToList()
        };

        var dialog = new SaveFileDialog
        {
            FileName = "EDUFIXiarz-profil.json",
            Filter = "Profil EDUFIXiarz (*.json)|*.json",
            AddExtension = true,
            DefaultExt = "json",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, _profileService.Serialize(profile));
            Log($"Zapisano profil: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            LogException("ZAPISU PROFILU", ex);
            MessageBox.Show("Nie udało się zapisać profilu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void LoadProfileButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Profil EDUFIXiarz (*.json)|*.json",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var profile = _profileService.Deserialize(File.ReadAllText(dialog.FileName));
            SetupView.HostnameCheck.IsChecked = profile.ChangeHostname;
            SetupView.HostnameBox.Text = string.IsNullOrWhiteSpace(profile.Hostname) ? Environment.MachineName : profile.Hostname;
            SetupView.BloatwareCheck.IsChecked = profile.RemoveBloatware;
            SetupView.OfficeCheck.IsChecked = profile.RemoveOffice;

            _joinDomainRequested = profile.JoinDomain;
            SetupView.DomainCredentialsExpander.Visibility = _joinDomainRequested ? Visibility.Visible : Visibility.Collapsed;
            SetupView.DomainCredentialsExpander.IsExpanded = _joinDomainRequested;
            SetupView.DomainCheck.Content = _joinDomainRequested ? "ANULUJ DOŁĄCZANIE DO DOMENY" : "DOŁĄCZ DO DOMENY AD";
            SetupView.DomainCheck.Background = FindResource(_joinDomainRequested ? "PanelAltBrush" : "InputBrush") as Brush;
            SetupView.DomainCheck.BorderBrush = FindResource(_joinDomainRequested ? "AccentBrush" : "BorderBrush") as Brush;
            SetupView.DomainBox.Text = profile.Domain;
            SetupView.DomainUserBox.Text = profile.DomainUser;
            SetupView.DomainPasswordBox.Clear();

            var aliases = AppSelectionAliases
                .Where(pair => profile.ApplicationIds.Contains(pair.Value, StringComparer.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToArray();

            SetAppSelection(aliases);
            SetupView.AppsCheck.IsChecked = profile.InstallApplications && aliases.Length > 0;
            SetupView.AppsPreviewPanel.Visibility = Visibility.Collapsed;
            SetupView.PreviewAppsButton.Content = "POKAŻ WYBRANE APLIKACJE  ›";
            Log($"Wczytano profil: {dialog.FileName}. Dane uwierzytelniające nie są przechowywane w profilu.");
        }
        catch (Exception ex)
        {
            LogException("ODCZYTU PROFILU", ex);
            MessageBox.Show("Nie udało się wczytać profilu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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