using System.Security.Cryptography;
using System.Text;

namespace BarbershopReservationsUni.Services;

/// <summary>
/// Генерира криптографски сигурни кодове и ги хешира с HMAC-SHA256.
/// Кодът е вързан към телефона, така че хешът от един номер не важи за друг.
/// </summary>
public static class OtpHasher
{
    /// <summary>Шестцифрен код от криптографски генератор (не System.Random).</summary>
    public static string GenerateCode() =>
        RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

    public static string Hash(string secret, string phone, string code)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes($"{phone}:{code}");
        return Convert.ToHexString(HMACSHA256.HashData(key, data));
    }

    /// <summary>Сравнение с константно време – без изтичане на информация по време на изпълнение.</summary>
    public static bool Verify(string secret, string phone, string code, string expectedHash)
    {
        var actual = Encoding.UTF8.GetBytes(Hash(secret, phone, code));
        var expected = Encoding.UTF8.GetBytes(expectedHash);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
