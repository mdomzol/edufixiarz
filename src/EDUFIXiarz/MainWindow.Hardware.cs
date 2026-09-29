using System;
using System.Threading.Tasks;
using System.Windows;

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
            DataContext = report;
            HardwareStatusText.Text = $"ODCZYTANO · {DateTime.Now:HH:mm:ss}";
            Log("Raport sprzętowy został odczytany.");
        }
        catch (Exception ex)
        {
            HardwareStatusText.Text = "NIE UDAŁO SIĘ ODCZYTAĆ RAPORTU";
            Log("BŁĄD RAPORTU SPRZĘTOWEGO: " + ex.Message);
        }
        finally
        {
            RefreshReportButton.IsEnabled = true;
        }
    }
}
