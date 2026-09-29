using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;

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

    private void ExportReportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        ExportReport("json", _reportExportService.ToJson);
    }

    private void ExportReportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        ExportReport("csv", _reportExportService.ToCsv);
    }

    private void ExportReport(string extension, Func<HardwareReport, string> formatter)
    {
        if (_currentReport is null)
        {
            MessageBox.Show("Najpierw odczytaj raport sprzętowy.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"EDUFIXiarz-{_currentReport.Hostname}-{DateTime.Now:yyyyMMdd-HHmmss}.{extension}",
            Filter = extension == "json" ? "Raport JSON (*.json)|*.json" : "Raport CSV (*.csv)|*.csv",
            AddExtension = true,
            DefaultExt = extension,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var content = formatter(_currentReport);
            if (extension == "csv")
                File.WriteAllText(dialog.FileName, content, new UTF8Encoding(true));
            else
                File.WriteAllText(dialog.FileName, content, Encoding.UTF8);

            Log($"Wyeksportowano raport: {dialog.FileName}");
            MessageBox.Show("Raport został zapisany.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("EKSPORTU RAPORTU", ex);
            MessageBox.Show("Nie udało się zapisać raportu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
