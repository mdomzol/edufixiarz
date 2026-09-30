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
$computerProduct = Get-CimInstance Win32_ComputerSystemProduct | Select-Object -First 1
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$board = Get-CimInstance Win32_BaseBoard | Select-Object -First 1
$bios = Get-CimInstance Win32_BIOS | Select-Object -First 1
$ramModules = @(Get-CimInstance Win32_PhysicalMemory)
$ramArray = Get-CimInstance Win32_PhysicalMemoryArray | Select-Object -First 1
$gpus = @(Get-CimInstance Win32_VideoController)
$disks = @(Get-CimInstance Win32_DiskDrive)
$logicalDisks = @(Get-CimInstance Win32_LogicalDisk -Filter ""DriveType = 3"")
$nics = @(Get-CimInstance Win32_NetworkAdapter | Where-Object { $_.PhysicalAdapter -eq $true -and $_.NetEnabled -eq $true })
$netConfigs = @(Get-CimInstance Win32_NetworkAdapterConfiguration -ErrorAction SilentlyContinue | Where-Object { $_.IPEnabled -eq $true })
$av = @(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct -ErrorAction SilentlyContinue)
$uninstallRoots = @(""HKLM:SoftwareMicrosoftWindowsCurrentVersionUninstall*"", ""HKLM:SoftwareWOW6432NodeMicrosoftWindowsCurrentVersionUninstall*"")
$installedApps = @($uninstallRoots | ForEach-Object { Get-ItemProperty $_ -ErrorAction SilentlyContinue } | Where-Object { $_.DisplayName } | ForEach-Object { if ($_.DisplayVersion) { $_.DisplayName + "" · "" + $_.DisplayVersion } else { $_.DisplayName } } | Sort-Object -Unique | Select-Object -First 250)

function Safe($value) { if ($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) { return '—' }; return [string]$value }
function SizeGB($bytes) { if ($null -eq $bytes) { return '—' }; return ('{0:N1} GB' -f ([double]$bytes / 1GB)) }

$uptimeSpan = (Get-Date) - $os.LastBootUpTime
$uptime = '{0} d · {1} h · {2} min' -f [int]$uptimeSpan.TotalDays, $uptimeSpan.Hours, $uptimeSpan.Minutes

$gpuNames = @($gpus | ForEach-Object { Safe $_.Name } | Where-Object { $_ -ne '—' })
$physicalDisks = @($disks | ForEach-Object {
    $size = SizeGB $_.Size
    if ($_.Model) { (Safe $_.Model) + ' · ' + $size } else { $size }
})

$networkAdapters = @($netConfigs | ForEach-Object {
    $name = if ($_.Description) { Safe $_.Description } else { Safe $_.Caption }
    $mac = if ($_.MACAddress) { ' · ' + $_.MACAddress } else { '' }
    $ips = @($_.IPAddress | Where-Object { $_ -and ($_ -notlike 'fe80::*') })
    $ipText = if ($ips.Count -gt 0) { ' · IP: ' + ($ips -join ', ') } else { '' }
    $name + $mac + $ipText
})
if ($networkAdapters.Count -eq 0) {
    $networkAdapters = @($nics | ForEach-Object {
        if ($_.Name) {
            $mac = if ($_.MACAddress) { ' · ' + $_.MACAddress } else { '' }
            (Safe $_.Name) + $mac
        }
    })
}

$antivirusNames = @($av | ForEach-Object { if ($_.displayName) { $_.displayName } } | Sort-Object -Unique)
if ($antivirusNames.Count -eq 0) { $antivirusNames = @('—') }

$logicalDiskInfo = @($logicalDisks | ForEach-Object {
    $size = SizeGB $_.Size
    $free = SizeGB $_.FreeSpace
    (Safe $_.DeviceID) + ' · ' + $free + ' wolne / ' + $size
})

$tpm = '—'
try {
    $tpmInfo = Get-Tpm -ErrorAction Stop
    if ($tpmInfo.TpmPresent) { $tpm = if ($tpmInfo.TpmReady) { 'Obecny · gotowy' } else { 'Obecny · wymaga uwagi' } }
    else { $tpm = 'Brak' }
} catch { $tpm = 'Niedostępne' }

$secureBoot = 'Niedostępne'
try { $secureBoot = if (Confirm-SecureBootUEFI -ErrorAction Stop) { 'Włączony' } else { 'Wyłączony' } }
catch { $secureBoot = 'Niedostępne' }

$activation = 'Niedostępne'
try {
    $license = Get-CimInstance SoftwareLicensingProduct -Filter ""ApplicationID = '{55c92734-d682-4d71-983e-d6ec3f16059f}' AND PartialProductKey IS NOT NULL"" -ErrorAction Stop |
        Where-Object { $_.LicenseStatus -eq 1 } | Select-Object -First 1
    $activation = if ($license) { 'Aktywny' } else { 'Nieaktywowany' }
} catch { $activation = 'Niedostępne' }

