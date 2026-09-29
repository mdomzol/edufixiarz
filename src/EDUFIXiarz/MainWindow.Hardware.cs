using System;
using System.Threading.Tasks;
using System.Windows;
namespace EDUFIXiarz;

public partial class MainWindow : Window
{

private async Task LoadHardwareReportAsync()
    {
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
    }

}
