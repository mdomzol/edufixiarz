using System.Windows;
using System.Windows.Media;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private void ReportMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(ReportView);

    private void AuditMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(AuditView);

    private void SetupMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(SetupView);

    private void AppsMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(AppsView);

    private void LogMenuButton_Click(object sender, RoutedEventArgs e) =>
        ShowPage(LogView);

    private void ShowPage(UIElement page)
    {
        ReportView.Visibility = Visibility.Collapsed;
        AuditView.Visibility = Visibility.Collapsed;
        SetupView.Visibility = Visibility.Collapsed;
        AppsView.Visibility = Visibility.Collapsed;
        LogView.Visibility = Visibility.Collapsed;

        ReportMenuButton.Tag = null;
        AuditMenuButton.Tag = null;
        SetupMenuButton.Tag = null;
        AppsMenuButton.Tag = null;
        LogMenuButton.Tag = null;

        CollapsedReportMenuButton.Tag = null;
        CollapsedAuditMenuButton.Tag = null;
        CollapsedSetupMenuButton.Tag = null;
        CollapsedAppsMenuButton.Tag = null;
        CollapsedLogMenuButton.Tag = null;

        page.Visibility = Visibility.Visible;

        var reportVisible = page == ReportView;
        ReportHeader.Visibility = reportVisible ? Visibility.Visible : Visibility.Collapsed;
        AuditHeader.Visibility = page == AuditView ? Visibility.Visible : Visibility.Collapsed;
        SetupHeader.Visibility = page == SetupView ? Visibility.Visible : Visibility.Collapsed;

        var active = page == ReportView ? ReportMenuButton
            : page == AuditView ? AuditMenuButton
            : page == SetupView ? SetupMenuButton
            : page == AppsView ? AppsMenuButton
            : LogMenuButton;

        active.Tag = "Active";

        var collapsedActive = page == ReportView ? CollapsedReportMenuButton
            : page == AuditView ? CollapsedAuditMenuButton
            : page == SetupView ? CollapsedSetupMenuButton
            : page == AppsView ? CollapsedAppsMenuButton
            : CollapsedLogMenuButton;

        collapsedActive.Tag = "Active";
    }
}