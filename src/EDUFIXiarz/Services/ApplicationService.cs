using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class ApplicationService
{
    public static IReadOnlyList<AppDefinition> Applications { get; } =
    [
        new() { Id = "Adobe.Acrobat.Reader.64-bit", Name = "Adobe Acrobat Reader", Category = "PDF · DOKUMENTY" },
        new() { Id = "voidtools.Everything", Name = "Everything", Category = "WYSZUKIWANIE" },
        new() { Id = "Google.Chrome", Name = "Google Chrome", Category = "PRZEGLĄDARKA" },
        new() { Id = "Mozilla.Firefox", Name = "Mozilla Firefox", Category = "PRZEGLĄDARKA" },
        new() { Id = "7zip.7zip", Name = "7-Zip", Category = "ARCHIWIZACJA" },
        new() { Id = "Microsoft.VisualStudioCode", Name = "Visual Studio Code", Category = "PROGRAMOWANIE" },
        new() { Id = "VideoLAN.VLC", Name = "VLC", Category = "MEDIA" },
        new() { Id = "Notepad++.Notepad++", Name = "Notepad++", Category = "EDYTOR TEKSTU" },
        new() { Id = "TheDocumentFoundation.LibreOffice", Name = "LibreOffice", Category = "BIURO" },
        new() { Id = "PuTTY.PuTTY", Name = "PuTTY", Category = "SIEĆ · SSH" },
        new() { Id = "GIMP.GIMP", Name = "GIMP", Category = "GRAFIKA" }
    ];

    private readonly ProcessService _process;

    public ApplicationService(ProcessService process) => _process = process;

    public async Task InstallAsync(
        IEnumerable<AppDefinition> applications,
        Action<string>? output = null,
        Action<string>? error = null)
    {
        var selected = applications.ToList();
        if (selected.Count == 0)
            return;

        output?.Invoke("Sprawdzanie dostępności winget…");
        await _process.RunAsync(
            "winget.exe",
            ["--version"],
            output,
            error);

        output?.Invoke($"winget jest dostępny. Wybrano aplikacji: {selected.Count}.");

        foreach (var app in selected)
        {
            output?.Invoke($"Sprawdzanie: {app.Name}…");

            var installed = await IsInstalledAsync(app.Id, output, error);
            if (installed)
            {
                output?.Invoke($"{app.Name} — już zainstalowany, pomijam.");
                continue;
            }

            output?.Invoke($"Instalacja {app.Name}…");
            await _process.RunAsync(
                "winget.exe",
                [
                    "install",
                    "--id", app.Id,
                    "--exact",
                    "--silent",
                    "--accept-package-agreements",
                    "--accept-source-agreements",
                    "--disable-interactivity"
                ],
                output,
                error);

            output?.Invoke($"Instalacja {app.Name} — OK.");
        }

        output?.Invoke("Instalacja aplikacji — zakończona.");
    }

    private async Task<bool> IsInstalledAsync(
        string packageId,
        Action<string>? output,
        Action<string>? error)
    {
        var result = await _process.RunAsync(
            "winget.exe",
            [
                "list",
                "--id", packageId,
                "--exact",
                "--accept-source-agreements",
                "--disable-interactivity"
            ],
            output: null,
            error: error);

        return result.Contains(packageId, StringComparison.OrdinalIgnoreCase);
    }
}
