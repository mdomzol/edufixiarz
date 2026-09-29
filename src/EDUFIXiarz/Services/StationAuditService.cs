using System.Text.Json;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class StationAuditService
{
    private readonly PowerShellService _powerShell;
    public StationAuditService(PowerShellService powerShell) => _powerShell = powerShell;

    public async Task<StationAudit> RunAsync(Action<string>? log = null)
    {
        log?.Invoke("Uruchamiam audyt stacji…");
        var json = await _powerShell.RunAsync(Script, error: log);
        var data = JsonSerializer.Deserialize<AuditData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Audyt nie zwrócił poprawnych danych.");
        var items = new List<AuditItem>
        {
            Item("Windows Update", data.WindowsUpdate, data.WindowsUpdateDetails),
            Item("Aktywacja Windows", data.Activation, data.ActivationDetails),
            Item("TPM", data.Tpm, data.TpmDetails),
            Item("Secure Boot", data.SecureBoot, data.SecureBootDetails),
            Item("BitLocker", data.BitLocker, data.BitLockerDetails),
            Item("Miejsce na dysku systemowym", data.SystemDrive, data.SystemDriveDetails),
            Item("Administratorzy lokalni", data.LocalAdmins, data.LocalAdminsDetails),
            Item("Usługi Windows", data.Services, data.ServicesDetails),
            Item("WinGet", data.WinGet, data.WinGetDetails),
            Item("Oczekujący restart", data.PendingRestart, data.PendingRestartDetails)
        };
        return new StationAudit { CheckedAt = DateTime.Now, Hostname = Environment.MachineName, Items = items };
    }

    private static AuditItem Item(string name, string status, string details) =>
        new() { Name = name, Status = status, Value = status, Details = details };

    private sealed class AuditData
    {
        public string WindowsUpdate { get; set; } = "WARN"; public string WindowsUpdateDetails { get; set; } = "";
        public string Activation { get; set; } = "WARN"; public string ActivationDetails { get; set; } = "";
        public string Tpm { get; set; } = "WARN"; public string TpmDetails { get; set; } = "";
        public string SecureBoot { get; set; } = "WARN"; public string SecureBootDetails { get; set; } = "";
        public string BitLocker { get; set; } = "WARN"; public string BitLockerDetails { get; set; } = "";
        public string SystemDrive { get; set; } = "WARN"; public string SystemDriveDetails { get; set; } = "";
        public string LocalAdmins { get; set; } = "WARN"; public string LocalAdminsDetails { get; set; } = "";
        public string Services { get; set; } = "WARN"; public string ServicesDetails { get; set; } = "";
        public string WinGet { get; set; } = "WARN"; public string WinGetDetails { get; set; } = "";
        public string PendingRestart { get; set; } = "WARN"; public string PendingRestartDetails { get; set; } = "";
    }

    private const string Script = @"
$wu = Get-Service wuauserv -ErrorAction SilentlyContinue
$windowsUpdate = if ($wu -and $wu.Status -eq 'Running') { 'OK' } else { 'WARN' }
$windowsUpdateDetails = if ($wu) { 'Usługa Windows Update: ' + $wu.Status } else { 'Nie znaleziono usługi Windows Update.' }

$license = Get-CimInstance SoftwareLicensingProduct -Filter ""ApplicationID='55c92734-d682-4d71-983e-d6ec3f16059f' AND PartialProductKey IS NOT NULL"" -ErrorAction SilentlyContinue | Select-Object -First 1
$activation = if ($license -and $license.LicenseStatus -eq 1) { 'OK' } else { 'WARN' }
$activationDetails = if ($license) { 'LicenseStatus: ' + $license.LicenseStatus } else { 'Nie znaleziono wpisu licencji.' }

$tpm = 'WARN'; $tpmDetails = 'Brak danych.'
try { $t=Get-Tpm -ErrorAction Stop; $tpm=if($t.TpmPresent -and $t.TpmReady){'OK'}elseif($t.TpmPresent){'WARN'}else{'ERROR'}; $tpmDetails=if($t.TpmPresent){'TPM obecny; gotowość: '+$t.TpmReady}else{'TPM nie jest obecny.'} } catch { $tpmDetails='Get-Tpm: '+$_.Exception.Message }

$secureBoot='WARN'; $secureBootDetails='Niedostępne.'
try { $sb=Confirm-SecureBootUEFI -ErrorAction Stop; $secureBoot=if($sb){'OK'}else{'WARN'}; $secureBootDetails=if($sb){'Secure Boot jest włączony.'}else{'Secure Boot jest wyłączony.'} } catch { $secureBootDetails='Sprawdzenie niedostępne (np. BIOS/Legacy).' }

$v=@(Get-BitLockerVolume -ErrorAction SilentlyContinue); $sv=$v|Where-Object {$_.MountPoint -eq $env:SystemDrive}|Select-Object -First 1
$bitLocker=if($sv -and $sv.VolumeStatus -eq 'FullyEncrypted' -and $sv.ProtectionStatus -eq 'On'){'OK'}else{'WARN'}
$bitLockerDetails=if($sv){'Status: '+$sv.VolumeStatus+'; ochrona: '+$sv.ProtectionStatus}else{'Brak danych BitLocker.'}

$d=Get-CimInstance Win32_LogicalDisk -Filter ""DeviceID='$env:SystemDrive'"" -ErrorAction SilentlyContinue; $free=if($d){[math]::Round($d.FreeSpace/1GB,1)}else{0}
$systemDrive=if($d -and $free -ge 20){'OK'}elseif($d -and $free -ge 10){'WARN'}else{'ERROR'}
$systemDriveDetails=if($d){$env:SystemDrive+' · '+$free+' GB wolne z '+[math]::Round($d.Size/1GB,1)+' GB'}else{'Nie odczytano dysku systemowego.'}

$admins=@(Get-LocalGroupMember -Group 'Administrators' -ErrorAction SilentlyContinue); $names=@($admins|ForEach-Object{$_.Name})
$localAdmins=if($names.Count -gt 0){'OK'}else{'WARN'}; $localAdminsDetails=if($names.Count -gt 0){$names -join ', '}else{'Nie udało się odczytać grupy Administratorzy.'}

$required=@('WinDefend','wuauserv','BITS'); $states=@($required|ForEach-Object{$s=Get-Service $_ -ErrorAction SilentlyContinue;if($s){$_+': '+$s.Status}else{$_+': brak'}})
$bad=@($required|Where-Object{$s=Get-Service $_ -ErrorAction SilentlyContinue;-not $s -or $s.Status -ne 'Running'}); $services=if($bad.Count -eq 0){'OK'}else{'WARN'}; $servicesDetails=$states -join '; '

$wg=$null; try{$wg=& winget.exe --version 2>$null}catch{}; $winGet=if($wg){'OK'}else{'WARN'}; $winGetDetails=if($wg){'Wersja: '+($wg|Select-Object -First 1)}else{'WinGet nie jest dostępny.'}

$paths=@('HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending','HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired'); $pending=@($paths|Where-Object{Test-Path $_})
$pendingRestart=if($pending.Count -eq 0){'OK'}else{'WARN'}; $pendingRestartDetails=if($pending.Count -eq 0){'Nie wykryto oczekującego restartu.'}else{'Windows sygnalizuje oczekujący restart.'}

[pscustomobject]@{WindowsUpdate=$windowsUpdate;WindowsUpdateDetails=$windowsUpdateDetails;Activation=$activation;ActivationDetails=$activationDetails;Tpm=$tpm;TpmDetails=$tpmDetails;SecureBoot=$secureBoot;SecureBootDetails=$secureBootDetails;BitLocker=$bitLocker;BitLockerDetails=$bitLockerDetails;SystemDrive=$systemDrive;SystemDriveDetails=$systemDriveDetails;LocalAdmins=$localAdmins;LocalAdminsDetails=$localAdminsDetails;Services=$services;ServicesDetails=$servicesDetails;WinGet=$winGet;WinGetDetails=$winGetDetails;PendingRestart=$pendingRestart;PendingRestartDetails=$pendingRestartDetails}|ConvertTo-Json -Compress
";
}