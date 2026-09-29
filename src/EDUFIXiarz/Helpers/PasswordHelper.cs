using System.Security;

namespace EDUFIXiarz.Helpers;

public static class PasswordHelper
{
    public static string ToPlainText(SecureString secure)
    {
        var ptr = System.Runtime.InteropServices.Marshal.SecureStringToBSTR(secure);
        try
        {
            return System.Runtime.InteropServices.Marshal.PtrToStringBSTR(ptr) ?? string.Empty;
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.ZeroFreeBSTR(ptr);
        }
    }
}
