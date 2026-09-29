namespace EDUFIXiarz.Services;

public sealed class BloatwareService
{
    private readonly PowerShellService _powerShell;
    public BloatwareService(PowerShellService powerShell) => _powerShell = powerShell;

    public Task RemoveAsync(Action<string>? output = null, Action<string>? error = null) =>
        _powerShell.RunAsync(@"
$patterns = 'McAfee','WildTangent','Booking','Spotify','Clipchamp'
Get-AppxPackage -AllUsers | Where-Object { $name = $_.Name; $patterns | Where-Object { $name -like ('*' + $_ + '*') } } |
    ForEach-Object {
        Write-Output ('Usuwanie AppX: ' + $_.Name)
        Remove-AppxPackage -Package $_.PackageFullName -AllUsers -ErrorAction SilentlyContinue
    }
", output, error);
}
