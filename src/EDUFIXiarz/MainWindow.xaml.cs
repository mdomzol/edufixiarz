using System.Diagnostics;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();

    private static readonly (string Id, string Name, Func<MainWindow, bool> Selected)[] Apps =
    [
        ("Adobe.Acrobat.Reader.64-bit", "Adobe Acrobat Reader", w => w.AdobeReaderCheck.IsChecked == true),
        ("voidtools.Everything", "Everything", w => w.EverythingCheck.IsChecked == true),
        ("Google.Chrome", "Google Chrome", w => w.ChromeCheck.IsChecked == true),
        ("Mozilla.Firefox", "Mozilla Firefox", w => w.FirefoxCheck.IsChecked == true),
        ("7zip.7zip", "7-Zip", w => w.SevenZipCheck.IsChecked == true),
        ("Microsoft.VisualStudioCode", "Visual Studio Code", w => w.VscodeCheck.IsChecked == true),
        ("VideoLAN.VLC", "VLC", w => w.VlcCheck.IsChecked == true),
        ("Notepad++.Notepad++", "Notepad++", w => w.NotepadPlusPlusCheck.IsChecked == true),
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
            ? Brushes.LightGreen
            : Brushes.Orange;

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

    private void SetupMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(SetupPage);
    private void AppsMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(AppsPage);
    private void LogMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(LogPage);

    private void ShowPage(UIElement page)
    {
        SetupPage.Visibility = Visibility.Collapsed;
        AppsPage.Visibility = Visibility.Collapsed;
        LogPage.Visibility = Visibility.Collapsed;

        SetupMenuButton.Background = (Brush)FindResource("PanelAltBrush");
        AppsMenuButton.Background = (Brush)FindResource("PanelAltBrush");
        LogMenuButton.Background = (Brush)FindResource("PanelAltBrush");

        page.Visibility = Visibility.Visible;

        var activeButton = page == SetupPage ? SetupMenuButton
            : page == AppsPage ? AppsMenuButton
            : LogMenuButton;

        activeButton.Background = (Brush)FindResource("AccentBrush");
        activeButton.Foreground = Brushes.White;
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
                await InstallSelectedAppsAsync();

            Log("Zakończono wybrane operacje.");
            ShowPage(LogPage);
            MessageBox.Show("Przygotowanie stanowiska zakończone. Niektóre zmiany mogą wymagać ponownego uruchomienia.",
                "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log("BŁĄD: " + ex.Message);
            ShowPage(LogPage);
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

    private async Task InstallSelectedAppsAsync()
    {
        var selectedApps = Apps.Where(a => a.Selected(this)).ToArray();
        Log($"Instalacja wybranych aplikacji ({selectedApps.Length})…");

        foreach (var (id, name, _) in selectedApps)
        {
            await RunProcess("winget.exe",
                ["install", "--id", id, "--exact", "--silent",
                 "--accept-package-agreements", "--accept-source-agreements"],
                $"Instalacja {name}");
        }
    }

    private void SelectAllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        AdobeReaderCheck.IsChecked = true;
        EverythingCheck.IsChecked = true;
        ChromeCheck.IsChecked = true;
        FirefoxCheck.IsChecked = true;
        SevenZipCheck.IsChecked = true;
        VscodeCheck.IsChecked = true;
        VlcCheck.IsChecked = true;
        NotepadPlusPlusCheck.IsChecked = true;
        LibreOfficeCheck.IsChecked = true;
        PuttyCheck.IsChecked = true;
        GimpCheck.IsChecked = true;
        AppsCheck.IsChecked = true;
        Log("Zaznaczono wszystkie aplikacje.");
    }

    private void ClearAllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        AdobeReaderCheck.IsChecked = false;
        EverythingCheck.IsChecked = false;
        ChromeCheck.IsChecked = false;
        FirefoxCheck.IsChecked = false;
        SevenZipCheck.IsChecked = false;
        VscodeCheck.IsChecked = false;
        VlcCheck.IsChecked = false;
        NotepadPlusPlusCheck.IsChecked = false;
        LibreOfficeCheck.IsChecked = false;
        PuttyCheck.IsChecked = false;
        GimpCheck.IsChecked = false;
        Log("Odznaczono wszystkie aplikacje.");
    }

    private void ResetSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        HostnameCheck.IsChecked = false;
        DomainCheck.IsChecked = false;
        BloatwareCheck.IsChecked = false;
        AppsCheck.IsChecked = false;
        OfficeCheck.IsChecked = false;
        ClearAllAppsButton_Click(sender, e);
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
