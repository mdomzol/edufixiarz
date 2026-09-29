using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace EDUFIXiarz;

public partial class MainWindow : Window
{
    private async void RunAuditButton_Click(object sender, RoutedEventArgs e)
    {
        AuditView.RunAuditButton.IsEnabled = false;
        AuditView.ExportProtocolButton.IsEnabled = false;
        AuditView.LoadLastAuditButton.IsEnabled = false;
        try
        {
            var audit = await _auditService.RunAsync(LogOutput);
            _currentAudit = audit;
            _auditHistoryService.Save(audit);
            AuditView.AuditGrid.ItemsSource = audit.Items;
            AuditView.OkCountText.Text = audit.OkCount.ToString();
            AuditView.WarningCountText.Text = audit.WarningCount.ToString();
            AuditView.ErrorCountText.Text = audit.ErrorCount.ToString();
            AuditView.AuditTimeText.Text = audit.CheckedAt.ToString("HH:mm:ss");
            Log($"Audyt stacji zakończony: OK={audit.OkCount}, WARN={audit.WarningCount}, ERROR={audit.ErrorCount}.");
        }
        catch (Exception ex)
        {
            LogException("AUDYTU STACJI", ex);
            MessageBox.Show("Nie udało się wykonać audytu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            AuditView.RunAuditButton.IsEnabled = true;
            AuditView.LoadLastAuditButton.IsEnabled = true;
            AuditView.ExportProtocolButton.IsEnabled = _currentAudit is not null && _currentReport is not null;
        }
    }

    private void LoadLastAuditButton_Click(object sender, RoutedEventArgs e)
    {
        var audit = _auditHistoryService.Load();
        if (audit is null)
        {
            MessageBox.Show("Brak zapisanego audytu tej stacji.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _currentAudit = audit;
        AuditView.AuditGrid.ItemsSource = audit.Items;
        AuditView.OkCountText.Text = audit.OkCount.ToString();
        AuditView.WarningCountText.Text = audit.WarningCount.ToString();
        AuditView.ErrorCountText.Text = audit.ErrorCount.ToString();
        AuditView.AuditTimeText.Text = audit.CheckedAt.ToString("HH:mm:ss");
        AuditView.ExportProtocolButton.IsEnabled = _currentReport is not null;
        Log($"Wczytano ostatni zapisany audyt: {audit.Hostname}, {audit.CheckedAt:yyyy-MM-dd HH:mm:ss}.");
    }

    private void ExportProtocolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentReport is null || _currentAudit is null)
        {
            MessageBox.Show("Najpierw odczytaj raport stacji i uruchom audyt.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            FileName = $"EDUFIXiarz-Protokol-{_currentReport.Hostname}-{DateTime.Now:yyyyMMdd-HHmmss}.html",
            Filter = "Protokół HTML (*.html)|*.html",
            AddExtension = true,
            DefaultExt = "html",
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            File.WriteAllText(dialog.FileName, _protocolExportService.ToHtml(_currentReport, _currentAudit));
            Log($"Wyeksportowano protokół odbioru: {dialog.FileName}");
            MessageBox.Show("Protokół został zapisany.", "EDUFIXiarz", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogException("EKSPORTU PROTOKOŁU", ex);
            MessageBox.Show("Nie udało się zapisać protokołu. " + ex.Message, "EDUFIXiarz — błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
