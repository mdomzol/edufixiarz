namespace EDUFIXiarz.Models;

public sealed class StationPreparation
{
    public DateTime StartedAt { get; init; } = DateTime.Now;
    public DateTime FinishedAt { get; init; } = DateTime.Now;
    public bool Completed { get; init; }
    public bool RestartRecommended { get; init; }
    public string TargetHostname { get; init; } = "";
    public string Domain { get; init; } = "";
    public List<string> RequestedOperations { get; init; } = [];
    public List<string> SelectedApplications { get; init; } = [];
    public List<PreparationStep> Steps { get; init; } = [];
}

public sealed class PreparationStep
{
    public string Name { get; init; } = "";
    public string Status { get; init; } = "INFO";
    public string Details { get; init; } = "";
    public DateTime CompletedAt { get; init; } = DateTime.Now;
}
