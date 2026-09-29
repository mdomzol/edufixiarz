using System.Windows;

namespace EDUFIXiarz;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(
                "Wystąpił nieoczekiwany błąd aplikacji. Sprawdź dziennik EDUFIXiarza.",
                "EDUFIXiarz — błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);
    }
}
