using System.IO;
using System.Text;
using System.Windows;

namespace EDUFIXiarz;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "EDU-FIX", "EDUFIXiarz", "Logs");
                Directory.CreateDirectory(directory);
                File.AppendAllText(
                    Path.Combine(directory, $"{DateTime.Now:yyyy-MM-dd}.log"),
                    $"[{DateTime.Now:HH:mm:ss}] NIEOBSŁUŻONY BŁĄD: {args.Exception}\r\n",
                    Encoding.UTF8);
            }
            catch
            {
                // Awaria logowania nie może zablokować komunikatu użytkownikowi.
            }

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
