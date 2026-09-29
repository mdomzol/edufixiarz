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
        Action<string>? error = null)
    {
        if (options.ChangeHostname)
        {
            if (!HostnameValidator.IsValid(options.Hostname))
                throw new InvalidOperationException(
                    "Hostname może zawierać maksymalnie 15 znaków i tylko litery, cyfry oraz myślnik.");

            output?.Invoke("Zmiana hostname…");
            await _powerShell.RunAsync(
                $"Rename-Computer -NewName '{Escape(options.Hostname)}' -Force",
                output,
                error);
            output?.Invoke("Zmiana hostname — OK.");
        }

        if (options.JoinDomain)
        {
            output?.Invoke($"Dołączanie do domeny {options.Domain}…");
            await _domain.JoinAsync(options.Domain, options.DomainUser, options.DomainPassword!, output, error);
            output?.Invoke("Dołączenie do domeny — OK.");
        }

        if (options.RemoveBloatware)
        {
            output?.Invoke("Usuwanie wybranych pakietów OEM…");
            await _bloatware.RemoveAsync(output, error);
            output?.Invoke("Bloatware — etap AppX zakończony.");
            output?.Invoke("Win32/OEM będzie obsługiwane przez profil pakietów w kolejnej iteracji.");
        }

        if (options.RemoveOffice)
        {
            output?.Invoke("Czyszczenie Microsoft Office / Microsoft 365…");
            await _office.RemoveAsync(output, error);
            output?.Invoke("Office / Microsoft 365 — etap automatycznego czyszczenia zakończony.");
            output?.Invoke("Po usunięciu zalecany jest restart przed instalacją licencjonowanego pakietu Office jednostki.");
        }

        if (options.InstallApplications)
        {
            var selected = applications.ToList();
            output?.Invoke($"Instalacja wybranych aplikacji ({selected.Count})…");
            await _applications.InstallAsync(selected, output, error);
        }
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
