using System.Diagnostics;

namespace EDUFIXiarz.Services;

public sealed class ProcessService
{
    public async Task<string> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        Action<string>? output = null,
        Action<string>? error = null)
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

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? $"{fileName} zakończył działanie kodem {process.ExitCode}."
                    : stderr.Trim());

        return stdout.Trim();
    }

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

        await process.StandardInput.WriteLineAsync(input);
        process.StandardInput.Close();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

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
