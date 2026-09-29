namespace EDUFIXiarz.Services;

public sealed class BloatwareService
{
    private readonly PowerShellService _powerShell;

    public BloatwareService(PowerShellService powerShell) => _powerShell = powerShell;

    public Task RemoveAsync(Action<string>? output = null, Action<string>? error = null) =>
        _powerShell.RunAsync(@"
$patterns = 'McAfee','WildTangent','Booking','Spotify','Clipchamp'
$packages = Get-AppxPackage -AllUsers |
    Where-Object {
        $name = $_.Name
        $patterns | Where-Object { $name -like ('*' + $_ + '*') }
    }

if (-not $packages) {
    Write-Output 'Nie znaleziono znanych pakietów bloatware.'
    return
}

$removed = 0
foreach ($package in $packages) {
    Write-Output ('Usuwanie AppX: ' + $package.Name)
    try {
        Remove-AppxPackage -Package $package.PackageFullName -AllUsers -ErrorAction Stop
        $removed++
    }
    catch {
        throw ('Nie udało się usunąć AppX ' + $package.Name + ': ' + $_.Exception.Message)
    }
}

Write-Output ('Usunięto pakietów AppX: ' + $removed)
", output, error); 
}