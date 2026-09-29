using System.Windows;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Views;

public partial class AuditComparisonWindow : Window
{
    public AuditComparisonWindow(AuditComparison comparison, StationAudit previous, StationAudit current)
    {
        InitializeComponent();

        HeaderText.Text = $"{previous.CheckedAt:yyyy-MM-dd HH:mm:ss}  →  {current.CheckedAt:yyyy-MM-dd HH:mm:ss} · {current.Hostname}";
        ImprovedText.Text = comparison.ImprovedCount.ToString();
        WorsenedText.Text = comparison.WorsenedCount.ToString();
        UnchangedText.Text = comparison.UnchangedCount.ToString();
        ComparisonGrid.ItemsSource = comparison.Changes;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
