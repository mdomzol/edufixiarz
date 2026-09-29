using System.Globalization;
using System.Net;
using System.Text;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class FullStationReportExportService
{
    public string ToCsv(StationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("ReportFormatVersion;ApplicationVersion;ReportId;GeneratedAt;Hostname;Sekcja;Pole;Wartość;Status;Szczegóły");

        void Add(string section, string field, string value, string status = "", string details = "")
        {
            builder.AppendLine(string.Join(";",
                Csv(report.FormatVersion.ToString(CultureInfo.InvariantCulture)),
                Csv(report.ApplicationVersion),
                Csv(report.ReportId),
                Csv(report.GeneratedAt.ToString("O", CultureInfo.InvariantCulture)),
                Csv(report.Hardware.Hostname),
                Csv(section),
                Csv(field),
                Csv(value),
                Csv(status),
                Csv(details)));
        }

        var h = report.Hardware;
        if (report.HardwareBeforePreparation is not null)
        {
            var before = report.HardwareBeforePreparation;
            Add("PRZED_PRZYGOTOWANIEM", "Hostname", before.Hostname);
            Add("PRZED_PRZYGOTOWANIEM", "System operacyjny", before.OperatingSystem);
            Add("PRZED_PRZYGOTOWANIEM", "Wersja", before.OsVersion);
            Add("PRZED_PRZYGOTOWANIEM", "CPU", before.Cpu);
            Add("PRZED_PRZYGOTOWANIEM", "RAM", before.Ram);
            Add("PRZED_PRZYGOTOWANIEM", "TPM", before.Tpm);
            Add("PRZED_PRZYGOTOWANIEM", "Secure Boot", before.SecureBoot);
            Add("PRZED_PRZYGOTOWANIEM", "BitLocker", before.BitLocker);
        }

        Add("META", "Producent", h.Manufacturer);
        Add("META", "Model", h.Model);
        Add("META", "Numer seryjny", h.SerialNumber);
        Add("SYSTEM", "System operacyjny", h.OperatingSystem);
        Add("SYSTEM", "Wersja", h.OsVersion);
        Add("SYSTEM", "Architektura", h.Architecture);
        Add("SYSTEM", "Czas pracy", h.Uptime);
        Add("CPU", "Procesor", h.Cpu);
        Add("CPU", "Rdzenie", h.CpuCores.ToString(CultureInfo.InvariantCulture));
        Add("CPU", "Wątki", h.CpuThreads.ToString(CultureInfo.InvariantCulture));
        Add("RAM", "Pamięć", h.Ram);
        Add("RAM", "Sloty", $"{h.RamUsedSlots}/{h.RamSlots}");
        Add("PŁYTA", "Płyta główna", h.Motherboard);
        Add("PŁYTA", "BIOS", h.Bios);
        Add("BEZPIECZEŃSTWO", "TPM", h.Tpm);
        Add("BEZPIECZEŃSTWO", "Secure Boot", h.SecureBoot);
        Add("BEZPIECZEŃSTWO", "BitLocker", h.BitLocker);

        foreach (var value in h.Gpus)
            Add("GPU", "Karta graficzna", value);
        foreach (var value in h.PhysicalDisks)
            Add("DYSK_FIZYCZNY", "Dysk", value);
        foreach (var value in h.LogicalDisks)
            Add("DYSK_LOGICZNY", "Dysk", value);
        foreach (var value in h.NetworkAdapters)
            Add("SIEĆ", "Adapter", value);
        foreach (var value in h.Antivirus)
            Add("BEZPIECZEŃSTWO", "Antywirus", value);

        if (report.Preparation is null)
        {
            Add("PRZYGOTOWANIE", "Stan", "NIE WYKONANO");
        }
        else
        {
            Add("PRZYGOTOWANIE", "Status", report.Preparation.Completed ? "ZAKOŃCZONE" : "NIEZAKOŃCZONE");
            Add("PRZYGOTOWANIE", "Rozpoczęto", report.Preparation.StartedAt.ToString("O", CultureInfo.InvariantCulture));
            Add("PRZYGOTOWANIE", "Zakończono", report.Preparation.FinishedAt.ToString("O", CultureInfo.InvariantCulture));
            Add("PRZYGOTOWANIE", "Restart zalecany", report.Preparation.RestartRecommended ? "TAK" : "NIE");
            Add("PRZYGOTOWANIE", "Docelowy hostname", report.Preparation.TargetHostname);
            Add("PRZYGOTOWANIE", "Domena", report.Preparation.Domain);

            foreach (var operation in report.Preparation.RequestedOperations)
                Add("PRZYGOTOWANIE", "Zaplanowana operacja", operation);

            foreach (var application in report.Preparation.SelectedApplications)
                Add("PRZYGOTOWANIE", "Wybrana aplikacja", application);

            foreach (var step in report.Preparation.Steps)
                Add("PRZYGOTOWANIE", step.Name, step.Status, step.Status, step.Details);
        }

        if (report.Audit is null)
        {
            Add("AUDYT", "Stan", "NIE WYKONANO");
        }
        else
        {
            Add("AUDYT", "Data kontroli", report.Audit.CheckedAt.ToString("O", CultureInfo.InvariantCulture));
            Add("AUDYT", "OK", report.Audit.OkCount.ToString(CultureInfo.InvariantCulture));
            Add("AUDYT", "Ostrzeżenia", report.Audit.WarningCount.ToString(CultureInfo.InvariantCulture));
            Add("AUDYT", "Błędy", report.Audit.ErrorCount.ToString(CultureInfo.InvariantCulture));

            foreach (var item in report.Audit.Items)
                Add("AUDYT", item.Name, item.Value, item.Status, item.Details);
        }

        return builder.ToString();
    }

    public string ToHtml(StationReport report)
    {
        var h = report.Hardware;
        var auditRows = new StringBuilder();

        if (report.Audit is null)
        {
            auditRows.Append("<tr><td colspan=\"3\">Audyt nie został jeszcze wykonany.</td></tr>");
        }
        else
        {
            foreach (var item in report.Audit.Items)
            {
                var css = item.Status switch { "OK" => "ok", "ERROR" => "error", _ => "warn" };
                auditRows.Append("<tr><td>")
                    .Append(E(item.Name))
                    .Append("</td><td><span class=\"status ")
                    .Append(css)
                    .Append("\">")
                    .Append(E(item.Status))
                    .Append("</span></td><td>")
                    .Append(E(item.Details))
                    .Append("</td></tr>");
            }
        }

        var auditSummary = report.Audit is null
            ? "AUDYT NIE WYKONANY"
            : $"OK: {report.Audit.OkCount} · OSTRZEŻENIA: {report.Audit.WarningCount} · BŁĘDY: {report.Audit.ErrorCount}";

        return $@"<!doctype html>
<html lang=""pl""><head><meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>EDUFIXiarz — raport stacji {E(h.Hostname)}</title>
<style>
body{{font-family:Segoe UI,Arial,sans-serif;background:#f4f5f6;color:#17191c;margin:0;padding:32px}}
main{{max-width:1100px;margin:auto;background:#fff;padding:40px;border:1px solid #ddd}}
h1{{margin:0;font-size:30px}}h2{{margin-top:32px;border-bottom:2px solid #f36b21;padding-bottom:8px}}
.meta{{color:#666;margin:6px 0 24px}}table{{width:100%;border-collapse:collapse}}
th,td{{text-align:left;padding:9px 10px;border-bottom:1px solid #ddd;vertical-align:top}}
th{{background:#f7f7f7}}.grid{{display:grid;grid-template-columns:1fr 1fr;gap:12px}}
.card{{border:1px solid #ddd;padding:16px;border-radius:7px}}.label{{font-size:11px;color:#777;text-transform:uppercase}}
.value{{font-size:16px;font-weight:600;margin-top:5px}}.summary{{padding:14px;background:#f7f7f7;border-left:4px solid #f36b21}}
.status{{font-weight:700;padding:3px 7px;border-radius:4px}}.ok{{background:#dff3e4;color:#176b2c}}
.warn{{background:#fff0d6;color:#8a5600}}.error{{background:#fde1e1;color:#a02020}}
.list{{margin:0;padding-left:20px}}footer{{margin-top:35px;color:#777;font-size:12px}}
@media(max-width:700px){{body{{padding:12px}}main{{padding:20px}}.grid{{grid-template-columns:1fr}}}}
</style></head><body><main>
<h1>RAPORT STACJI</h1>
<div class=""meta"">EDUFIXiarz · EDU-FIX IT · format {report.FormatVersion} · wersja {E(report.ApplicationVersion)} · wygenerowano {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</div>

<h2>Identyfikacja</h2>
<div class=""grid"">
<div class=""card""><div class=""label"">Hostname</div><div class=""value"">{E(h.Hostname)}</div></div>
<div class=""card""><div class=""label"">Numer seryjny</div><div class=""value"">{E(h.SerialNumber)}</div></div>
<div class=""card""><div class=""label"">Producent</div><div class=""value"">{E(h.Manufacturer)}</div></div>
<div class=""card""><div class=""label"">Model</div><div class=""value"">{E(h.Model)}</div></div>
</div>

{HardwareBeforeHtml(report.HardwareBeforePreparation)}

<h2>System</h2>
<table><tr><th>Parametr</th><th>Wartość</th></tr>
<tr><td>System operacyjny</td><td>{E(h.OperatingSystem)}</td></tr>
<tr><td>Wersja</td><td>{E(h.OsVersion)}</td></tr>
<tr><td>Architektura</td><td>{E(h.Architecture)}</td></tr>
<tr><td>Czas pracy</td><td>{E(h.Uptime)}</td></tr>
</table>

<h2>Podzespoły</h2>
<table><tr><th>Parametr</th><th>Wartość</th></tr>
<tr><td>Procesor</td><td>{E(h.Cpu)}</td></tr>
<tr><td>Rdzenie / wątki</td><td>{h.CpuCores} / {h.CpuThreads}</td></tr>
<tr><td>Pamięć RAM</td><td>{E(h.Ram)} · sloty: {h.RamUsedSlots}/{h.RamSlots}</td></tr>
<tr><td>Płyta główna</td><td>{E(h.Motherboard)}</td></tr>
<tr><td>BIOS</td><td>{E(h.Bios)}</td></tr>
</table>

<h2>Grafika i magazyn</h2>
<table><tr><th>Kategoria</th><th>Wartość</th></tr>
<tr><td>GPU</td><td>{E(string.Join(" · ", h.Gpus))}</td></tr>
<tr><td>Dyski fizyczne</td><td>{E(string.Join(" · ", h.PhysicalDisks))}</td></tr>
<tr><td>Dyski logiczne</td><td>{E(string.Join(" · ", h.LogicalDisks))}</td></tr>
</table>

<h2>Sieć i zabezpieczenia</h2>
<table><tr><th>Kategoria</th><th>Wartość</th></tr>
<tr><td>Adaptery sieciowe</td><td>{E(string.Join(" · ", h.NetworkAdapters))}</td></tr>
<tr><td>Antywirus</td><td>{E(string.Join(" · ", h.Antivirus))}</td></tr>
<tr><td>TPM</td><td>{E(h.Tpm)}</td></tr>
<tr><td>Secure Boot</td><td>{E(h.SecureBoot)}</td></tr>
<tr><td>BitLocker</td><td>{E(h.BitLocker)}</td></tr>
</table>

<h2>Przygotowanie stacji</h2>
{PreparationHtml(report.Preparation)}

<h2>Audyt</h2>
<div class=""summary"">{E(auditSummary)}</div>
<table><tr><th>Kontrola</th><th>Status</th><th>Szczegóły</th></tr>{auditRows}</table>

<footer>EDUFIXiarz · EDU-FIX IT · identyfikator raportu: {E(report.ReportId)}</footer>
</main></body></html>";
    }

    private static string HardwareBeforeHtml(HardwareReport? before)
    {
        if (before is null)
            return "";

        return $@"<h2>Stan przed przygotowaniem</h2>
<table><tr><th>Parametr</th><th>Wartość</th></tr>
<tr><td>Hostname</td><td>{E(before.Hostname)}</td></tr>
<tr><td>System operacyjny</td><td>{E(before.OperatingSystem)}</td></tr>
<tr><td>Wersja</td><td>{E(before.OsVersion)}</td></tr>
<tr><td>Procesor</td><td>{E(before.Cpu)}</td></tr>
<tr><td>Pamięć RAM</td><td>{E(before.Ram)}</td></tr>
<tr><td>TPM</td><td>{E(before.Tpm)}</td></tr>
<tr><td>Secure Boot</td><td>{E(before.SecureBoot)}</td></tr>
<tr><td>BitLocker</td><td>{E(before.BitLocker)}</td></tr>
</table>";
    }

    private static string PreparationHtml(StationPreparation? preparation)
    {
        if (preparation is null)
            return "<p>Przygotowanie stacji nie zostało wykonane.</p>";

        var rows = new StringBuilder();
        foreach (var step in preparation.Steps)
        {
            var css = step.Status == "OK" ? "ok" : step.Status == "ERROR" ? "error" : "warn";
            rows.Append("<tr><td>")
                .Append(E(step.Name))
                .Append("</td><td><span class=\"status ")
                .Append(css)
                .Append("\">")
                .Append(E(step.Status))
                .Append("</span></td><td>")
                .Append(E(step.Details))
                .Append("</td></tr>");
        }

        var apps = preparation.SelectedApplications.Count == 0
            ? "Brak"
            : string.Join(" · ", preparation.SelectedApplications.Select(E));

        return $@"<div class=""grid"">
<div class=""card""><div class=""label"">Status</div><div class=""value"">{E(preparation.Completed ? "ZAKOŃCZONE" : "NIEZAKOŃCZONE")}</div></div>
<div class=""card""><div class=""label"">Restart</div><div class=""value"">{E(preparation.RestartRecommended ? "ZALECANY" : "NIE WYMAGANY")}</div></div>
<div class=""card""><div class=""label"">Docelowy hostname</div><div class=""value"">{E(preparation.TargetHostname)}</div></div>
<div class=""card""><div class=""label"">Domena</div><div class=""value"">{E(preparation.Domain)}</div></div>
</div>
<table><tr><th>Etap</th><th>Status</th><th>Szczegóły</th></tr>{rows}</table>
<p><strong>Wybrane aplikacje:</strong> {apps}</p>";
    }

    private static string Csv(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    private static string E(string value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
