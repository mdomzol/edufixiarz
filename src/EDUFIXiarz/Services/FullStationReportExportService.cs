using System.Globalization;
using System.Net;
using System.Text;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class FullStationReportExportService
{
    public string ToCsv(StationReport report)
    {
        var b = new StringBuilder();
        b.AppendLine("ReportFormatVersion;ApplicationVersion;ReportId;GeneratedAt;SessionId;Stage;StationId;Hostname;Sekcja;Pole;Wartość;Status;Szczegóły");
        void Add(string section,string field,string value,string status="",string details="")
        {
            var snapshot = report.AfterSnapshot ?? report.BeforeSnapshot;
            b.AppendLine(string.Join(";",Csv(report.FormatVersion.ToString(CultureInfo.InvariantCulture)),Csv(report.ApplicationVersion),Csv(report.ReportId),
                Csv(report.GeneratedAt.ToString("O",CultureInfo.InvariantCulture)),Csv(snapshot?.SessionId ?? "—"),Csv(snapshot?.Stage ?? "RAPORT"),
                Csv(snapshot?.StationId ?? "—"),Csv(snapshot?.Hardware.Hostname ?? "—"),Csv(section),Csv(field),Csv(value),Csv(status),Csv(details)));
        }
        AddSnapshot(Add, "PRZED", report.BeforeSnapshot); AddSnapshot(Add, "PO", report.AfterSnapshot);

        if (report.Preparation is null) Add("PRZYGOTOWANIE","Stan","NIE WYKONANO");
        else {
            Add("PRZYGOTOWANIE","Status",report.Preparation.Completed?"ZAKOŃCZONE":"NIEZAKOŃCZONE");
            Add("PRZYGOTOWANIE","Rozpoczęto",report.Preparation.StartedAt.ToString("O",CultureInfo.InvariantCulture));
            Add("PRZYGOTOWANIE","Zakończono",report.Preparation.FinishedAt.ToString("O",CultureInfo.InvariantCulture));
            Add("PRZYGOTOWANIE","Restart zalecany",report.Preparation.RestartRecommended?"TAK":"NIE");
            Add("PRZYGOTOWANIE","Docelowy hostname",report.Preparation.TargetHostname); Add("PRZYGOTOWANIE","Domena",report.Preparation.Domain);
            foreach(var x in report.Preparation.RequestedOperations) Add("PRZYGOTOWANIE","Zaplanowana operacja",x);
            foreach(var x in report.Preparation.SelectedApplications) Add("PRZYGOTOWANIE","Wybrana aplikacja",x);
            foreach(var x in report.Preparation.Steps) Add("PRZYGOTOWANIE",x.Name,x.Status,x.Status,x.Details);
        }
        if(report.Audit is null) Add("AUDYT","Stan","NIE WYKONANO");
        else {
            Add("AUDYT","Data kontroli",report.Audit.CheckedAt.ToString("O",CultureInfo.InvariantCulture));
            Add("AUDYT","OK",report.Audit.OkCount.ToString(CultureInfo.InvariantCulture));
            Add("AUDYT","Ostrzeżenia",report.Audit.WarningCount.ToString(CultureInfo.InvariantCulture));
            Add("AUDYT","Błędy",report.Audit.ErrorCount.ToString(CultureInfo.InvariantCulture));
            foreach(var x in report.Audit.Items) Add("AUDYT",x.Name,x.Value,x.Status,x.Details);
        }
        return b.ToString();
    }

    private static void AddSnapshot(Action<string,string,string,string,string> add,string prefix,StationSnapshot? snapshot)
    {
        if(snapshot is null){ add(prefix,"Stan","BRAK"); return; }
        var h=snapshot.Hardware;
        add(prefix,"StationId",h.StationId); add(prefix,"UUID urządzenia",h.DeviceUuid); add(prefix,"Hostname",h.Hostname);
        add(prefix,"Użytkownik",h.UserName); add(prefix,"Domena",h.Domain); add(prefix,"Producent",h.Manufacturer); add(prefix,"Model",h.Model);
        add(prefix,"Numer seryjny",h.SerialNumber); add(prefix,"System operacyjny",h.OperatingSystem); add(prefix,"Wersja",h.OsVersion);
        add(prefix,"Aktywacja Windows",h.Activation); add(prefix,"Windows Update",h.WindowsUpdate); add(prefix,"CPU",h.Cpu); add(prefix,"RAM",h.Ram);
        add(prefix,"TPM",h.Tpm); add(prefix,"Secure Boot",h.SecureBoot); add(prefix,"BitLocker",h.BitLocker);
        foreach(var x in h.InstalledApplications) add(prefix,"Aplikacja",x);
    }

    public string ToHtml(StationReport report)
    {
        var audit = report.Audit is null ? "<p>Audyt nie został wykonany.</p>" :
            "<table><tr><th>Kontrola</th><th>Status</th><th>Szczegóły</th></tr>"+string.Join("",report.Audit.Items.Select(x=>$"<tr><td>{E(x.Name)}</td><td>{E(x.Status)}</td><td>{E(x.Details)}</td></tr>"))+"</table>";
        return $@"<!doctype html><html lang=""pl""><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1"">
<title>EDUFIXiarz — raport stacji</title><style>body{{font-family:Segoe UI,Arial,sans-serif;background:#f4f5f6;color:#17191c;margin:0;padding:32px}}main{{max-width:1100px;margin:auto;background:#fff;padding:40px;border:1px solid #ddd}}h1{{margin:0}}h2{{margin-top:32px;border-bottom:2px solid #f36b21;padding-bottom:8px}}table{{width:100%;border-collapse:collapse}}th,td{{text-align:left;padding:9px;border-bottom:1px solid #ddd;vertical-align:top}}th{{background:#f7f7f7}}.meta{{color:#666;margin:8px 0 24px}}footer{{margin-top:35px;color:#777;font-size:12px}}</style></head><body><main>
<h1>RAPORT STACJI</h1><div class=""meta"">EDUFIXiarz · format {report.FormatVersion} · raport {E(report.ReportId)} · {report.GeneratedAt:yyyy-MM-dd HH:mm:ss}</div>
{SnapshotHtml("STAN PRZED PRZYGOTOWANIEM",report.BeforeSnapshot)}
<h2>PRZYGOTOWANIE</h2>{PreparationHtml(report.Preparation)}
{SnapshotHtml("STAN PO PRZYGOTOWANIU",report.AfterSnapshot)}
<h2>AUDYT</h2>{audit}
<footer>EDUFIXiarz · EDU-FIX IT</footer></main></body></html>";
    }

    private static string SnapshotHtml(string title,StationSnapshot? snapshot)
    {
        if(snapshot is null) return "";
        var h=snapshot.Hardware;
        var rows=new[]{("StationId",h.StationId),("UUID",h.DeviceUuid),("Hostname",h.Hostname),("Użytkownik",h.UserName),("Domena",h.Domain),
            ("Producent",h.Manufacturer),("Model",h.Model),("Numer seryjny",h.SerialNumber),("System",h.OperatingSystem),("Wersja",h.OsVersion),
            ("Aktywacja",h.Activation),("Windows Update",h.WindowsUpdate),("CPU",h.Cpu),("RAM",h.Ram),("TPM",h.Tpm),("Secure Boot",h.SecureBoot),("BitLocker",h.BitLocker)};
        return $"<h2>{E(title)}</h2><table><tr><th>Parametr</th><th>Wartość</th></tr>{string.Join("",rows.Select(x=>$"<tr><td>{E(x.Item1)}</td><td>{E(x.Item2)}</td></tr>"))}</table>";
    }

    private static string PreparationHtml(StationPreparation? p)
    {
        if(p is null) return "<p>Przygotowanie nie zostało wykonane.</p>";
        return $"<p><strong>{E(p.Completed?"ZAKOŃCZONE":"NIEZAKOŃCZONE")}</strong> · restart: {E(p.RestartRecommended?"ZALECANY":"NIE")}</p><table><tr><th>Etap</th><th>Status</th><th>Szczegóły</th></tr>{string.Join("",p.Steps.Select(x=>$"<tr><td>{E(x.Name)}</td><td>{E(x.Status)}</td><td>{E(x.Details)}</td></tr>"))}</table>";
    }
    private static string Csv(string value)=>""" + (value??string.Empty).Replace(""","""") + """;
    private static string E(string value)=>WebUtility.HtmlEncode(value??string.Empty);
}
