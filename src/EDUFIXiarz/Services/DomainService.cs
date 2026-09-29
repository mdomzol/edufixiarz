using System.Security;
using EDUFIXiarz.Helpers;

namespace EDUFIXiarz.Services;

public sealed class DomainService
{
    private readonly PowerShellService _powerShell;

    public DomainService(PowerShellService powerShell) => _powerShell = powerShell;

    public Task JoinAsync(string domain, string user, SecureString password, Action<string>? output = null, Action<string>? error = null)
    {
        if (string.IsNullOrWhiteSpace(domain)) throw new InvalidOperationException("Podaj nazwę domeny AD.");
        if (string.IsNullOrWhiteSpace(user)) throw new InvalidOperationException("Podaj konto używane do dołączenia stacji do domeny AD.");
        if (password.Length == 0) throw new InvalidOperationException("Podaj hasło do konta domenowego.");

        var command = "$secure = ConvertTo-SecureString ([Console]::In.ReadLine()) -AsPlainText -Force; " +
                      $"$cred = New-Object System.Management.Automation.PSCredential('{Escape(user)}',$secure); " +
                      $"Add-Computer -DomainName '{Escape(domain)}' -Credential $cred -Force -ErrorAction Stop";

        return _powerShell.RunWithInputAsync(command, PasswordHelper.ToPlainText(password), output, error);
    }

    private static string Escape(string value) => value.Replace("'", "''");
}
