using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;
using EDUFIXiarz.Models;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<PreparationProgressStep> _preparationProgressSteps = new();
    private bool _preparationFailed;

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
        _sessionId = Guid.NewGuid().ToString("N");
        _currentPreparation = null;
        _currentAudit = null;
        _preparationFailed = false;

        try
        {
            ApplyPreparationPowerSettings();

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

            InitializePreparationProgress(options);

            UpdatePreparationStage("ODCZYT BAZOWY", "RUNNING", "Pobieranie informacji o stacji…");
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
            UpdatePreparationStage("ODCZYT BAZOWY", "DONE", "Informacje o stacji odczytane.");

            UpdatePreparationStage("SNAPSHOT BAZOWY", "RUNNING", "Zapisywanie stanu przed zmianami…");
            var beforeSnapshot = new StationSnapshot
            {
                Stage = StationSnapshot.Stages.BeforePreparation,
                SessionId = _sessionId,
                StationId = _hardwareBeforePreparation.StationId,
                Hardware = _hardwareBeforePreparation,
                CapturedAt = DateTime.Now
            };
            try
            {
                var beforePaths = _stationSnapshotStorageService.Save(beforeSnapshot, _stationSnapshotExportService);
                Log($"Zapisano automatyczny snapshot bazowy: {beforePaths.CsvPath}");
                UpdatePreparationStage("SNAPSHOT BAZOWY", "DONE", "Stan bazowy zapisany.");
            }
            catch (Exception snapshotEx)
            {
                LogException("ZAPISU SNAPSHOTU BAZOWEGO", snapshotEx);
                UpdatePreparationStage("SNAPSHOT BAZOWY", "ERROR", snapshotEx.Message);
                throw;
            }

            _currentPreparation = await _setupService.RunAsync(
                options,
                GetSelectedApps(),
                LogOutput,
                LogError,
                UpdateOperationProgress);

            Log("Przygotowanie zakończone — wykonuję odczyt kontrolny stacji.");
            UpdatePreparationStage("ODCZYT KONTROLNY", "RUNNING", "Sprawdzanie stacji po zmianach…");
            var afterPreparation = await _hardwareService.GetReportAsync(LogError);
            _currentReport = afterPreparation;
            DataContext = afterPreparation;
            UpdatePreparationStage("ODCZYT KONTROLNY", "DONE", "Odczyt kontrolny zakończony.");

            UpdatePreparationStage("SNAPSHOT KOŃCOWY", "RUNNING", "Zapisywanie stanu po przygotowaniu…");
            var afterSnapshot = new StationSnapshot
            {
                Stage = StationSnapshot.Stages.AfterPreparation,
                SessionId = _sessionId,
                StationId = afterPreparation.StationId,
                Hardware = afterPreparation,
                CapturedAt = DateTime.Now
            };
            try
            {
                var afterPaths = _stationSnapshotStorageService.Save(afterSnapshot, _stationSnapshotExportService);
                Log($"Zapisano automatyczny snapshot końcowy: {afterPaths.CsvPath}");
                UpdatePreparationStage("SNAPSHOT KOŃCOWY", "DONE", "Stan końcowy zapisany.");
            }
            catch (Exception snapshotEx)
            {
                LogException("ZAPISU SNAPSHOTU KOŃCOWEGO", snapshotEx);
                UpdatePreparationStage("SNAPSHOT KOŃCOWY", "ERROR", snapshotEx.Message);
                throw;
            }

            Log("Uruchamiam audyt końcowy po przygotowaniu stacji.");
            UpdatePreparationStage("AUDYT KOŃCOWY", "RUNNING", "Weryfikacja gotowości stacji…");
            try
            {
                var previousAudit = _auditHistoryService.Load();
                var audit = await _auditService.RunAsync(LogOutput);
                _previousAudit = previousAudit;
                _currentAudit = audit;
                _auditHistoryService.Save(audit);
                AuditView.AuditGrid.ItemsSource = audit.Items;
                AuditView.OkCountText.Text = audit.OkCount.ToString();
                AuditView.WarningCountText.Text = audit.WarningCount.ToString();
                AuditView.ErrorCountText.Text = audit.ErrorCount.ToString();
                AuditView.AuditTimeText.Text = audit.CheckedAt.ToString("HH:mm:ss");
                AuditView.CompareAuditButton.IsEnabled = _previousAudit is not null;
                Log($"Audyt końcowy zakończony: OK={audit.OkCount}, WARN={audit.WarningCount}, ERROR={audit.ErrorCount}.");
                UpdatePreparationStage(
                    "AUDYT KOŃCOWY",
                    "DONE",
                    $"OK {audit.OkCount} · WARN {audit.WarningCount} · ERROR {audit.ErrorCount}");
            }
            catch (Exception auditEx)
            {
                _preparationFailed = true;
                LogException("AUDYTU KOŃCOWEGO", auditEx);
                UpdatePreparationStage("AUDYT KOŃCOWY", "ERROR", auditEx.Message);
            }

            Log($"Zakończono wybrane operacje: {_currentPreparation.Steps.Count} etapów.");
            ShowPage(LogView);
            MessageBox.Show(
                _preparationFailed
                    ? "Przygotowanie zakończyło się z błędem. Sprawdź stan etapów i dziennik."
                    : "Przygotowanie stanowiska zakończone. Niektóre zmiany mogą wymagać ponownego uruchomienia.",
                "EDUFIXiarz",
                MessageBoxButton.OK,
                _preparationFailed ? MessageBoxImage.Warning : MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            _preparationFailed = true;
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


    private void ApplyPreparationPowerSettings()
    {
        try
        {
            using (var powerKey = Registry.LocalMachine.OpenSubKey(
                @"SYSTEM\CurrentControlSet\Control\Session Manager\Power",
                writable: true))
            {
                if (powerKey is null)
                    throw new InvalidOperationException("Nie można otworzyć ustawień zasilania systemu Windows.");

                powerKey.SetValue("HiberbootEnabled", 0, RegistryValueKind.DWord);
            }

            RunPowerCfg("/change standby-timeout-ac 0");
            RunPowerCfg("/change standby-timeout-dc 0");
            RunPowerCfg("/change disk-timeout-ac 90");
            RunPowerCfg("/change disk-timeout-dc 90");

            Log("Ustawienia zasilania przygotowania: Szybkie uruchamianie WYŁĄCZONE · Uśpienie NIGDY (AC/DC) · Dysk 90 min (AC/DC).");
        }
        catch (Exception ex)
        {
            LogException("USTAWIEŃ ZASILANIA", ex);
            throw new InvalidOperationException(
                "Nie udało się zastosować wymaganych ustawień zasilania. Przygotowanie stacji zostało przerwane. " + ex.Message,
                ex);
        }
    }

    private static void RunPowerCfg(string arguments)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };

        process.Start();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd().Trim();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(error)
                    ? $"powercfg.exe zakończył działanie kodem {process.ExitCode}."
                    : error);
        }
    }

    private void InitializePreparationProgress(SetupOptions options)
    {
        _preparationProgressSteps.Clear();

        AddPreparationStep("ODCZYT BAZOWY");
        AddPreparationStep("SNAPSHOT BAZOWY");

        AddPreparationStep("Zmiana nazwy stacji", options.ChangeHostname);
        AddPreparationStep("Czyszczenie pakietów AppX", options.RemoveBloatware);
        AddPreparationStep("Czyszczenie Office / Microsoft 365", options.RemoveOffice);
        AddPreparationStep("Instalacja aplikacji", options.InstallApplications);
        AddPreparationStep("Dołączenie do domeny AD", options.JoinDomain);

        AddPreparationStep("ODCZYT KONTROLNY");
        AddPreparationStep("SNAPSHOT KOŃCOWY");
        AddPreparationStep("AUDYT KOŃCOWY");

        PreparationProgressItems.ItemsSource = _preparationProgressSteps;
        RefreshPreparationProgress();
    }

    private void AddPreparationStep(string name, bool enabled = true)
    {
        var step = new PreparationProgressStep
        {
            Number = _preparationProgressSteps.Count + 1,
            Name = name,
            Status = enabled ? "WAITING" : "SKIPPED",
            Details = enabled ? "Oczekuje" : "Etap niewybrany"
        };

        _preparationProgressSteps.Add(step);
    }

    private void UpdatePreparationStage(string name, string status, string details)
    {
        var step = _preparationProgressSteps.FirstOrDefault(x =>
            string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

        if (step is null)
            return;

        step.Status = status;
        step.Details = details;
        RefreshPreparationProgress();
    }

    private void UpdateOperationProgress(string label, string status, string details)
    {
        UpdatePreparationStage(label, status, details);
    }

    private void RefreshPreparationProgress()
    {
        var activeSteps = _preparationProgressSteps
            .Where(x => x.Status != "SKIPPED")
            .ToList();

        var completed = activeSteps.Count(x => x.Status == "DONE");
        var total = activeSteps.Count;
        var percentage = total == 0 ? 0 : completed * 100.0 / total;

        var progressAnimation = new System.Windows.Media.Animation.DoubleAnimation
        {
            To = percentage,
            Duration = TimeSpan.FromMilliseconds(320),
            EasingFunction = new System.Windows.Media.Animation.CubicEase
            {
                EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut
            }
        };

        OperationProgressBar.BeginAnimation(
            System.Windows.Controls.ProgressBar.ValueProperty,
            progressAnimation);

        var running = activeSteps.FirstOrDefault(x => x.Status == "RUNNING");
        var error = activeSteps.FirstOrDefault(x => x.Status == "ERROR");

        if (error is not null)
        {
            OperationProgressText.Text = $"BŁĄD · {error.Name}";
        }
        else if (running is not null)
        {
            OperationProgressText.Text = $"{completed:00} / {total:00} · {running.Name}";
        }
        else if (total > 0 && completed == total)
        {
            OperationProgressText.Text = $"ZAKOŃCZONO · {total:00} / {total:00}";
        }
        else
        {
            OperationProgressText.Text = $"GOTOWY · {completed:00} / {total:00}";
        }
    }

    private void SetSetupOperationState(bool isRunning)
    {
        SetupView.IsEnabled = !isRunning;
        AppsView.IsEnabled = !isRunning;
        AuditView.IsEnabled = !isRunning;

        ReportMenuButton.IsEnabled = !isRunning;
        SetupMenuButton.IsEnabled = !isRunning;
        AppsMenuButton.IsEnabled = !isRunning;
        AuditMenuButton.IsEnabled = !isRunning;
        LogMenuButton.IsEnabled = !isRunning;

        if (!isRunning)
            RefreshPreparationProgress();
        else
        {
            OperationProgressBar.Value = 0;
            OperationProgressText.Text = "PRZYGOTOWANIE W TOKU…";
        }
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

    private void ToggleDomainSetup_Click(object sender, RoutedEventArgs e)
    {
        _joinDomainRequested = !_joinDomainRequested;
        SetupView.DomainCredentialsPanel.Visibility = _joinDomainRequested ? Visibility.Visible : Visibility.Collapsed;
        SetupView.DomainCheck.Content = _joinDomainRequested ? "ANULUJ DOŁĄCZANIE DO DOMENY" : "DOŁĄCZ DO DOMENY AD";
        SetupView.DomainCheck.Background = FindResource(_joinDomainRequested ? "PanelAltBrush" : "InputBrush") as Brush;
        SetupView.DomainCheck.BorderBrush = FindResource(_joinDomainRequested ? "AccentBrush" : "BorderBrush") as Brush;
        Log(_joinDomainRequested ? "Włączono konfigurację dołączenia do domeny AD." : "Wyłączono konfigurację dołączenia do domeny AD.");
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
            SetupView.DomainCredentialsPanel.Visibility = _joinDomainRequested ? Visibility.Visible : Visibility.Collapsed;
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
        SetupView.DomainCredentialsPanel.Visibility = Visibility.Collapsed;
        SetupView.DomainCheck.Content = "DOŁĄCZ DO DOMENY AD";
        SetupView.DomainCheck.Background = FindResource("InputBrush") as Brush;
        SetupView.DomainCheck.BorderBrush = FindResource("BorderBrush") as Brush;
        SetupView.DomainBox.Clear();
        SetupView.DomainUserBox.Clear();
        SetupView.DomainPasswordBox.Clear();

        SetAppSelection();
        SetupView.AppsPreviewPanel.Visibility = Visibility.Collapsed;
        SetupView.PreviewAppsButton.Content = "POKAŻ WYBRANE APLIKACJE  ›";

        Log("Wybór zadań i aplikacji został wyzerowany.");
    }
}