using Microsoft.AspNetCore.DataProtection;

namespace BarbershopReservationsUni.Web.Services;

/// <summary>
/// Пази идентификатора на потвърдения клиент в криптирана и подписана бисквитка (Data Protection).
/// Потребителят не може да я подправи, за да види чужди резервации, за разлика от бисквитка с чист телефонен номер.
/// </summary>
public class ClientSession
{
    private const string CookieName = "Barbershop.Client";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);

    private readonly IDataProtector protector;
    private readonly TimeProvider time;

    public ClientSession(IDataProtectionProvider provider, TimeProvider time)
    {
        protector = provider.CreateProtector("Barbershop.ClientSession.v1");
        this.time = time;
    }

    public void SignIn(HttpContext http, int clientId)
    {
        var expires = time.GetUtcNow().Add(Lifetime);
        var payload = $"{clientId}|{expires.ToUnixTimeSeconds()}";

        http.Response.Cookies.Append(CookieName, protector.Protect(payload), new CookieOptions
        {
            HttpOnly = true,
            Secure = http.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Expires = expires
        });
    }

    public void SignOut(HttpContext http) => http.Response.Cookies.Delete(CookieName);

    /// <summary>Връща ClientId, ако има валидна и неизтекла сесия.</summary>
    public int? GetClientId(HttpContext http)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName, out var cookie) || string.IsNullOrEmpty(cookie))
            return null;

        try
        {
            var parts = protector.Unprotect(cookie).Split('|');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var clientId)
                || !long.TryParse(parts[1], out var expiresUnix)
                || time.GetUtcNow().ToUnixTimeSeconds() > expiresUnix)
            {
                return null;
            }

            return clientId;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Подправена или изтекла (ротиран ключ) бисквитка.
            return null;
        }
    }
}
