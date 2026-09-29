using Microsoft.AspNetCore.DataProtection;

namespace BarbershopReservationsUni.Web.Services;

/// <summary>Подписана и криптирана сесия за профила на бръснар.</summary>
public class BarberSession
{
    private const string CookieName = "Barbershop.Barber";
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(8);
    private readonly IDataProtector protector;
    private readonly TimeProvider time;

    public BarberSession(IDataProtectionProvider provider, TimeProvider time)
    {
        protector = provider.CreateProtector("Barbershop.BarberSession.v1");
        this.time = time;
    }

    public void SignIn(HttpContext http, int barberId)
    {
        var expires = time.GetUtcNow().Add(Lifetime);
        var payload = $"{barberId}|{expires.ToUnixTimeSeconds()}";
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

    public int? GetBarberId(HttpContext http)
    {
        if (!http.Request.Cookies.TryGetValue(CookieName, out var cookie) || string.IsNullOrEmpty(cookie)) return null;
        try
        {
            var parts = protector.Unprotect(cookie).Split('|');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var barberId) ||
                !long.TryParse(parts[1], out var expiresUnix) || time.GetUtcNow().ToUnixTimeSeconds() > expiresUnix)
                return null;
            return barberId;
        }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
}
