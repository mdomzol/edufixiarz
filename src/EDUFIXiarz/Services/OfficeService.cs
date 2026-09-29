namespace EDUFIXiarz.Services;

public sealed class OfficeService
{
    private readonly PowerShellService _powerShell;
    public OfficeService(PowerShellService powerShell) => _powerShell = powerShell;

    public Task RemoveAsync(Action<string>? output = null, Action<string>? error = null) =>
        _powerShell.RunAsync(@"
$officeAppx = Get-AppxPackage -AllUsers -Name 'Microsoft.Office.Desktop' -ErrorAction SilentlyContinue
foreach ($package in $officeAppx) {
    Write-Output ('Usuwanie Office AppX: ' + $package.Name)
    Remove-AppxPackage -Package $package.PackageFullName -AllUsers -ErrorAction Stop
}
$officeIds = @('Microsoft.Office','Microsoft.Office2016','Microsoft.Office2019','Microsoft.Office2021','Microsoft.Office2024')
foreach ($id in $officeIds) {
    $installed = winget list --id $id --exact --accept-source-agreements 2>$null | Out-String
    if ($installed -match [regex]::Escape($id)) {
        Write-Output ('Usuwanie pakietu winget: ' + $id)
        winget uninstall --id $id --exact --silent --accept-source-agreements
    }
}
", output, error);
}
