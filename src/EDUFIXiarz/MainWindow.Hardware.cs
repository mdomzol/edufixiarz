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
        ApplicationVersion = "1.3.0",
        GeneratedAt = DateTime.Now,
        Hardware = _currentReport!,
        Preparation = _currentPreparation,
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
