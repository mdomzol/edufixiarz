namespace EDUFIXiarz.Models;

public sealed class StationSnapshot
{
    public const int CurrentFormatVersion = 1;

    public string SnapshotId { get; init; } = Guid.NewGuid().ToString("N");
    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public string ApplicationVersion { get; init; } = "1.3.0";
    public DateTime CapturedAt { get; init; } = DateTime.Now;
    public string Stage { get; init; } = "ODCZYT";
    public HardwareReport Hardware { get; init; } = new();
}
