using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();

    private static readonly (string Id, string Name)[] BaseApps =
    [
        ("Adobe.Acrobat.Reader.64-bit", "Adobe Acrobat Reader"),
        ("voidtools.Everything", "Everything"),
        ("Google.Chrome", "Google Chrome"),
        ("Mozilla.Firefox", "Mozilla Firefox"),
        ("7zip.7zip", "7-Zip"),
        ("Microsoft.VisualStudioCode", "Visual Studio Code"),
        ("VideoLAN.VLC", "VLC"),
        ("Notepad++.Notepad++", "Notepad++")
    ];

    private static readonly (string Id, string Name, Func<MainWindow, bool> Selected)[] OptionalApps =
    [
        ("TheDocumentFoundation.LibreOffice", "LibreOffice", w => w.LibreOfficeCheck.IsChecked == true),
        ("PuTTY.PuTTY", "PuTTY", w => w.PuttyCheck.IsChecked == true),
        ("GIMP.GIMP", "GIMP", w => w.GimpCheck.IsChecked == true)
    ];

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
            {
                var hostname = HostnameBox.Text.Trim();
                if (!IsValidHostname(hostname))
                    throw new InvalidOperationException("Hostname może zawierać maksymalnie 15 znaków i tylko litery, cyfry oraz myślnik.");

                await RunPowerShell(
                    $"Rename-Computer -NewName '{Escape(hostname)}' -Force",
                    "Zmiana hostname");
            }

            if (DomainCheck.IsChecked == true)
                await JoinDomainAsync();

            if (BloatwareCheck.IsChecked == true)
                await RemoveBloatwareAsync();

            if (OfficeCheck.IsChecked == true)
                await RemoveOfficeAsync();

            if (AppsCheck.IsChecked == true)
                await InstallBaseAppsAsync();

            await InstallOptionalAppsAsync();

            Log("Zakończono wybrane operacje.");
            MessageBox.Show("Przygotowanie stanowiska zakończone. Niektóre zmiany mogą wymagać ponownego uruchomienia.",
                "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
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

    private async Task JoinDomainAsync()
    {
        var domain = DomainBox.Text.Trim();
        var user = DomainUserBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(domain))
            throw new InvalidOperationException("Podaj nazwę domeny AD.");
        if (string.IsNullOrWhiteSpace(user))
            throw new InvalidOperationException("Podaj konto używane do dołączenia stacji do domeny AD.");
        if (DomainPasswordBox.SecurePassword.Length == 0)
            throw new InvalidOperationException("Podaj hasło do konta domenowego.");

        Log($"Dołączanie do domeny {domain}…");

        var command =
            "$secure = ConvertTo-SecureString ([Console]::In.ReadLine()) -AsPlainText -Force; " +
            $"$cred = New-Object System.Management.Automation.PSCredential('{Escape(user)}',$secure); " +
            $"Add-Computer -DomainName '{Escape(domain)}' -Credential $cred -Force -ErrorAction Stop";

        await RunPowerShellWithInput(command, SecurePasswordToPlainText(DomainPasswordBox.SecurePassword));
        DomainPasswordBox.Clear();
        Log("Dołączenie do domeny — OK.");
    }

    private async Task RemoveBloatwareAsync()
    {
        Log("Usuwanie wybranych pakietów OEM…");

        const string script = @"
$patterns = 'McAfee','WildTangent','Booking','Spotify','Clipchamp'
Get-AppxPackage -AllUsers |
    Where-Object { $name = $_.Name; $patterns | Where-Object { $name -like ('*' + $_ + '*') } } |
    ForEach-Object {
        Write-Output ('Usuwanie AppX: ' + $_.Name)
        Remove-AppxPackage -Package $_.PackageFullName -AllUsers -ErrorAction SilentlyContinue
    }
";

        await RunPowerShell(script, "Usuwanie pakietów AppX");
        Log("Bloatware — etap AppX zakończony.");
        Log("Win32/OEM będzie obsługiwane przez profil pakietów w kolejnej iteracji.");
    }

    private async Task RemoveOfficeAsync()
    {
        Log("Czyszczenie Microsoft Office / Microsoft 365…");

        const string script = @"
$officeAppx = Get-AppxPackage -AllUsers -Name 'Microsoft.Office.Desktop' -ErrorAction SilentlyContinue
foreach ($package in $officeAppx) {
    Write-Output ('Usuwanie Office AppX: ' + $package.Name)
    Remove-AppxPackage -Package $package.PackageFullName -AllUsers -ErrorAction Stop
}

$officeIds = @(
    'Microsoft.Office',
    'Microsoft.Office2016',
    'Microsoft.Office2019',
    'Microsoft.Office2021',
    'Microsoft.Office2024'
)

foreach ($id in $officeIds) {
    $installed = winget list --id $id --exact --accept-source-agreements 2>$null | Out-String
    if ($installed -match [regex]::Escape($id)) {
        Write-Output ('Usuwanie pakietu winget: ' + $id)
        winget uninstall --id $id --exact --silent --accept-source-agreements
    }
}
";

        await RunPowerShell(script, "Czyszczenie Microsoft Office / Microsoft 365");
        Log("Office / Microsoft 365 — etap automatycznego czyszczenia zakończony.");
        Log("Po usunięciu zalecany jest restart przed instalacją licencjonowanego pakietu Office jednostki.");
    }

    private async Task InstallBaseAppsAsync()
    {
        Log($"Instalacja aplikacji bazowych ({BaseApps.Length})…");

        foreach (var (id, name) in BaseApps)
        {
            await RunProcess("winget.exe",
                ["install", "--id", id, "--exact", "--silent",
                 "--accept-package-agreements", "--accept-source-agreements"],
                $"Instalacja {name}");
        }
    }

    private async Task InstallOptionalAppsAsync()
    {
        foreach (var (id, name, selected) in OptionalApps)
        {
            if (!selected(this))
                continue;

            await RunProcess("winget.exe",
                ["install", "--id", id, "--exact", "--silent",
                 "--accept-package-agreements", "--accept-source-agreements"],
                $"Instalacja {name}");
        }
    }

    private void ResetSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        HostnameCheck.IsChecked = false;
        DomainCheck.IsChecked = false;
        BloatwareCheck.IsChecked = false;
        AppsCheck.IsChecked = false;
        OfficeCheck.IsChecked = false;
        LibreOfficeCheck.IsChecked = false;
        PuttyCheck.IsChecked = false;
        GimpCheck.IsChecked = false;
        Log("Wybór zadań i aplikacji został wyzerowany.");
    }

    private async Task RunPowerShell(string command, string label)
    {
        await RunProcess("powershell.exe",
            ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", command],
            label);
    }

    private async Task RunPowerShellWithInput(string command, string secret)
    {
        Log("Przekazywanie poświadczeń…");

        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        foreach (var arg in new[] { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", command })
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException("Nie można uruchomić PowerShell.");
        await process.StandardInput.WriteLineAsync(secret);
        process.StandardInput.Close();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(output)) Log(output.Trim());
        if (!string.IsNullOrWhiteSpace(error)) Log(error.Trim());

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"Dołączenie do domeny zakończone kodem {process.ExitCode}.");
    }

    private async Task RunProcess(string fileName, IEnumerable<string> args, string label)
    {
        Log(label + "…");

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = Process.Start(psi) ?? throw new InvalidOperationException($"Nie można uruchomić {fileName}.");
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(output)) Log(output.Trim());
        if (!string.IsNullOrWhiteSpace(error)) Log(error.Trim());

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{label} zakończone kodem {process.ExitCode}.");

        Log(label + " — OK.");
    }

    private static bool IsValidHostname(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 15 &&
        value.All(c => char.IsLetterOrDigit(c) || c == '-') &&
        !value.StartsWith('-') &&
        !value.EndsWith('-');

    private static string Escape(string value) => value.Replace("'", "''");

    private static string SecurePasswordToPlainText(SecureString secure)
    {
        var ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secure);
        try { return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr) ?? string.Empty; }
        finally { System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr); }
    }
}
