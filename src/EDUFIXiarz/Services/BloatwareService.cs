namespace EDUFIXiarz.Services;

public sealed class BloatwareService
{
    private readonly PowerShellService _powerShell;

    public BloatwareService(PowerShellService powerShell) => _powerShell = powerShell;

    public Task RemoveAsync(Action<string>? output = null, Action<string>? error = null) =>
        _powerShell.RunAsync(@"
$patterns = @('McAfee','WildTangent','Booking','Spotify','Clipchamp')

$packages = Get-AppxPackage -AllUsers |
    Where-Object {
        $name = $_.Name
        $patterns | Where-Object { $name -like ('*' + $_ + '*') }
    } |
    Sort-Object PackageFullName -Unique

if (-not $packages) {
    Write-Output 'Nie znaleziono znanych pakietów AppX objętych profilem czyszczenia.'
    return
}

Write-Output ('Znaleziono pakietów AppX: ' + $packages.Count)

$removed = 0
$failed = 0

foreach ($package in $packages) {
    Write-Output ('Usuwanie AppX: ' + $package.Name)

    try {
        Remove-AppxPackage -Package $package.PackageFullName -AllUsers -ErrorAction Stop
        $removed++
        Write-Output ('Usunięto AppX — OK: ' + $package.Name)
    }
    catch {
        $failed++
        Write-Output ('OSTRZEŻENIE: Nie udało się usunąć AppX ' + $package.Name + ': ' + $_.Exception.Message)
    }
}

Write-Output ('Usunięto pakietów AppX: ' + $removed + ' · nieudane: ' + $failed)
if ($failed -gt 0) {
    Write-Output 'Czyszczenie AppX zakończono z ostrzeżeniami — pozostałe pakiety zostały przetworzone.'
}
", output, error);
}
