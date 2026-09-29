namespace EDUFIXiarz.Services;

public sealed class PowerShellService
{
    private readonly ProcessService _processService;

    public PowerShellService(ProcessService processService)
    {
        _processService = processService;
    }

    public Task<string> RunAsync(
        string command,
        Action<string>? output = null,
        Action<string>? error = null)
    {
        return _processService.RunAsync(
            "powershell.exe",
            ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", "$OutputEncoding = [System.Text.Encoding]::UTF8; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " + command],
            output,
            error);
    }

    public Task<string> RunWithInputAsync(
        string command,
        string input,
        Action<string>? output = null,
        Action<string>? error = null)
    {
        return _processService.RunWithInputAsync(
            "powershell.exe",
            ["-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-Command", "$OutputEncoding = [System.Text.Encoding]::UTF8; [Console]::OutputEncoding = [System.Text.Encoding]::UTF8; " + command],
            input,
            output,
            error);
    }
}
