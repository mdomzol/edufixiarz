using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EDUFIXiarz.Models;

public sealed class PreparationProgressStep : INotifyPropertyChanged
{
    private string _status = "WAITING";
    private string _details = "Oczekuje";

    public int Number { get; init; }
    public string Name { get; init; } = "—";

    public string Status
    {
        get => _status;
        set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusLabel));
            OnPropertyChanged(nameof(StatusGlyph));
        }
    }

    public string Details
    {
        get => _details;
        set
        {
            if (_details == value) return;
            _details = value;
            OnPropertyChanged();
        }
    }

    public string StatusLabel => Status switch
    {
        "RUNNING" => "W TOKU",
        "DONE" => "GOTOWE",
        "ERROR" => "BŁĄD",
        "SKIPPED" => "POMINIĘTO",
        _ => "OCZEKUJE"
    };

    public string StatusGlyph => Status switch
    {
        "RUNNING" => "…",
        "DONE" => "✓",
        "ERROR" => "!",
        "SKIPPED" => "–",
        _ => Number.ToString()
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
