using System.Linq;
using System.Windows;
using System.Windows.Controls;
using EDUFIXiarz.Models;
using EDUFIXiarz.Services;

namespace EDUFIXiarz.Views;

public partial class AuditHistoryWindow : Window
{
    private readonly AuditComparisonService _comparisonService = new();
    private readonly List<StationAudit> _history;
    private StationAudit? _selected;

    public AuditHistoryWindow(IEnumerable<StationAudit> history)
    {
        InitializeComponent();
        _history = history.OrderByDescending(x => x.CheckedAt).ToList();
        HistoryGrid.ItemsSource = _history;
        if (_history.Count > 0)
            HistoryGrid.SelectedIndex = 0;
    }

    private void HistoryGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selected = HistoryGrid.SelectedItem as StationAudit;
        if (_selected is null)
        {
            CompareSelectedButton.IsEnabled = false;
            return;
        }

        SelectedDateText.Text = _selected.CheckedAt.ToString("yyyy-MM-dd HH:mm:ss");
        SelectedHostText.Text = _selected.Hostname;
        SelectedOkText.Text = _selected.OkCount.ToString();
        SelectedWarnText.Text = _selected.WarningCount.ToString();
        SelectedErrorText.Text = _selected.ErrorCount.ToString();
        ItemsGrid.ItemsSource = _selected.Items;
        var index = _history.IndexOf(_selected);
        CompareSelectedButton.IsEnabled = index >= 0 && index < _history.Count - 1;
    }

    private void CompareSelectedButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selected is null) return;
        var index = _history.IndexOf(_selected);
        if (index < 0 || index >= _history.Count - 1) return;

        var previous = _history[index + 1];
        var comparison = _comparisonService.Compare(previous, _selected);
        new AuditComparisonWindow(comparison, previous, _selected) { Owner = this }.ShowDialog();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}