using System.Windows;
using System.Windows.Media;
namespace EDUFIXiarz;

public partial class MainWindow : Window
{

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

}
