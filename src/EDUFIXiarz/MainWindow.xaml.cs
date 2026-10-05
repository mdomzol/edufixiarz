using System.Security.Principal;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private readonly FullStationReportExportService _fullStationReportExportService;
    private readonly StationSnapshotExportService _stationSnapshotExportService;
    private readonly StationSnapshotStorageService _stationSnapshotStorageService;
    private readonly SnapshotComparisonService _snapshotComparisonService;
    private string _sessionId = Guid.NewGuid().ToString("N");
    private readonly ProfileService _profileService;
    private readonly StationAuditService _auditService;
    private readonly ProtocolExportService _protocolExportService;
    private readonly AuditHistoryService _auditHistoryService;
    private readonly AuditComparisonService _auditComparisonService;
    private StationAudit? _currentAudit;
    private StationPreparation? _currentPreparation;
    private HardwareReport? _hardwareBeforePreparation;
    private StationAudit? _previousAudit;
    private HardwareReport? _currentReport;
    private bool _joinDomainRequested;
    private bool _sidebarCollapsed;

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

        SourceInitialized += MainWindow_SourceInitialized;

        AuditView.RunAuditButton.Click += RunAuditButton_Click;
        AuditView.ExportProtocolButton.Click += ExportProtocolButton_Click;
        AuditView.LoadLastAuditButton.Click += LoadLastAuditButton_Click;
        AuditView.CompareAuditButton.Click += CompareAuditButton_Click;
        AuditView.HistoryButton.Click += HistoryButton_Click;
        SetupView.DomainCheck.Click += ToggleDomainSetup_Click;
        SetupView.PreviewAppsButton.Click += PreviewAppsButton_Click;
        SetupView.ResetSelectionButton.Click += ResetSelectionButton_Click;
        SetupView.RunButton.Click += RunButton_Click;
        SetupView.SchoolProfileButton.Click += SchoolProfileButton_Click;
        SetupView.OfficeProfileButton.Click += OfficeProfileButton_Click;
        SetupView.DeveloperProfileButton.Click += DeveloperProfileButton_Click;
        SetupView.FullProfileButton.Click += FullProfileButton_Click;
        SetupView.SaveProfileButton.Click += SaveProfileButton_Click;
        SetupView.LoadProfileButton.Click += LoadProfileButton_Click;

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
        _fullStationReportExportService = new FullStationReportExportService();
        _stationSnapshotExportService = new StationSnapshotExportService();
        _stationSnapshotStorageService = new StationSnapshotStorageService();
        _snapshotComparisonService = new SnapshotComparisonService();
        _profileService = new ProfileService();
        _auditService = new StationAuditService(_powerShellService);
        _protocolExportService = new ProtocolExportService();
        _auditHistoryService = new AuditHistoryService();
        _auditComparisonService = new AuditComparisonService();

        SetupView.HostnameBox.Text = Environment.MachineName;

        var isAdministrator = IsAdministrator();
        PrivilegeTitle.Text = isAdministrator ? "TRYB ADMINISTRATORA" : "TRYB UŻYTKOWNIKA";
        PrivilegeText.Text = isAdministrator
            ? "UPRAWNIENIA ADMINISTRATORA AKTYWNE"
            : "WYMAGANY ADMINISTRATOR";
        PrivilegeText.Foreground = isAdministrator
            ? (System.Windows.Media.Brush)FindResource("TextBrush")
            : Brushes.Orange;
        PrivilegeStatusCard.BorderBrush = isAdministrator
            ? (System.Windows.Media.Brush)FindResource("AccentBrush")
            : Brushes.DarkOrange;

        Log("EDUFIXiarz uruchomiony.");
        Log($"Stacja: {Environment.MachineName}");
        Log(IsAdministrator()
            ? "Sesja posiada uprawnienia administratora."
            : "Uruchom aplikację jako administrator, aby wykonywać zmiany systemowe.");

        ShowPage(ReportView);
        Loaded += async (_, _) => await LoadHardwareReportAsync();
    }

    private void SidebarToggleButton_Click(object sender, RoutedEventArgs e)
    {
        _sidebarCollapsed = !_sidebarCollapsed;

        var targetWidth = _sidebarCollapsed ? 48 : 252;
        var targetMargin = _sidebarCollapsed
            ? new Thickness(18, 24, 18, 20)
            : new Thickness(34, 30, 34, 24);
        var targetToggleMargin = _sidebarCollapsed
            ? new Thickness(7, 124, 0, 0)
            : new Thickness(20, 124, 0, 0);

        SidebarColumn.BeginAnimation(
            ColumnDefinition.WidthProperty,
            new GridLengthAnimation
            {
                From = SidebarColumn.Width,
                To = new GridLength(targetWidth),
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

        MainContent.BeginAnimation(
            MarginProperty,
            new ThicknessAnimation
            {
                To = targetMargin,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

        SidebarToggleButton.BeginAnimation(
            MarginProperty,
            new ThicknessAnimation
            {
                To = targetToggleMargin,
                Duration = TimeSpan.FromMilliseconds(220),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            });

        SidebarToggleButton.ToolTip = _sidebarCollapsed ? "Rozwiń menu boczne" : "Zwiń menu boczne";
        SidebarToggleButton.Tag = _sidebarCollapsed;

        if (_sidebarCollapsed)
        {
            SidebarContent.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(130),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });

            CollapsedSidebarContent.Visibility = Visibility.Visible;
            CollapsedSidebarContent.Opacity = 0;
            CollapsedSidebarContent.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    BeginTime = TimeSpan.FromMilliseconds(120),
                    Duration = TimeSpan.FromMilliseconds(140),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });
        }
        else
        {
            SidebarContent.Visibility = Visibility.Visible;
            SidebarContent.Opacity = 0;
            SidebarContent.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    From = 0,
                    To = 1,
                    BeginTime = TimeSpan.FromMilliseconds(100),
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                });

            CollapsedSidebarContent.BeginAnimation(
                OpacityProperty,
                new DoubleAnimation
                {
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(100),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                    Completed = (_, _) => CollapsedSidebarContent.Visibility = Visibility.Collapsed
                });
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleWindowState();
            return;
        }

        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            DragMove();
    }

    private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e) => ToggleWindowState();

    private void CloseWindowButton_Click(object sender, RoutedEventArgs e) => Close();

    private void ToggleWindowState()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        if (PresentationSource.FromVisual(this) is not System.Windows.Interop.HwndSource source)
            return;

        source.AddHook(WindowProc);
    }

    private static IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int WM_GETMINMAXINFO = 0x0024;

        if (msg == WM_GETMINMAXINFO)
        {
            var monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
                if (GetMonitorInfo(monitor, ref monitorInfo))
                {
                    var workArea = monitorInfo.rcWork;
                    var monitorArea = monitorInfo.rcMonitor;
                    var info = Marshal.PtrToStructure<MINMAXINFO>(lParam);

                    // MINMAXINFO uses physical pixels. WindowChrome already handles
                    // its resize frame, so use the monitor work area directly. This keeps
                    // the maximized window exactly inside the taskbar-safe area on both
                    // standard and high-resolution displays.
                    info.ptMaxPosition.X = workArea.Left - monitorArea.Left;
                    info.ptMaxPosition.Y = workArea.Top - monitorArea.Top;
                    info.ptMaxSize.X = workArea.Right - workArea.Left;
                    info.ptMaxSize.Y = workArea.Bottom - workArea.Top;

                    Marshal.StructureToPtr(info, lParam, false);
                    handled = true;
                }
            }
        }

        return IntPtr.Zero;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _log.AppendLine(line);
        LogView.LogBox.Text = _log.ToString();
        LogView.LogBox.ScrollToEnd();

        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EDU-FIX", "EDUFIXiarz", "Logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, $"{DateTime.Now:yyyy-MM-dd}.log"), line + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // Logowanie do pliku nie może przerwać operacji administracyjnej.
        }
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

    private void LogException(string context, Exception exception)
    {
        Log($"BŁĄD {context}: {exception.Message}");

        for (var inner = exception.InnerException; inner is not null; inner = inner.InnerException)
            Log($"SZCZEGÓŁ: {inner.Message}");
    }
}