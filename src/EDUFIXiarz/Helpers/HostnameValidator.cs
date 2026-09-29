namespace EDUFIXiarz.Helpers;

public static class HostnameValidator
{
    public static bool IsValid(string value) =>
        !string.IsNullOrWhiteSpace(value) &&
        value.Length <= 15 &&
        value.All(c => (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-') &&
        !value.StartsWith('-') &&
        !value.EndsWith('-');
}
