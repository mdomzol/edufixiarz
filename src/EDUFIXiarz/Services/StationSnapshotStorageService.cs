using System.Text;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class StationSnapshotStorageService
{
    public string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "EDU-FIX", "EDUFIXiarz", "Snapshots");

    public (string CsvPath, string HtmlPath) Save(StationSnapshot snapshot, StationSnapshotExportService exporter)
    {
        var stationDirectory = Path.Combine(RootDirectory, Sanitize(snapshot.StationId));
        Directory.CreateDirectory(stationDirectory);

        var stem = $"EDUFIXiarz-Odczyt-{Sanitize(snapshot.Hardware.Hostname)}-{Sanitize(snapshot.Stage)}-{snapshot.CapturedAt:yyyyMMdd-HHmmss}";
        var csvPath = Path.Combine(stationDirectory, stem + ".csv");
        var htmlPath = Path.Combine(stationDirectory, stem + ".html");

        File.WriteAllText(csvPath, exporter.ToCsv(snapshot), new UTF8Encoding(true));
        File.WriteAllText(htmlPath, exporter.ToHtml(snapshot), new UTF8Encoding(false));
        return (csvPath, htmlPath);
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new string((value ?? "unknown").Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(result) ? "unknown" : result;
    }
}
