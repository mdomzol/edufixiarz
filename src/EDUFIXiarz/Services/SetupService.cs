using EDUFIXiarz.Helpers;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class SetupService
{
    private readonly PowerShellService _powerShell;
    private readonly DomainService _domain;
    private readonly BloatwareService _bloatware;
    private readonly OfficeService _office;
    private readonly ApplicationService _applications;

    public SetupService(
        PowerShellService powerShell,
        DomainService domain,
        BloatwareService bloatware,
        OfficeService office,
        ApplicationService applications)
    {
        _powerShell = powerShell;
        _domain = domain;
        _bloatware = bloatware;
        _office = office;
        _applications = applications;
    }

    public async Task RunAsync(
        SetupOptions options,
        IEnumerable<AppDefinition> applications,
        Action<string>? output = null,
        Action<string>? error = null,
        Action<int, int, string>? progress = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(applications);

        var selectedApplications = applications.ToList();
        ValidateOptions(options, selectedApplications);

        var totalStages = (options.ChangeHostname ? 1 : 0)
            + (options.RemoveBloatware ? 1 : 0)
            + (options.RemoveOffice ? 1 : 0)
            + (options.InstallApplications && selectedApplications.Count > 0 ? 1 : 0)
            + (options.JoinDomain ? 1 : 0);
        var completedStages = 0;
        void ReportProgress(string label)
        {
            completedStages++;
            progress?.Invoke(completedStages, totalStages, label);
        }

        if (options.ChangeHostname)
        {
            output?.Invoke("Zmiana hostname…");
            await _powerShell.RunAsync(
                $"Rename-Computer -NewName '{Escape(options.Hostname)}' -Force",
                output,
                error);
            output?.Invoke("Zmiana hostname — OK.");
            ReportProgress("Zmiana nazwy stacji");
        }

        if (options.RemoveBloatware)
        {
            output?.Invoke("Usuwanie wybranych pakietów OEM…");
            await _bloatware.RemoveAsync(output, error);
            output?.Invoke("Bloatware — etap AppX zakończony.");
            output?.Invoke("Win32/OEM będzie obsługiwane przez profil pakietów w kolejnej iteracji.");
            ReportProgress("Czyszczenie pakietów AppX");
        }

        if (options.RemoveOffice)
        {
            output?.Invoke("Czyszczenie Microsoft Office / Microsoft 365…");
            await _office.RemoveAsync(output, error);
            output?.Invoke("Office / Microsoft 365 — etap automatycznego czyszczenia zakończony.");
            output?.Invoke("Po usunięciu zalecany jest restart przed instalacją licencjonowanego pakietu Office jednostki.");
            ReportProgress("Czyszczenie Office / Microsoft 365");
        }

        if (options.InstallApplications && selectedApplications.Count > 0)
        {
            output?.Invoke($"Instalacja wybranych aplikacji ({selectedApplications.Count})…");
            await _applications.InstallAsync(selectedApplications, output, error);
            ReportProgress($"Instalacja aplikacji ({selectedApplications.Count})");
        }

        if (options.JoinDomain)
        {
            output?.Invoke($"Dołączanie do domeny {options.Domain}…");
            await _domain.JoinAsync(options.Domain, options.DomainUser, options.DomainPassword!, output, error);
            output?.Invoke("Dołączenie do domeny — OK.");
            output?.Invoke("Dołączenie do domeny może wymagać ponownego uruchomienia stacji.");
            ReportProgress("Dołączenie do domeny AD");
        }

        output?.Invoke("Wszystkie zaplanowane etapy zostały wykonane. Sprawdź dziennik pod kątem ostrzeżeń.");
    }

    private static void ValidateOptions(SetupOptions options, IReadOnlyCollection<AppDefinition> applications)
    {
        if (!options.ChangeHostname &&
            !options.JoinDomain &&
            !options.RemoveBloatware &&
            !options.RemoveOffice &&
            !options.InstallApplications)
        {
            throw new InvalidOperationException("Nie wybrano żadnej operacji do wykonania.");
        }

        if (options.ChangeHostname && !HostnameValidator.IsValid(options.Hostname))
            throw new InvalidOperationException(
                "Hostname może zawierać maksymalnie 15 znaków i tylko litery, cyfry oraz myślnik.");

        if (options.JoinDomain)
        {
            if (string.IsNullOrWhiteSpace(options.Domain))
                throw new InvalidOperationException("Podaj nazwę domeny AD.");

            if (string.IsNullOrWhiteSpace(options.DomainUser))
                throw new InvalidOperationException("Podaj konto używane do dołączenia stacji do domeny AD.");

            if (options.DomainPassword is null || options.DomainPassword.Length == 0)
                throw new InvalidOperationException("Podaj hasło do konta domenowego.");
        }

        if (options.InstallApplications && applications.Count == 0)
            throw new InvalidOperationException("Włączono instalację aplikacji, ale nie wybrano żadnego programu.");
    }


    private static string Escape(string value) => value.Replace("'", "''");
}
