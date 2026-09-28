using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();

    public MainWindow()
    {
        InitializeComponent();
        HostnameBox.Text = Environment.MachineName;
        PrivilegeText.Text = IsAdministrator() ? "UPRAWNIENIA ADMINISTRATORA" : "WYMAGANY ADMINISTRATOR";
        PrivilegeText.Foreground = IsAdministrator()
            ? System.Windows.Media.Brushes.LightGreen
            : System.Windows.Media.Brushes.Orange;
        Log("EDUFIXiarz uruchomiony.");
        Log($"Stacja: {Environment.MachineName}");
        Log(IsAdministrator()
            ? "Sesja posiada uprawnienia administratora."
            : "Uruchom aplikację jako administrator, aby wykonywać zmiany systemowe.");
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void Log(string message)
    {
        _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        LogBox.Text = _log.ToString();
        LogBox.ScrollToEnd();
    }

    private async void RunButton_Click(object sender, RoutedEventArgs e)
    {
        if (!IsAdministrator())
        {
            MessageBox.Show("EDUFIXiarz musi być uruchomiony jako administrator.",
                "Wymagane uprawnienia", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        RunButton.IsEnabled = false;
        try
        {
            if (HostnameCheck.IsChecked == true)
                await RunPowerShell("Rename-Computer -NewName '" + Escape(HostnameBox.Text.Trim()) + "' -Force",
                    "Zmiana hostname");

            if (DomainCheck.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(DomainBox.Text))
                {
                    Log("Pominięto domenę: pole jest puste.");
                }
                else
                {
                    Log("Dołączenie do domeny wymaga poświadczeń operatora i jest przygotowane do obsługi w kolejnym kroku.");
                    Log("Bezpieczny placeholder: nie zapisujemy haseł ani nie umieszczamy ich w parametrach procesu.");
                }
            }

            if (BloatwareCheck.IsChecked == true)
                await RunPowerShell("Get-AppxPackage -AllUsers | Where-Object { $_.Name -match 'McAfee|WildTangent|Booking|SpotifyAB|Clipchamp' } | ForEach-Object { Remove-AppxPackage -Package $_.PackageFullName -AllUsers -ErrorAction SilentlyContinue }",
                    "Usuwanie wybranych pakietów OEM");

            if (AppsCheck.IsChecked == true)
            {
                Log("Lista aplikacji bazowych jest przygotowana jako kolejny moduł instalatora.");
                Log("Docelowo aplikacje będą instalowane z jawnie zdefiniowanego katalogu pakietów.");
            }

            Log("Zakończono wybrane operacje.");
        }
        catch (Exception ex)
        {
            Log("BŁĄD: " + ex.Message);
            MessageBox.Show(ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RunButton.IsEnabled = true;
        }
    }

    private async Task RunPowerShell(string command, string label)
    {
        Log(label + "…");
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{command.Replace("\", "\\").Replace(""", "\"")}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Nie można uruchomić PowerShell.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(output)) Log(output.Trim());
        if (!string.IsNullOrWhiteSpace(error)) Log(error.Trim());
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{label} zakończone kodem {process.ExitCode}.");
        Log(label + " — OK.");
    }

    private static string Escape(string value) =>
        value.Replace("'", "''");
}
