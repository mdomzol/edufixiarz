using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class AuditComparisonService
{
    private static readonly Dictionary<string, int> StatusRank = new(StringComparer.OrdinalIgnoreCase)
    {
        ["ERROR"] = 0,
        ["WARN"] = 1,
        ["OK"] = 2
    };

    public AuditComparison Compare(StationAudit previous, StationAudit current)
    {
        var previousByName = previous.Items.ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
        var changes = new List<AuditChange>();

        foreach (var currentItem in current.Items)
        {
            previousByName.TryGetValue(currentItem.Name, out var previousItem);
            if (previousItem is null)
            {
                changes.Add(new AuditChange
                {
                    Name = currentItem.Name,
                    Direction = "NOWE",
                    CurrentStatus = currentItem.Status,
                    CurrentDetails = currentItem.Details
                });
                continue;
            }

            var direction = GetDirection(previousItem.Status, currentItem.Status);
            changes.Add(new AuditChange
            {
                Name = currentItem.Name,
                Direction = direction,
                PreviousStatus = previousItem.Status,
                CurrentStatus = currentItem.Status,
                PreviousDetails = previousItem.Details,
                CurrentDetails = currentItem.Details
            });
        }

        foreach (var previousItem in previous.Items)
        {
            if (current.Items.Any(x => string.Equals(x.Name, previousItem.Name, StringComparison.OrdinalIgnoreCase)))
                continue;

            changes.Add(new AuditChange
            {
                Name = previousItem.Name,
                Direction = "USUNIĘTE",
                PreviousStatus = previousItem.Status,
                PreviousDetails = previousItem.Details
            });
        }

        return new AuditComparison { Changes = changes };
    }

    private static string GetDirection(string previous, string current)
    {
        if (string.Equals(previous, current, StringComparison.OrdinalIgnoreCase))
            return "BEZ ZMIAN";

        var oldRank = StatusRank.GetValueOrDefault(previous, 1);
        var newRank = StatusRank.GetValueOrDefault(current, 1);

        return newRank > oldRank ? "POPRAWA" : "POGORSZENIE";
    }
}
