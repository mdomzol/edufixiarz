using System.Windows;
using EDUFIXiarz.Models;
namespace EDUFIXiarz;

public partial class MainWindow : Window
{

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

}