$windowsUpdate = 'Niedostępne'
try {
    $wu = Get-Service -Name wuauserv -ErrorAction Stop
    $windowsUpdate = if ($wu.Status -eq 'Running') { 'Usługa uruchomiona' } else { 'Usługa: ' + $wu.Status }
} catch { $windowsUpdate = 'Niedostępne' }

$bitLocker = @()
$bitLockerVolumes = @()
try {
    $bitLockerVolumes = @(Get-BitLockerVolume -ErrorAction Stop | ForEach-Object {
        $mount = Safe $_.MountPoint
        $status = Safe $_.VolumeStatus
        $protection = Safe $_.ProtectionStatus
        $method = Safe $_.EncryptionMethod
        $percentage = if ($null -ne $_.EncryptionPercentage) { ('{0:N0}%' -f [double]$_.EncryptionPercentage) } else { '—' }
        $lock = Safe $_.LockStatus
        $protectors = @($_.KeyProtector | ForEach-Object {
            if ($_.KeyProtectorType) { [string]$_.KeyProtectorType }
        } | Sort-Object -Unique)
        $protectorText = if ($protectors.Count -gt 0) { $protectors -join ', ' } else { '—' }

        [pscustomobject]@{
            MountPoint = $mount
            VolumeStatus = $status
            ProtectionStatus = $protection
            EncryptionMethod = $method
            EncryptionPercentage = $percentage
            LockStatus = $lock
            KeyProtectors = $protectorText
        }
    })
    $bitLocker = @($bitLockerVolumes | ForEach-Object {
        $_.MountPoint + ' · ' + $_.VolumeStatus + ' · ochrona: ' + $_.ProtectionStatus
    })
    if ($bitLockerVolumes.Count -eq 0) {
        $bitLocker = @('Brak woluminów BitLocker')
    }
} catch {
    $bitLocker = @('Niedostępne')
    $bitLockerVolumes = @()
}

[pscustomobject]@{
    Hostname = Safe $env:COMPUTERNAME
    StationId = if ($bios.SerialNumber -and $bios.SerialNumber -ne '—') { Safe $bios.SerialNumber } elseif ($computerProduct.UUID) { Safe $computerProduct.UUID } else { Safe $env:COMPUTERNAME }
    DeviceUuid = Safe $computerProduct.UUID
    UserName = Safe ([System.Security.Principal.WindowsIdentity]::GetCurrent().Name)
    Domain = Safe $cs.Domain
    SerialNumber = Safe $bios.SerialNumber
    Manufacturer = Safe $cs.Manufacturer
    Model = Safe $cs.Model
    Architecture = Safe $os.OSArchitecture
    OperatingSystem = Safe $os.Caption
    OsVersion = Safe ($os.Version + ' · build ' + $os.BuildNumber)
    Uptime = $uptime
    Activation = $activation
    WindowsUpdate = $windowsUpdate
    Cpu = Safe $cpu.Name
    CpuCores = [int]$cpu.NumberOfCores
    CpuThreads = [int]$cpu.NumberOfLogicalProcessors
    Ram = SizeGB $cs.TotalPhysicalMemory
    RamSlots = if ($ramArray.MemoryDevices) { [int]$ramArray.MemoryDevices } else { [int]$ramModules.Count }
    RamUsedSlots = [int]$ramModules.Count
    Motherboard = Safe (($board.Manufacturer + ' ' + $board.Product).Trim())
    Bios = Safe (($bios.Manufacturer + ' ' + $bios.SMBIOSBIOSVersion).Trim())
    Tpm = $tpm
    SecureBoot = $secureBoot
    BitLocker = $bitLocker -join ' | '
    BitLockerVolumes = $bitLockerVolumes
    BitLockerDataReady = $true
    Gpus = $gpuNames
    PhysicalDisks = $physicalDisks
    LogicalDisks = $logicalDiskInfo
    NetworkAdapters = $networkAdapters
    Antivirus = $antivirusNames
    InstalledApplications = $installedApps
} | ConvertTo-Json -Depth 4 -Compress
";

    public HardwareService(PowerShellService powerShell) => _powerShell = powerShell;

    public async Task<HardwareReport> GetReportAsync(Action<string>? error = null)
    {
        try
        {
            var json = await RunAsync(error);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException("PowerShell nie zwrócił żadnych danych.");

            json = SanitizeJson(json);
            return JsonSerializer.Deserialize<HardwareReport>(json, JsonOptions)
                ?? throw new InvalidOperationException("PowerShell nie zwrócił poprawnego raportu.");
        }
        catch (InvalidOperationException) { throw; }
        catch (JsonException ex) { throw new InvalidOperationException("PowerShell zwrócił dane, których aplikacja nie potrafi odczytać.", ex); }
        catch (Exception ex) { throw new InvalidOperationException("Nie udało się odczytać informacji o sprzęcie.", ex); }
    }

    private Task<string> RunAsync(Action<string>? error) => _powerShell.RunAsync(Script, error: error);

    private static string SanitizeJson(string json)
    {
        var builder = new StringBuilder(json.Length);
        foreach (var character in json)
            builder.Append(character == '\t' || character == '\r' || character == '\n' || character >= ' ' ? character : ' ');
        return builder.ToString().Trim();
    }
}
