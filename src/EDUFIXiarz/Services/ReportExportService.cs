using System.Text;
using System.Text.Json;

namespace EDUFIXiarz.Services;

public sealed class ReportExportService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public string ToJson(HardwareReport report) =>
        JsonSerializer.Serialize(report, JsonOptions);

    public string ToCsv(HardwareReport report)
    {
        var rows = new (string Name, string Value)[]
        {
            ("Hostname", report.Hostname),
            ("Numer seryjny", report.SerialNumber),
            ("Producent", report.Manufacturer),
            ("Model", report.Model),
            ("Architektura", report.Architecture),
            ("System", report.OperatingSystem),
            ("Wersja systemu", report.OsVersion),
            ("Czas pracy", report.Uptime),
            ("Procesor", report.Cpu),
            ("Rdzenie", report.CpuCores.ToString()),
            ("Wątki", report.CpuThreads.ToString()),
            ("Pamięć RAM", report.Ram),
            ("Sloty RAM", report.RamUsedSlots + "/" + report.RamSlots),
            ("Płyta główna", report.Motherboard),
            ("BIOS", report.Bios),
            ("TPM", report.Tpm),
            ("Secure Boot", report.SecureBoot),
            ("BitLocker", report.BitLocker),
            ("Karty graficzne", string.Join(" | ", report.Gpus)),
            ("Dyski fizyczne", string.Join(" | ", report.PhysicalDisks)),
            ("Dyski logiczne", string.Join(" | ", report.LogicalDisks)),
            ("Sieć", string.Join(" | ", report.NetworkAdapters)),
            ("Antywirus", string.Join(" | ", report.Antivirus))
        };

        var builder = new StringBuilder();
        builder.AppendLine("Parametr;Wartość");

        foreach (var row in rows)
            builder.AppendLine(Escape(row.Name) + ";" + Escape(row.Value));

        return builder.ToString();
    }

    private static string Escape(string value) =>
        """ + value.Replace(""", """") + """;
}
