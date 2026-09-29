using System.Diagnostics;

namespace EDUFIXiarz.Services;

public sealed class ProcessService
{
    public Task<string> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        Action<string>? output = null,
        Action<string>? error = null) =>
        RunCoreAsync(fileName, arguments, output, error, new HashSet<int>());

    public Task<string> RunAllowingExitCodesAsync(
        string fileName,
        IEnumerable<string> arguments,
        IReadOnlySet<int> allowedExitCodes,
        Action<string>? output = null,
        Action<string>? error = null) =>
        RunCoreAsync(fileName, arguments, output, error, allowedExitCodes);

    public async Task<string> RunWithInputAsync(
        string fileName,
        IEnumerable<string> arguments,
        string input,
        Action<string>? output = null,
        Action<string>? error = null)
    {
        var psi = CreateStartInfo(fileName, arguments);
        psi.RedirectStandardInput = true;

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Nie można uruchomić {fileName}.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.StandardInput.WriteLineAsync(input);
        process.StandardInput.Close();

        await process.WaitForExitAsync();

        var stdout = await outputTask;
        var stderr = await errorTask;

        if (!string.IsNullOrWhiteSpace(stdout))
            output?.Invoke(stdout.Trim());

        if (!string.IsNullOrWhiteSpace(stderr))
            error?.Invoke(stderr.Trim());

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? $"{fileName} zakończył działanie kodem {process.ExitCode}."
                    : stderr.Trim());

        return stdout.Trim();
    }

    private static async Task<string> RunCoreAsync(
        string fileName,
        IEnumerable<string> arguments,
        Action<string>? output,
        Action<string>? error,
        IReadOnlySet<int> allowedExitCodes)
    {
        var psi = CreateStartInfo(fileName, arguments);
        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Nie można uruchomić {fileName}.");

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = await outputTask;
        var stderr = await errorTask;

        if (!string.IsNullOrWhiteSpace(stdout))
            output?.Invoke(stdout.Trim());

        if (!string.IsNullOrWhiteSpace(stderr))
            error?.Invoke(stderr.Trim());

        if (process.ExitCode != 0 && !allowedExitCodes.Contains(process.ExitCode))
        {
            var detail = string.IsNullOrWhiteSpace(stderr) ? stdout.Trim() : stderr.Trim();
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(detail)
                    ? $"{fileName} zakończył działanie kodem {process.ExitCode}."
                    : $"{fileName}: {detail}");
        }

        return stdout.Trim();
    }

    private static ProcessStartInfo CreateStartInfo(
        string fileName,
        IEnumerable<string> arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
            psi.ArgumentList.Add(argument);

        return psi;
    }
}
