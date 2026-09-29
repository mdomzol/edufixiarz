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
        try
        {
            var audit = await _auditService.RunAsync(LogOutput);
            _currentAudit = audit;
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
            AuditView.ExportProtocolButton.IsEnabled = _currentAudit is not null && _currentReport is not null;
        }
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
