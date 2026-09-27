using System.Text;

namespace BarbershopReservationsUni.Services;

/// <summary>
/// Привежда български телефонни номера към единен вид, за да не се създават
/// дублирани клиенти при "0888 123 456", "0888-123-456" и "+359 888 123 456".
/// </summary>
public static class PhoneNormalizer
{
    /// <summary>Връща номера във вид 0XXXXXXXXX (национален формат) или чист вид за чужди номера.</summary>
    public static string Normalize(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;

        var sb = new StringBuilder(phone.Length);
        for (var i = 0; i < phone.Length; i++)
        {
            var ch = phone[i];
            if (char.IsDigit(ch)) sb.Append(ch);
            else if (ch == '+' && sb.Length == 0) sb.Append(ch);
        }

        var digits = sb.ToString();

        if (digits.StartsWith("+359")) digits = "0" + digits[4..];
        else if (digits.StartsWith("00359")) digits = "0" + digits[5..];
        else if (digits.StartsWith("359") && digits.Length == 12) digits = "0" + digits[3..];

        return digits;
    }

    /// <summary>Валиден е български мобилен/стационарен номер в национален формат (9–10 цифри, започва с 0).</summary>
    public static bool IsValid(string? phone)
    {
        var n = Normalize(phone);
        return n.Length is >= 9 and <= 10 && n[0] == '0' && n.All(char.IsDigit);
    }

    /// <summary>Скрива средата на номера за показване в интерфейса: 0888***456.</summary>
    public static string Mask(string? phone)
    {
        var n = Normalize(phone);
        return n.Length < 7 ? n : n[..4] + new string('*', n.Length - 7) + n[^3..];
    }
}
