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

    public async Task<StationPreparation> RunAsync(
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

        var startedAt = DateTime.Now;
        var steps = new List<PreparationStep>();
        var requestedOperations = new List<string>();

        if (options.ChangeHostname) requestedOperations.Add("Zmiana nazwy stacji");
        if (options.RemoveBloatware) requestedOperations.Add("Czyszczenie pakietów AppX");
        if (options.RemoveOffice) requestedOperations.Add("Czyszczenie Office / Microsoft 365");
        if (options.InstallApplications && selectedApplications.Count > 0)
            requestedOperations.Add("Instalacja aplikacji");
        if (options.JoinDomain) requestedOperations.Add("Dołączenie do domeny AD");

        var restartRecommended = options.ChangeHostname || options.RemoveOffice || options.JoinDomain;
        var totalStages = requestedOperations.Count;
        var completedStages = 0;

        void ReportProgress(string label)
        {
            completedStages++;
            progress?.Invoke(completedStages, totalStages, label);
        }

        async Task RunStepAsync(string name, Func<Task> action, string successDetails)
        {
            try
            {
                await action();
                steps.Add(new PreparationStep
                {
                    Name = name,
                    Status = "OK",
                    Details = successDetails,
                    CompletedAt = DateTime.Now
                });
                ReportProgress(name);
            }
            catch (Exception ex)
            {
                steps.Add(new PreparationStep
                {
                    Name = name,
                    Status = "ERROR",
                    Details = ex.Message,
                    CompletedAt = DateTime.Now
                });
                throw;
            }
        }

        if (options.ChangeHostname)
        {
            await RunStepAsync(
                "Zmiana nazwy stacji",
                async () =>
                {
                    if (string.Equals(Environment.MachineName, options.Hostname, StringComparison.OrdinalIgnoreCase))
                    {
                        output?.Invoke($"Hostname jest już ustawiony jako {options.Hostname} — pomijam.");
                        return;
                    }

                    output?.Invoke("Zmiana hostname…");
                    await _powerShell.RunAsync(
                        $"Rename-Computer -NewName '{Escape(options.Hostname)}' -Force",
                        output,
                        error);
                    output?.Invoke("Zmiana hostname — OK.");
                },
                $"Docelowa nazwa: {options.Hostname}");
        }

        if (options.RemoveBloatware)
        {
            await RunStepAsync(
                "Czyszczenie pakietów AppX",
                async () =>
                {
                    output?.Invoke("Usuwanie wybranych pakietów OEM…");
                    await _bloatware.RemoveAsync(output, error);
                    output?.Invoke("Bloatware — etap AppX zakończony.");
                    output?.Invoke("Win32/OEM będzie obsługiwane przez profil pakietów w kolejnej iteracji.");
                },
                "Usuwanie wybranych pakietów AppX zakończone.");
        }

        if (options.RemoveOffice)
        {
            await RunStepAsync(
                "Czyszczenie Office / Microsoft 365",
                async () =>
                {
                    output?.Invoke("Czyszczenie Microsoft Office / Microsoft 365…");
                    await _office.RemoveAsync(output, error);
                    output?.Invoke("Office / Microsoft 365 — etap automatycznego czyszczenia zakończony.");
                    output?.Invoke("Po usunięciu zalecany jest restart przed instalacją licencjonowanego pakietu Office jednostki.");
                },
                "Automatyczne czyszczenie Office / Microsoft 365 zakończone.");
        }

        if (options.InstallApplications && selectedApplications.Count > 0)
        {
            await RunStepAsync(
                $"Instalacja aplikacji ({selectedApplications.Count})",
                async () =>
                {
                    output?.Invoke($"Instalacja wybranych aplikacji ({selectedApplications.Count})…");
                    await _applications.InstallAsync(selectedApplications, output, error);
                },
                $"Wybrano {selectedApplications.Count} aplikacji.");
        }

        if (options.JoinDomain)
        {
            await RunStepAsync(
                "Dołączenie do domeny AD",
                async () =>
                {
                    output?.Invoke($"Dołączanie do domeny {options.Domain}…");
                    await _domain.JoinAsync(options.Domain, options.DomainUser, options.DomainPassword!, output, error);
                    output?.Invoke("Dołączenie do domeny — OK.");
                    output?.Invoke("Dołączenie do domeny może wymagać ponownego uruchomienia stacji.");
                },
                $"Domena docelowa: {options.Domain}");
        }

        output?.Invoke("Wszystkie zaplanowane etapy zostały wykonane. Sprawdź dziennik pod kątem ostrzeżeń.");

        return new StationPreparation
        {
            StartedAt = startedAt,
            FinishedAt = DateTime.Now,
            Completed = true,
            RestartRecommended = restartRecommended,
            TargetHostname = options.ChangeHostname ? options.Hostname : "",
            Domain = options.JoinDomain ? options.Domain : "",
            RequestedOperations = requestedOperations,
            SelectedApplications = selectedApplications.Select(x => x.Id).ToList(),
            Steps = steps
        };
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
