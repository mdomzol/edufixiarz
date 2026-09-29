namespace EDUFIXiarz.Helpers;

public static class HostnameValidator
{
    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 15 &&
        value.All(c => char.IsLetterOrDigit(c) || c == '-') &&
        !value.StartsWith('-') &&
        !value.EndsWith('-');
}
