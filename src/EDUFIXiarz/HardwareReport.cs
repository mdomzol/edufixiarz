namespace EDUFIXiarz;

public sealed class HardwareReport
{
    public string Hostname { get; set; } = "—";
    public string StationId { get; set; } = "—";
    public string UserName { get; set; } = "—";
    public string Domain { get; set; } = "—";
    public string SerialNumber { get; set; } = "—";
    public string Manufacturer { get; set; } = "—";
    public string Model { get; set; } = "—";
    public string Architecture { get; set; } = "—";
    public string OperatingSystem { get; set; } = "—";
    public string OsVersion { get; set; } = "—";
    public string Uptime { get; set; } = "—";
    public string Activation { get; set; } = "—";
    public string WindowsUpdate { get; set; } = "—";
    public string Cpu { get; set; } = "—";
    public int CpuCores { get; set; }
    public int CpuThreads { get; set; }
    public string Ram { get; set; } = "—";
    public int RamSlots { get; set; }
    public int RamUsedSlots { get; set; }
    public string Motherboard { get; set; } = "—";
    public string Bios { get; set; } = "—";
    public string Tpm { get; set; } = "—";
    public string SecureBoot { get; set; } = "—";
    public string BitLocker { get; set; } = "—";
    public List<string> Gpus { get; set; } = [];
    public List<string> PhysicalDisks { get; set; } = [];
    public List<string> LogicalDisks { get; set; } = [];
    public List<string> NetworkAdapters { get; set; } = [];
    public List<string> Antivirus { get; set; } = [];
    public List<string> InstalledApplications { get; set; } = [];
}
