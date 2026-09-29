using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class SnapshotComparisonService
{
    public SnapshotComparison Compare(StationSnapshot? before, StationSnapshot? after)
    {
        var result = new SnapshotComparison();
        if (before is null || after is null)
            return result;

        var b = before.Hardware;
        var a = after.Hardware;

        Add(result, "Hostname", b.Hostname, a.Hostname);
        Add(result, "Użytkownik", b.UserName, a.UserName);
        Add(result, "Domena", b.Domain, a.Domain);
        Add(result, "System operacyjny", b.OperatingSystem, a.OperatingSystem);
        Add(result, "Wersja systemu", b.OsVersion, a.OsVersion);
        Add(result, "Aktywacja Windows", b.Activation, a.Activation);
        Add(result, "Windows Update", b.WindowsUpdate, a.WindowsUpdate);
        Add(result, "TPM", b.Tpm, a.Tpm);
        Add(result, "Secure Boot", b.SecureBoot, a.SecureBoot);
        Add(result, "BitLocker", b.BitLocker, a.BitLocker);
        Add(result, "Pamięć RAM", b.Ram, a.Ram);
        Add(result, "Aplikacje", string.Join(" · ", b.InstalledApplications), string.Join(" · ", a.InstalledApplications));
        Add(result, "Dyski logiczne", string.Join(" · ", b.LogicalDisks), string.Join(" · ", a.LogicalDisks));
        Add(result, "Adaptery sieciowe", string.Join(" · ", b.NetworkAdapters), string.Join(" · ", a.NetworkAdapters));

        return result;
    }

    private static void Add(SnapshotComparison result, string field, string before, string after)
    {
        if (!string.Equals(before ?? "", after ?? "", StringComparison.OrdinalIgnoreCase))
            result.Changes.Add(new SnapshotChange { Field = field, Before = before ?? "—", After = after ?? "—" });
    }
}
