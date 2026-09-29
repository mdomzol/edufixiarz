namespace EDUFIXiarz.Models;

public sealed class StationReport
{
    public const int CurrentFormatVersion = 3;

    public string ReportId { get; init; } = Guid.NewGuid().ToString("N");
    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public string ApplicationVersion { get; init; } = AppInfo.Version;
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public StationSnapshot? BeforeSnapshot { get; init; }
    public StationPreparation? Preparation { get; init; }
    public StationSnapshot? AfterSnapshot { get; init; }
    public StationAudit? Audit { get; init; }
}
