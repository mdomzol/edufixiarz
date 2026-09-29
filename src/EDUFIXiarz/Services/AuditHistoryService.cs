using System.IO;
using System.Text.Json;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class AuditHistoryService
{
    private readonly string _filePath;
    private readonly string _previousFilePath;

    public AuditHistoryService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "EDU-FIX", "EDUFIXiarz");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "last-audit.json");
        _previousFilePath = Path.Combine(directory, "previous-audit.json");
    }

    public StationAudit? LoadPrevious()
    {
        try
        {
            if (!File.Exists(_previousFilePath)) return null;
            return JsonSerializer.Deserialize<StationAudit>(File.ReadAllText(_previousFilePath));
        }
        catch
        {
            return null;
        }
    }

    public void Save(StationAudit audit)
    {
        try
        {
            if (File.Exists(_filePath))
                File.Copy(_filePath, _previousFilePath, overwrite: true);

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
