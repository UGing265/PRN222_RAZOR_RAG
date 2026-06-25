using System.Security.Cryptography;

namespace BLL.Services.Auth;

public static class PasswordGenerator
{
    private const string Charset = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    public static string Generate(int length)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be positive.");
        return RandomNumberGenerator.GetString(Charset, length);
    }
}
