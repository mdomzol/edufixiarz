using System.Globalization;
using System.Net;
using System.Text;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class StationSnapshotExportService
{
    public string ToCsv(StationSnapshot snapshot)
    {
        var h = snapshot.Hardware;
        var b = new StringBuilder();
        b.AppendLine("SnapshotFormatVersion;ApplicationVersion;SnapshotId;SessionId;CapturedAt;Stage;StationId;Hostname;Sekcja;Pole;Wartość");
        void Add(string section, string field, string value) => b.AppendLine(string.Join(";",
            Csv(snapshot.FormatVersion.ToString(CultureInfo.InvariantCulture)), Csv(snapshot.ApplicationVersion),
            Csv(snapshot.SnapshotId), Csv(snapshot.SessionId), Csv(snapshot.CapturedAt.ToString("O", CultureInfo.InvariantCulture)),
            Csv(snapshot.Stage), Csv(snapshot.StationId), Csv(h.Hostname), Csv(section), Csv(field), Csv(value)));

        Add("IDENTYFIKACJA", "StationId", h.StationId); Add("IDENTYFIKACJA", "UUID urządzenia", h.DeviceUuid);
        Add("IDENTYFIKACJA", "Hostname", h.Hostname); Add("IDENTYFIKACJA", "Użytkownik", h.UserName);
        Add("IDENTYFIKACJA", "Domena / Workgroup", h.Domain); Add("META", "Producent", h.Manufacturer);
        Add("META", "Model", h.Model); Add("META", "Numer seryjny", h.SerialNumber);
        Add("SYSTEM", "System operacyjny", h.OperatingSystem); Add("SYSTEM", "Wersja", h.OsVersion);
        Add("SYSTEM", "Architektura", h.Architecture); Add("SYSTEM", "Czas pracy", h.Uptime);
        Add("SYSTEM", "Aktywacja Windows", h.Activation); Add("SYSTEM", "Windows Update", h.WindowsUpdate);
        Add("CPU", "Procesor", h.Cpu); Add("CPU", "Rdzenie", h.CpuCores.ToString(CultureInfo.InvariantCulture));
        Add("CPU", "Wątki", h.CpuThreads.ToString(CultureInfo.InvariantCulture)); Add("RAM", "Pamięć", h.Ram);
        Add("RAM", "Sloty", $"{h.RamUsedSlots}/{h.RamSlots}"); Add("PŁYTA", "Płyta główna", h.Motherboard);
        Add("PŁYTA", "BIOS", h.Bios); Add("BEZPIECZEŃSTWO", "TPM", h.Tpm);
        Add("BEZPIECZEŃSTWO", "Secure Boot", h.SecureBoot); Add("BEZPIECZEŃSTWO", "BitLocker", h.BitLocker);
        foreach (var v in h.Gpus) Add("GPU", "Karta graficzna", v);
        foreach (var v in h.PhysicalDisks) Add("DYSK_FIZYCZNY", "Dysk", v);
        foreach (var v in h.LogicalDisks) Add("DYSK_LOGICZNY", "Dysk", v);
        foreach (var v in h.NetworkAdapters) Add("SIEĆ", "Adapter", v);
        foreach (var v in h.Antivirus) Add("BEZPIECZEŃSTWO", "Antywirus", v);
        foreach (var v in h.InstalledApplications) Add("OPROGRAMOWANIE", "Aplikacja", v);
        return b.ToString();
    }

    public string ToHtml(StationSnapshot snapshot)
    {
        var h = snapshot.Hardware;
        string Rows(IEnumerable<(string Label, string Value)> rows) => string.Join("", rows.Select(x => $"<tr><td>{E(x.Label)}</td><td>{E(x.Value)}</td></tr>"));
        string List(IEnumerable<string> values) => E(string.Join(" · ", values));
        return $@"<!doctype html><html lang=""pl""><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>EDUFIXiarz — odczyt {E(h.Hostname)}</title><style>
body{{font-family:Segoe UI,Arial,sans-serif;background:#f4f5f6;color:#17191c;margin:0;padding:32px}}main{{max-width:1100px;margin:auto;background:#fff;padding:40px;border:1px solid #ddd}}
h1{{margin:0;font-size:30px}}h2{{margin-top:30px;border-bottom:2px solid #f36b21;padding-bottom:8px}}.meta{{color:#666;margin:6px 0 24px}}
table{{width:100%;border-collapse:collapse}}th,td{{text-align:left;padding:9px 10px;border-bottom:1px solid #ddd;vertical-align:top}}th{{background:#f7f7f7}}
footer{{margin-top:35px;color:#777;font-size:12px}}@media(max-width:700px){{body{{padding:12px}}main{{padding:20px}}}}
</style></head><body><main><h1>ODCZYT STACJI</h1>
<div class=""meta"">EDUFIXiarz · {E(snapshot.Stage)} · format {snapshot.FormatVersion} · sesja {E(snapshot.SessionId)} · {snapshot.CapturedAt:yyyy-MM-dd HH:mm:ss}</div>
<h2>Identyfikacja</h2><table><tr><th>Parametr</th><th>Wartość</th></tr>{Rows(new[] {
("StationId",h.StationId),("UUID urządzenia",h.DeviceUuid),("Hostname",h.Hostname),("Użytkownik",h.UserName),
("Domena / Workgroup",h.Domain),("Producent",h.Manufacturer),("Model",h.Model),("Numer seryjny",h.SerialNumber)})}</table>
<h2>System</h2><table><tr><th>Parametr</th><th>Wartość</th></tr>{Rows(new[] {
("System operacyjny",h.OperatingSystem),("Wersja",h.OsVersion),("Architektura",h.Architecture),("Czas pracy",h.Uptime),
("Aktywacja Windows",h.Activation),("Windows Update",h.WindowsUpdate)})}</table>
<h2>Sprzęt</h2><table><tr><th>Parametr</th><th>Wartość</th></tr>{Rows(new[] {
("CPU",h.Cpu),("Rdzenie / wątki",$"{h.CpuCores} / {h.CpuThreads}"),("RAM",$"{h.Ram} · sloty {h.RamUsedSlots}/{h.RamSlots}"),
("Płyta główna",h.Motherboard),("BIOS",h.Bios),("GPU",List(h.Gpus)),("Dyski fizyczne",List(h.PhysicalDisks)),("Dyski logiczne",List(h.LogicalDisks))})}</table>
<h2>Sieć i bezpieczeństwo</h2><table><tr><th>Parametr</th><th>Wartość</th></tr>{Rows(new[] {
("Adaptery sieciowe",List(h.NetworkAdapters)),("Antywirus",List(h.Antivirus)),("TPM",h.Tpm),("Secure Boot",h.SecureBoot),("BitLocker",h.BitLocker)})}</table>
<h2>Oprogramowanie</h2><table><tr><th>Aplikacje</th></tr>{string.Join("",h.InstalledApplications.Select(x=>$"<tr><td>{E(x)}</td></tr>"))}</table>
<footer>EDUFIXiarz · EDU-FIX IT · snapshot: {E(snapshot.SnapshotId)} · stacja: {E(snapshot.StationId)}</footer></main></body></html>";
    }
    private static string Csv(string value) => ((char)34) + (value ?? string.Empty).Replace(((char)34).ToString(), new string((char)34, 2)) + ((char)34);
    private static string E(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
