namespace EDUFIXiarz.Models;

public sealed class StationReport
{
    public const int CurrentFormatVersion = 2;

    public string ReportId { get; init; } = Guid.NewGuid().ToString("N");
    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public string ApplicationVersion { get; init; } = "1.3.0";
    public DateTime GeneratedAt { get; init; } = DateTime.Now;
    public HardwareReport Hardware { get; init; } = new();
    public StationPreparation? Preparation { get; init; }
    public StationAudit? Audit { get; init; }
}
