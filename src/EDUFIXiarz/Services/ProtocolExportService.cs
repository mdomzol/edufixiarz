using System.Net;
using System.Text;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class ProtocolExportService
{
    public string ToHtml(HardwareReport report, StationAudit audit)
    {
        var rows = new StringBuilder();
        foreach (var item in audit.Items)
        {
            var css = item.Status switch { "OK" => "ok", "ERROR" => "error", _ => "warn" };
            rows.Append("<tr><td>").Append(E(item.Name)).Append("</td><td><span class="status ")
                .Append(css).Append("">").Append(E(item.Status)).Append("</span></td><td>")
                .Append(E(item.Details)).Append("</td></tr>");
        }

        return $@"<!doctype html>
<html lang=""pl""><head><meta charset=""utf-8""><title>Protokół odbioru stacji — {E(report.Hostname)}</title>
<style>
body{{font-family:Segoe UI,Arial,sans-serif;background:#f4f5f6;color:#17191c;margin:0;padding:32px}}
main{{max-width:1000px;margin:auto;background:#fff;padding:36px;border:1px solid #ddd}}
h1{{margin:0 0 6px;font-size:28px}}h2{{margin-top:30px;border-bottom:2px solid #f36b21;padding-bottom:8px}}
.meta{{color:#666;margin-bottom:20px}}.summary{{display:flex;gap:12px;margin:18px 0}}.badge{{padding:8px 12px;border-radius:6px;background:#eee}}
table{{width:100%;border-collapse:collapse}}th,td{{text-align:left;padding:10px;border-bottom:1px solid #ddd;vertical-align:top}}th{{background:#f7f7f7}}
.status{{font-weight:700;padding:3px 7px;border-radius:4px}}.ok{{background:#dff3e4;color:#176b2c}}.warn{{background:#fff0d6;color:#8a5600}}.error{{background:#fde1e1;color:#a02020}}
.signature{{margin-top:55px;display:grid;grid-template-columns:1fr 1fr;gap:40px}}.line{{border-top:1px solid #555;padding-top:8px}}
footer{{margin-top:35px;color:#777;font-size:12px}}
</style></head><body><main>
<h1>PROTOKÓŁ ODBIORU STACJI</h1>
<div class=""meta"">EDUFIXiarz · EDU-FIX IT · {audit.CheckedAt:yyyy-MM-dd HH:mm:ss}</div>
<h2>Identyfikacja</h2>
<table><tr><th>Hostname</th><td>{E(report.Hostname)}</td></tr><tr><th>Producent</th><td>{E(report.Manufacturer)}</td></tr><tr><th>Model</th><td>{E(report.Model)}</td></tr><tr><th>Numer seryjny</th><td>{E(report.SerialNumber)}</td></tr><tr><th>System</th><td>{E(report.OperatingSystem)} · {E(report.OsVersion)}</td></tr></table>
<h2>Podsumowanie audytu</h2>
<div class=""summary""><span class=""badge"">OK: {audit.OkCount}</span><span class=""badge"">OSTRZEŻENIA: {audit.WarningCount}</span><span class=""badge"">BŁĘDY: {audit.ErrorCount}</span></div>
<table><tr><th>Kontrola</th><th>Status</th><th>Wynik / szczegóły</th></tr>{rows}</table>
<h2>Uwagi</h2><p>Protokół przedstawia stan stacji w momencie wykonania audytu. Wyniki zależne od usług sieciowych, polityk organizacji lub dostępności komponentów Windows mogą wymagać dodatkowej weryfikacji.</p>
<div class=""signature""><div class=""line"">Osoba wykonująca / EDU-FIX IT</div><div class=""line"">Przedstawiciel jednostki</div></div>
<footer>EDUFIXiarz · narzędzie administracyjne EDU-FIX IT</footer>
</main></body></html>";
    }

    private static string E(string value) => WebUtility.HtmlEncode(value);
}
