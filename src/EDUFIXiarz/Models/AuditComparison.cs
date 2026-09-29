namespace EDUFIXiarz.Models;

public sealed class AuditComparison
{
    public int ImprovedCount => Changes.Count(x => x.Direction == "POPRAWA");
    public int WorsenedCount => Changes.Count(x => x.Direction == "POGORSZENIE");
    public int UnchangedCount => Changes.Count(x => x.Direction == "BEZ ZMIAN");
    public List<AuditChange> Changes { get; init; } = [];
}

public sealed class AuditChange
{
    public string Name { get; init; } = "";
    public string Direction { get; init; } = "BEZ ZMIAN";
    public string PreviousStatus { get; init; } = "—";
    public string CurrentStatus { get; init; } = "—";
    public string PreviousDetails { get; init; } = "";
    public string CurrentDetails { get; init; } = "";
}
