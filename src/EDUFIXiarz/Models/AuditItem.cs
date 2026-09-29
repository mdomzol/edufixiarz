namespace EDUFIXiarz.Models;

public sealed class AuditItem
{
    public string Name { get; init; } = "";
    public string Status { get; init; } = "INFO";
    public string Value { get; init; } = "—";
    public string Details { get; init; } = "";
}
