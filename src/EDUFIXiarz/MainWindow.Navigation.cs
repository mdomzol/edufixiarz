using System.Windows;
using System.Windows.Media;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private void ReportMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(ReportView);

    private void SetupMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(SetupView);

    private void AppsMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(AppsView);

    private void LogMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(LogView);

    private void DomainCheck_Click(object sender, RoutedEventArgs e)
    {
        _joinDomainRequested = !_joinDomainRequested;

        SetupView.DomainCredentialsExpander.Visibility =
            _joinDomainRequested ? Visibility.Visible : Visibility.Collapsed;
        SetupView.DomainCredentialsExpander.IsExpanded = _joinDomainRequested;
        SetupView.DomainCheck.Content =
            _joinDomainRequested ? "ANULUJ DOŁĄCZANIE DO DOMENY" : "DOŁĄCZ DO DOMENY AD";

        SetupView.DomainCheck.Background = _joinDomainRequested
            ? FindResource("PanelAltBrush") as Brush
            : FindResource("InputBrush") as Brush;
        SetupView.DomainCheck.BorderBrush = _joinDomainRequested
            ? FindResource("AccentBrush") as Brush
            : FindResource("BorderBrush") as Brush;

        if (!_joinDomainRequested)
            SetupView.DomainPasswordBox.Clear();

        Log(_joinDomainRequested
            ? "Włączono konfigurację dołączenia stacji do domeny AD."
            : "Wyłączono konfigurację dołączenia stacji do domeny AD.");
    }

    private void ShowPage(UIElement page)
    {
        ReportView.Visibility = Visibility.Collapsed;
        SetupView.Visibility = Visibility.Collapsed;
        AppsView.Visibility = Visibility.Collapsed;
        LogView.Visibility = Visibility.Collapsed;

        ReportMenuButton.Tag = null;
        SetupMenuButton.Tag = null;
        AppsMenuButton.Tag = null;
        LogMenuButton.Tag = null;

        page.Visibility = Visibility.Visible;

        var reportVisible = page == ReportView;
        ReportHeader.Visibility = reportVisible ? Visibility.Visible : Visibility.Collapsed;
        ReportStatusHeader.Visibility = reportVisible ? Visibility.Visible : Visibility.Collapsed;
        SetupHeader.Visibility = page == SetupView ? Visibility.Visible : Visibility.Collapsed;

        var active = page == ReportView ? ReportMenuButton
            : page == SetupView ? SetupMenuButton
            : page == AppsView ? AppsMenuButton
            : LogMenuButton;

        active.Tag = "Active";
    }
}