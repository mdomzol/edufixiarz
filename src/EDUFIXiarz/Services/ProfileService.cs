using System.Text.Json;
using EDUFIXiarz.Models;

namespace EDUFIXiarz.Services;

public sealed class ProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string Serialize(SetupProfile profile) =>
        JsonSerializer.Serialize(profile, JsonOptions);

    public SetupProfile Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidOperationException("Plik profilu jest pusty.");

        return JsonSerializer.Deserialize<SetupProfile>(json, JsonOptions)
            ?? throw new InvalidOperationException("Plik profilu nie zawiera poprawnej konfiguracji.");
    }
}
