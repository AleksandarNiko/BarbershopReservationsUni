namespace BarbershopReservationsUni.Services;

/// <summary>Настройки на салона и на защитата на кодовете. Свързва се със секция "Booking" в appsettings.</summary>
public class BookingOptions
{
    public const string SectionName = "Booking";

    /// <summary>Начало на работния ден.</summary>
    public TimeSpan OpeningTime { get; set; } = new(9, 0, 0);

    /// <summary>Край на работния ден – най-късният край на услуга.</summary>
    public TimeSpan ClosingTime { get; set; } = new(19, 0, 0);

    /// <summary>Стъпка между началните часове в минути.</summary>
    public int SlotMinutes { get; set; } = 30;

    /// <summary>Неработни дни от седмицата (по подразбиране – неделя).</summary>
    public DayOfWeek[] ClosedDays { get; set; } = [DayOfWeek.Sunday];

    /// <summary>Колко дни напред е позволено да се резервира.</summary>
    public int MaxDaysAhead { get; set; } = 60;

    /// <summary>Тайна за HMAC на еднократните кодове. Трябва да е зададена извън репото (user-secrets / env).</summary>
    public string OtpSecret { get; set; } = string.Empty;

    public int OtpValidMinutes { get; set; } = 5;
    public int OtpMaxFailedAttempts { get; set; } = 5;

    /// <summary>Най-много заявки за код към един номер в рамките на <see cref="OtpRequestWindowMinutes"/>.</summary>
    public int OtpMaxRequestsPerWindow { get; set; } = 3;
    public int OtpRequestWindowMinutes { get; set; } = 10;
}
