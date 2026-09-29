namespace EDUFIXiarz.Models;

public sealed class StationSnapshot
{
    public const int CurrentFormatVersion = 2;

    public static class Stages
    {
        public const string Read = "ODCZYT";
        public const string BeforePreparation = "PRZED-PRZYGOTOWANIEM";
        public const string AfterPreparation = "PO-PRZYGOTOWANIU";
    }

    public string SnapshotId { get; init; } = Guid.NewGuid().ToString("N");
    public string SessionId { get; init; } = Guid.NewGuid().ToString("N");
    public int FormatVersion { get; init; } = CurrentFormatVersion;
    public string ApplicationVersion { get; init; } = AppInfo.Version;
    public DateTime CapturedAt { get; init; } = DateTime.Now;
    public string Stage { get; init; } = Stages.Read;
    public string StationId { get; init; } = "—";
    public HardwareReport Hardware { get; init; } = new();
}
