namespace EDUFIXiarz.Models;

public sealed class SnapshotComparison
{
    public List<SnapshotChange> Changes { get; init; } = [];
    public int ChangedCount => Changes.Count;
}

public sealed class SnapshotChange
{
    public string Field { get; init; } = "";
    public string Before { get; init; } = "—";
    public string After { get; init; } = "—";
}
