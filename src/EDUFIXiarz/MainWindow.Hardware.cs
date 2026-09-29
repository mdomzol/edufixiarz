using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using EDUFIXiarz.Models;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private async void RefreshReportButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadHardwareReportAsync();
    }

    private async Task LoadHardwareReportAsync()
    {
        RefreshReportButton.IsEnabled = false;
        HardwareStatusText.Text = "ODCZYTYWANIE INFORMACJI…";

        try
        {
            Log("Uruchamiam PowerShell/CIM do odczytu sprzętu…");
            var report = await _hardwareService.GetReportAsync(LogError);
            _currentReport = report;
            DataContext = report;
            HardwareStatusText.Text = $"ODCZYTANO · {DateTime.Now:HH:mm:ss}";
            Log("Raport sprzętowy został odczytany.");
        }
        catch (Exception ex)
        {
            HardwareStatusText.Text = "NIE UDAŁO SIĘ ODCZYTAĆ RAPORTU";
            LogException("RAPORTU SPRZĘTOWEGO", ex);
        }
        finally
        {
            RefreshReportButton.IsEnabled = true;
        }
    }

    private void ExportBeforeSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        if (_hardwareBeforePreparation is null)
        {
            MessageBox.Show("Brak zapisanego odczytu bazowego. Odczyt bazowy powstaje przy rozpoczęciu przygotowania stacji.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ExportSnapshot(_hardwareBeforePreparation, "PRZED-PRZYGOTOWANIEM");
    }

    private StationSnapshot CreateSnapshot(HardwareReport report, string stage) => new()
    {
        Stage = stage,
        Hardware = report,
        StationId = report.StationId,
        SessionId = _sessionId,
        CapturedAt = DateTime.Now
    };

    private void ExportSnapshot(HardwareReport report, string stage)
    {
        var snapshot = CreateSnapshot(report, stage);

        var dialog = new SaveFileDialog
        {
            FileName = $"EDUFIXiarz-Odczyt-{report.Hostname}-{stage}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Filter = "Odczyt stacji — CSV + HTML (*.csv)|*.csv",
            DefaultExt = "csv",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var csvPath = dialog.FileName;
            var htmlPath = Path.ChangeExtension(csvPath, ".html");
            File.WriteAllText(csvPath, _stationSnapshotExportService.ToCsv(snapshot), new UTF8Encoding(true));
            File.WriteAllText(htmlPath, _stationSnapshotExportService.ToHtml(snapshot), new UTF8Encoding(false));
            Log($"Wyeksportowano niezależny odczyt {stage}: {csvPath} + {htmlPath}");
            MessageBox.Show($"Zapisano odczyt {stage} jako CSV + HTML.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("EKSPORTU ODCZYTU STACJI", ex);
            MessageBox.Show("Nie udało się zapisać odczytu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportStationSnapshotButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentReport is null)
        {
            MessageBox.Show("Najpierw odczytaj raport stacji.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var stage = _hardwareBeforePreparation is not null && ReferenceEquals(_currentReport, _hardwareBeforePreparation)
            ? "PRZED-PRZYGOTOWANIEM"
            : _currentPreparation is not null
                ? "PO-PRZYGOTOWANIU"
                : "ODCZYT";

        var snapshot = CreateSnapshot(_currentReport, stage);

        var dialog = new SaveFileDialog
        {
            FileName = $"EDUFIXiarz-Odczyt-{_currentReport.Hostname}-{stage}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Filter = "Odczyt stacji — CSV + HTML (*.csv)|*.csv",
            DefaultExt = "csv",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var csvPath = dialog.FileName;
            var htmlPath = Path.ChangeExtension(csvPath, ".html");
            File.WriteAllText(csvPath, _stationSnapshotExportService.ToCsv(snapshot), new UTF8Encoding(true));
            File.WriteAllText(htmlPath, _stationSnapshotExportService.ToHtml(snapshot), new UTF8Encoding(false));
            Log($"Wyeksportowano niezależny odczyt stacji: {csvPath} + {htmlPath}");
            MessageBox.Show("Zapisano niezależny odczyt stacji jako CSV + HTML.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("EKSPORTU ODCZYTU STACJI", ex);
            MessageBox.Show("Nie udało się zapisać odczytu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportFullReportHtmlButton_Click(object sender, RoutedEventArgs e)
    {
        ExportFullStationReport("html");
    }

    private void ExportFullReportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        ExportFullStationReport("csv");
    }

    private void ExportFullStationReport(string extension)
    {
        if (_currentReport is null)
        {
            MessageBox.Show("Najpierw odczytaj raport stacji.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"EDUFIXiarz-Raport-{_currentReport.Hostname}-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}",
            Filter = extension == "html"
                ? "Raport HTML (*.html)|*.html"
                : "Raport CSV (*.csv)|*.csv",
            AddExtension = true,
            DefaultExt = extension,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var stationReport = CreateStationReport();
            var content = extension == "html"
                ? _fullStationReportExportService.ToHtml(stationReport)
                : _fullStationReportExportService.ToCsv(stationReport);

            File.WriteAllText(
                dialog.FileName,
                content,
                extension == "csv" ? new UTF8Encoding(true) : new UTF8Encoding(false));

            Log($"Wyeksportowano pełny raport stacji: {dialog.FileName}");
            MessageBox.Show("Pełny raport został zapisany.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("EKSPORTU PEŁNEGO RAPORTU", ex);
            MessageBox.Show("Nie udało się zapisać raportu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private StationReport CreateStationReport() => new()
    {
        ReportId = Guid.NewGuid().ToString("N"),
        FormatVersion = StationReport.CurrentFormatVersion,
        ApplicationVersion = Models.AppInfo.Version,
        GeneratedAt = DateTime.Now,
        BeforeSnapshot = _hardwareBeforePreparation is null ? null : CreateSnapshot(_hardwareBeforePreparation, StationSnapshot.Stages.BeforePreparation),
        Preparation = _currentPreparation,
        AfterSnapshot = _currentReport is null ? null : CreateSnapshot(_currentReport, _currentPreparation is null ? StationSnapshot.Stages.Read : StationSnapshot.Stages.AfterPreparation),
        Audit = _currentAudit
    };

    private void ExportStationReportBundleButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentReport is null)
        {
            MessageBox.Show("Najpierw odczytaj raport stacji.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"EDUFIXiarz-Raport-{_currentReport.Hostname}-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            Filter = "Komplet raportu — CSV + HTML (*.csv)|*.csv",
            AddExtension = true,
            DefaultExt = "csv",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var stationReport = CreateStationReport();
            var csvPath = dialog.FileName;
            var htmlPath = Path.ChangeExtension(csvPath, ".html");

            File.WriteAllText(csvPath, _fullStationReportExportService.ToCsv(stationReport), new UTF8Encoding(true));
            File.WriteAllText(htmlPath, _fullStationReportExportService.ToHtml(stationReport), new UTF8Encoding(false));

            Log($"Wyeksportowano komplet raportu: {csvPath} + {htmlPath}");
            MessageBox.Show(
                "Zapisano komplet raportu:\n\nCSV — dane dla EDUFIX Reader\nHTML — raport dla technika",
                "EDUFIXiarz",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("EKSPORTU KOMPLETU RAPORTU", ex);
            MessageBox.Show("Nie udało się zapisać kompletu raportu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
