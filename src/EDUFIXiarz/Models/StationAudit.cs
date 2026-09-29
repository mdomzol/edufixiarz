namespace EDUFIXiarz.Models;

public sealed class StationAudit
{
    public DateTime CheckedAt { get; init; } = DateTime.Now;
    public string Hostname { get; init; } = "—";
    public List<AuditItem> Items { get; init; } = [];
    public int OkCount => Items.Count(x => x.Status == "OK");
    public int WarningCount => Items.Count(x => x.Status == "WARN");
    public int ErrorCount => Items.Count(x => x.Status == "ERROR");
}
