using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EDUFIXiarz.Helpers;
using EDUFIXiarz.Models;
using EDUFIXiarz.Services;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private readonly StringBuilder _log = new();
    private readonly ProcessService _processService;
    private readonly PowerShellService _powerShellService;
    private readonly HardwareService _hardwareService;
    private readonly DomainService _domainService;
    private readonly BloatwareService _bloatwareService;
    private readonly OfficeService _officeService;
    private readonly ApplicationService _applicationService;
    private readonly SetupService _setupService;
    private bool _joinDomainRequested;

    private static readonly Dictionary<string, string> AppSelectionAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Adobe"] = "Adobe.Acrobat.Reader.64-bit",
        ["Everything"] = "voidtools.Everything",
        ["Chrome"] = "Google.Chrome",
        ["Firefox"] = "Mozilla.Firefox",
        ["7zip"] = "7zip.7zip",
        ["VSCode"] = "Microsoft.VisualStudioCode",
        ["VLC"] = "VideoLAN.VLC",
        ["Notepad++"] = "Notepad++.Notepad++",
        ["LibreOffice"] = "TheDocumentFoundation.LibreOffice",
        ["PuTTY"] = "PuTTY.PuTTY",
        ["GIMP"] = "GIMP.GIMP"
    };

    public MainWindow()
    {
        InitializeComponent();

        SetupView.DomainCheck.Click += DomainCheck_Click;
        SetupView.PreviewAppsButton.Click += PreviewAppsButton_Click;
        SetupView.ResetSelectionButton.Click += ResetSelectionButton_Click;
        SetupView.RunButton.Click += RunButton_Click;

        AppsView.SelectAllAppsButton.Click += SelectAllAppsButton_Click;
        AppsView.ClearAllAppsButton.Click += ClearAllAppsButton_Click;
        AppsView.StandardPackageButton.Click += StandardPackageButton_Click;
        AppsView.BasicPackageButton.Click += BasicPackageButton_Click;
        AppsView.DeveloperPackageButton.Click += DeveloperPackageButton_Click;
        AppsView.GraphicsPackageButton.Click += GraphicsPackageButton_Click;

        _processService = new ProcessService();
        _powerShellService = new PowerShellService(_processService);
        _hardwareService = new HardwareService(_powerShellService);
        _domainService = new DomainService(_powerShellService);
        _bloatwareService = new BloatwareService(_powerShellService);
        _officeService = new OfficeService(_powerShellService);
        _applicationService = new ApplicationService(_processService);
        _setupService = new SetupService(_powerShellService, _domainService, _bloatwareService, _officeService, _applicationService);

        SetupView.HostnameBox.Text = Environment.MachineName;
        UpdateSelectedAppsCount();

        PrivilegeText.Text = IsAdministrator() ? "UPRAWNIENIA ADMINISTRATORA" : "WYMAGANY ADMINISTRATOR";
        PrivilegeText.Foreground = IsAdministrator() ? Brushes.LightGreen : Brushes.Orange;

        Log("EDUFIXiarz uruchomiony.");
        Log($"Stacja: {Environment.MachineName}");
        Log(IsAdministrator()
            ? "Sesja posiada uprawnienia administratora."
            : "Uruchom aplikację jako administrator, aby wykonywać zmiany systemowe.");

        ShowPage(ReportView);
        Loaded += async (_, _) => await LoadHardwareReportAsync();
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void Log(string message)
    {
        _log.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        LogView.LogBox.Text = _log.ToString();
        LogView.LogBox.ScrollToEnd();
    }

    private void LogOutput(string output)
    {
        if (!string.IsNullOrWhiteSpace(output))
            Log(output);
    }

    private void LogError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            Log("BŁĄD PROCESU: " + error);
    }
}