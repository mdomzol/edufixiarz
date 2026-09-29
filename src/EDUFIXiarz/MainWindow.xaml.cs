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

        _processService = new ProcessService();
        _powerShellService = new PowerShellService(_processService);
        _hardwareService = new HardwareService(_powerShellService);
        _domainService = new DomainService(_powerShellService);
        _bloatwareService = new BloatwareService(_powerShellService);
        _officeService = new OfficeService(_powerShellService);
        _applicationService = new ApplicationService(_processService);
        _setupService = new SetupService(_powerShellService, _domainService, _bloatwareService, _officeService, _applicationService);

        HostnameBox.Text = Environment.MachineName;
        PrivilegeText.Text = IsAdministrator() ? "UPRAWNIENIA ADMINISTRATORA" : "WYMAGANY ADMINISTRATOR";
        PrivilegeText.Foreground = IsAdministrator() ? Brushes.LightGreen : Brushes.Orange;

        Log("EDUFIXiarz uruchomiony.");
        Log($"Stacja: {Environment.MachineName}");
        Log(IsAdministrator()
            ? "Sesja posiada uprawnienia administratora."
            : "Uruchom aplikację jako administrator, aby wykonywać zmiany systemowe.");

        ShowPage(ReportPage);
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
        LogBox.Text = _log.ToString();
        LogBox.ScrollToEnd();
    }

    private void LogOutput(string output)
    {
        if (!string.IsNullOrWhiteSpace(output))
            Log(output);
    }

    private void LogError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            Log(error);
    }

    private void ReportMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(ReportPage);
    private void SetupMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(SetupPage);
    private void AppsMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(AppsPage);
    private void LogMenuButton_Click(object sender, RoutedEventArgs e) => ShowPage(LogPage);

    private void DomainCheck_Click(object sender, RoutedEventArgs e)
    {
        _joinDomainRequested = !_joinDomainRequested;
        DomainCredentialsExpander.Visibility = _joinDomainRequested ? Visibility.Visible : Visibility.Collapsed;
        DomainCredentialsExpander.IsExpanded = _joinDomainRequested;
        DomainCheck.Content = _joinDomainRequested ? "ANULUJ DOŁĄCZANIE DO DOMENY" : "DOŁĄCZ DO DOMENY AD";
        DomainCheck.Background = _joinDomainRequested
            ? FindResource("PanelAltBrush") as Brush
            : FindResource("InputBrush") as Brush;
        DomainCheck.BorderBrush = _joinDomainRequested
            ? FindResource("AccentBrush") as Brush
            : FindResource("BorderBrush") as Brush;

        if (!_joinDomainRequested)
            DomainPasswordBox.Clear();

        Log(_joinDomainRequested
            ? "Włączono konfigurację dołączenia stacji do domeny AD."
            : "Wyłączono konfigurację dołączenia stacji do domeny AD.");
    }

    private void ShowPage(UIElement page)
    {
        ReportPage.Visibility = Visibility.Collapsed;
        SetupPage.Visibility = Visibility.Collapsed;
        AppsPage.Visibility = Visibility.Collapsed;
        LogPage.Visibility = Visibility.Collapsed;

        ReportMenuButton.Tag = null;
        SetupMenuButton.Tag = null;
        AppsMenuButton.Tag = null;
        LogMenuButton.Tag = null;

        page.Visibility = Visibility.Visible;

        var reportVisible = page == ReportPage;
        ReportHeader.Visibility = reportVisible ? Visibility.Visible : Visibility.Collapsed;
        ReportStatusHeader.Visibility = reportVisible ? Visibility.Visible : Visibility.Collapsed;
        SetupHeader.Visibility = page == SetupPage ? Visibility.Visible : Visibility.Collapsed;

        var active = page == ReportPage ? ReportMenuButton
            : page == SetupPage ? SetupMenuButton
            : page == AppsPage ? AppsMenuButton
            : LogMenuButton;
        active.Tag = "Active";
    }

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

        RunButton.IsEnabled = false;
        try
        {
            var options = new SetupOptions
            {
                ChangeHostname = HostnameCheck.IsChecked == true,
                Hostname = HostnameBox.Text.Trim(),
                JoinDomain = _joinDomainRequested,
                Domain = DomainBox.Text.Trim(),
                DomainUser = DomainUserBox.Text.Trim(),
                DomainPassword = DomainPasswordBox.SecurePassword,
                RemoveBloatware = BloatwareCheck.IsChecked == true,
                RemoveOffice = OfficeCheck.IsChecked == true,
                InstallApplications = AppsCheck.IsChecked == true
            };

            await _setupService.RunAsync(options, GetSelectedApps(), LogOutput, LogError);
            DomainPasswordBox.Clear();

            Log("Zakończono wybrane operacje.");
            ShowPage(LogPage);
            MessageBox.Show(
                "Przygotowanie stanowiska zakończone. Niektóre zmiany mogą wymagać ponownego uruchomienia.",
                "EDUFIXiarz",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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

    private List<AppDefinition> GetSelectedApps()
    {
        var selectedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in AppSelectionAliases)
        {
            if (GetAppCheckBox(alias.Key)?.IsChecked == true)
                selectedIds.Add(alias.Value);
        }

        return ApplicationService.Applications
            .Where(app => selectedIds.Contains(app.Id))
            .ToList();
    }

    private CheckBox? GetAppCheckBox(string alias) => alias switch
    {
        "Adobe" => AdobeReaderCheck,
        "Everything" => EverythingCheck,
        "Chrome" => ChromeCheck,
        "Firefox" => FirefoxCheck,
        "7zip" => SevenZipCheck,
        "VSCode" => VscodeCheck,
        "VLC" => VlcCheck,
        "Notepad++" => NotepadPlusPlusCheck,
        "LibreOffice" => LibreOfficeCheck,
        "PuTTY" => PuttyCheck,
        "GIMP" => GimpCheck,
        _ => null
    };

    private void PreviewAppsButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = GetSelectedApps();

        SelectedAppsCountText.Text = selected.Count.ToString();
        AppsPreviewList.Children.Clear();

        if (selected.Count == 0)
        {
            AppsPreviewSummaryText.Text = "Brak aplikacji wskazanych do instalacji.";
        }
        else
        {
            var summary = selected.Count == 1 ? "aplikacja wskazana" : "aplikacje wskazane";
            AppsPreviewSummaryText.Text = $"{selected.Count} {summary} do instalacji.";

            for (var index = 0; index < selected.Count; index++)
            {
                var item = new Border
                {
                    BorderBrush = FindResource("BorderBrush") as Brush,
                    BorderThickness = new Thickness(0, index == 0 ? 0 : 1, 0, 0),
                    Padding = new Thickness(0, index == 0 ? 0 : 8, 0, 8)
                };

                var row = new Grid();
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var number = new TextBlock
                {
                    Text = $"{index + 1:00}",
                    FontSize = 10,
                    FontWeight = FontWeights.Bold,
                    Foreground = FindResource("AccentBrush") as Brush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var name = new TextBlock
                {
                    Text = selected[index].Name,
                    FontSize = 12,
                    Foreground = FindResource("TextBrush") as Brush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                Grid.SetColumn(name, 1);
                row.Children.Add(number);
                row.Children.Add(name);
                item.Child = row;
                AppsPreviewList.Children.Add(item);
            }
        }

        var isVisible = AppsPreviewPanel.Visibility == Visibility.Visible;
        AppsPreviewPanel.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
        PreviewAppsButton.Content = isVisible
            ? "POKAŻ WYBRANE APLIKACJE  ›"
            : "UKRYJ WYBRANE APLIKACJE  ‹";
    }

    private void SetAppSelection(params string[] aliases)
    {
        var selected = aliases.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in AppSelectionAliases.Keys)
        {
            var checkBox = GetAppCheckBox(alias);
            if (checkBox is not null)
                checkBox.IsChecked = selected.Contains(alias);
        }

        AppsCheck.IsChecked = aliases.Length > 0;
        UpdateSelectedAppsCount();
    }

    private void UpdateSelectedAppsCount()
    {
        SelectedAppsCountText.Text = GetSelectedApps().Count.ToString();
    }

    private void StandardPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Adobe", "Everything", "Chrome", "7zip", "VLC", "LibreOffice", "Notepad++");
        Log("Wybrano pakiet: Standardowe stanowisko biurowe.");
    }

    private void BasicPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Chrome", "7zip", "VLC", "Notepad++");
        Log("Wybrano pakiet: Podstawowy komputer.");
    }

    private void DeveloperPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Everything", "Chrome", "Firefox", "7zip", "VSCode", "PuTTY", "Notepad++");
        Log("Wybrano pakiet: Programista IT / DEV.");
    }

    private void GraphicsPackageButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection("Adobe", "Chrome", "7zip", "VLC", "LibreOffice", "GIMP", "Notepad++");
        Log("Wybrano pakiet: Multimedia i grafika.");
    }

    private void SelectAllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection(AppSelectionAliases.Keys.ToArray());
        Log("Zaznaczono wszystkie aplikacje.");
    }

    private void ClearAllAppsButton_Click(object sender, RoutedEventArgs e)
    {
        SetAppSelection();
        AppsCheck.IsChecked = false;
        Log("Odznaczono wszystkie aplikacje.");
    }

    private void ResetSelectionButton_Click(object sender, RoutedEventArgs e)
    {
        HostnameCheck.IsChecked = false;
        _joinDomainRequested = false;
        DomainCredentialsExpander.IsExpanded = false;
        DomainCredentialsExpander.Visibility = Visibility.Collapsed;
        DomainCheck.Content = "DOŁĄCZ DO DOMENY AD";
        DomainCheck.Background = FindResource("InputBrush") as Brush;
        DomainCheck.BorderBrush = FindResource("BorderBrush") as Brush;
        DomainPasswordBox.Clear();
        BloatwareCheck.IsChecked = false;
        OfficeCheck.IsChecked = false;
        ClearAllAppsButton_Click(sender, e);
        Log("Wybór zadań i aplikacji został wyzerowany.");
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
