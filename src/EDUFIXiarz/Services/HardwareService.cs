using System.Text;
using System.Text.Json;

namespace EDUFIXiarz.Services;

public sealed class HardwareService
{
    private readonly PowerShellService _powerShell;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string Script = @"
$os = Get-CimInstance Win32_OperatingSystem
$cs = Get-CimInstance Win32_ComputerSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$board = Get-CimInstance Win32_BaseBoard | Select-Object -First 1
$bios = Get-CimInstance Win32_BIOS | Select-Object -First 1
$ramModules = @(Get-CimInstance Win32_PhysicalMemory)
$ramArray = Get-CimInstance Win32_PhysicalMemoryArray | Select-Object -First 1
$gpus = @(Get-CimInstance Win32_VideoController)
$disks = @(Get-CimInstance Win32_DiskDrive)
$nics = @(Get-CimInstance Win32_NetworkAdapter | Where-Object { $_.PhysicalAdapter -eq $true -and $_.NetEnabled -eq $true })
$av = @(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct -ErrorAction SilentlyContinue)
function Safe($value) { if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) { return '—' }; return [string]$value }
function SizeGB($bytes) { if ($null -eq $bytes) { return '—' }; return ('{0:N1} GB' -f ([double]$bytes / 1GB)) }
[pscustomobject]@{
    Hostname = Safe $env:COMPUTERNAME
    SerialNumber = Safe $bios.SerialNumber
    Manufacturer = Safe $cs.Manufacturer
    Model = Safe $cs.Model
    Architecture = Safe $os.OSArchitecture
    OperatingSystem = Safe $os.Caption
    OsVersion = Safe ($os.Version + ' · build ' + $os.BuildNumber)
    Uptime = ((Get-Date) - $os.LastBootUpTime).ToString('ddd hhh mmm')
    Cpu = Safe $cpu.Name
    CpuCores = [int]$cpu.NumberOfCores
    CpuThreads = [int]$cpu.NumberOfLogicalProcessors
    Ram = SizeGB $cs.TotalPhysicalMemory
    RamSlots = if ($ramArray.MemoryDevices) { [int]$ramArray.MemoryDevices } else { [int]$ramModules.Count }
    RamUsedSlots = [int]$ramModules.Count
    Motherboard = Safe (($board.Manufacturer + ' ' + $board.Product).Trim())
    Bios = Safe (($bios.Manufacturer + ' ' + $bios.SMBIOSBIOSVersion).Trim())
    Gpus = @($gpus | ForEach-Object { Safe $_.Name } | Where-Object { $_ -ne '—' })
    PhysicalDisks = @($disks | ForEach-Object { $size = SizeGB $_.Size; if ($_.Model) { (Safe $_.Model) + ' · ' + $size } else { $size } })
    NetworkAdapters = @($nics | ForEach-Object { if ($_.Name) { $mac = if ($_.MACAddress) { ' · ' + $_.MACAddress } else { '' }; (Safe $_.Name) + $mac } })
    Antivirus = @($av | ForEach-Object { if ($_.displayName) { $_.displayName } } | Sort-Object -Unique)
} | ConvertTo-Json -Depth 4 -Compress
";

    public HardwareService(PowerShellService powerShell) => _powerShell = powerShell;

    public async Task<HardwareReport> GetReportAsync(Action<string>? error = null)
    {
        var json = await _powerShell.RunAsync(null, error);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("PowerShell nie zwrócił żadnych danych.");

        json = SanitizeJson(json);
        try
        {
            return JsonSerializer.Deserialize<HardwareReport>(json, JsonOptions)
                ?? throw new InvalidOperationException("PowerShell nie zwrócił poprawnego raportu.");
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "PowerShell zwrócił dane, których aplikacja nie potrafi odczytać.", ex);
        }
    }

    private Task<string> RunAsync(Action<string>? error)
        => _powerShell.RunAsync(Script, error: error);

    private static string SanitizeJson(string json)
    {
        var builder = new StringBuilder(json.Length);
        foreach (var character in json)
            builder.Append(character == '	' || character == '' || character == '
' || character >= ' ' ? character : ' ');
        return builder.ToString().Trim();
    }
}
