using System.Text.Json;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class AuditHistoryService
{
    private readonly string _filePath;

    public AuditHistoryService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EDU-FIX", "EDUFIXiarz");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "last-audit.json");
    }

    public void Save(StationAudit audit)
    {
        try
        {
            var json = JsonSerializer.Serialize(audit, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Historia audytów jest dodatkiem i nie może przerwać audytu.
        }
    }

    public StationAudit? Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return null;
            return JsonSerializer.Deserialize<StationAudit>(File.ReadAllText(_filePath));
        }
        catch
        {
            return null;
        }
    }
}
