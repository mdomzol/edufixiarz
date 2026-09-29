namespace EDUFIXiarz.Services;

public sealed class OfficeService
{
    private readonly PowerShellService _powerShell;

    public OfficeService(PowerShellService powerShell) => _powerShell = powerShell;

    public Task RemoveAsync(Action<string>? output = null, Action<string>? error = null) =>
        _powerShell.RunAsync(@"
$officeAppx = Get-AppxPackage -AllUsers -Name 'Microsoft.Office.Desktop' -ErrorAction SilentlyContinue
$failed = 0
$removed = 0

foreach ($package in $officeAppx) {
    Write-Output ('Usuwanie Office AppX: ' + $package.Name)

    try {
        Remove-AppxPackage -Package $package.PackageFullName -AllUsers -ErrorAction Stop
        $removed++
        Write-Output ('Office AppX — OK: ' + $package.Name)
    }
    catch {
        $failed++
        Write-Output ('OSTRZEŻENIE: Nie udało się usunąć Office AppX ' + $package.Name + ': ' + $_.Exception.Message)
    }
}

if (-not (Get-Command winget.exe -ErrorAction SilentlyContinue)) {
    Write-Output ('Office AppX — usunięto: ' + $removed + ' · nieudane: ' + $failed)
    Write-Output 'winget nie jest dostępny — pominięto sprawdzanie pakietów Office Win32.'
    return
}

$officeIds = @('Microsoft.Office','Microsoft.Office2016','Microsoft.Office2019','Microsoft.Office2021','Microsoft.Office2024')

foreach ($id in $officeIds) {
    $installed = winget list --id $id --exact --accept-source-agreements 2>$null | Out-String

    if ($LASTEXITCODE -ne 0) {
        continue
    }

    if ($installed -match [regex]::Escape($id)) {
        Write-Output ('Usuwanie pakietu winget: ' + $id)

        try {
            winget uninstall --id $id --exact --silent --accept-source-agreements

            if ($LASTEXITCODE -ne 0) {
                throw ('Kod wyjścia: ' + $LASTEXITCODE)
            }

            $removed++
            Write-Output ('Pakiet winget — OK: ' + $id)
        }
        catch {
            $failed++
            Write-Output ('OSTRZEŻENIE: Nie udało się usunąć pakietu winget ' + $id + ': ' + $_.Exception.Message)
        }
    }
}

Write-Output ('Czyszczenie Office zakończone · usunięte: ' + $removed + ' · nieudane: ' + $failed)
if ($failed -gt 0) {
    Write-Output 'Czyszczenie Office zakończono z ostrzeżeniami — pozostałe składniki zostały przetworzone.'
}
", output, error);
}
