using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Services;

/// <summary>Причина, поради която резервацията не е създадена.</summary>
public enum BookingError
{
    None = 0,
    ServiceNotFound,
    BarberNotFound,
    InvalidTime,       // извън работно време / не е на границата на слот / неработен ден
    InPast,
    SlotTaken,
    InvalidPhone
}

public sealed record BookingResult(Appointment? Appointment, BookingError Error)
{
    public bool Succeeded => Appointment is not null && Error == BookingError.None;
    public static BookingResult Ok(Appointment appointment) => new(appointment, BookingError.None);
    public static BookingResult Fail(BookingError error) => new(null, error);
}

public enum CancelResult
{
    Cancelled,
    NotFound,
    AlreadyCancelled,
    InPast
}

public enum OtpRequestResult
{
    Sent,
    UnknownPhone,
    TooManyRequests
}

public enum OtpVerifyResult
{
    Success,
    InvalidCode,
    Expired,
    TooManyAttempts
}

public interface IBookingService
{
    Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Barber>> GetActiveBarbersAsync(CancellationToken cancellationToken = default);

    /// <summary>Свободните начални часове ("HH:mm") за бръснар, услуга и ден.</summary>
    Task<IReadOnlyList<string>> GetAvailableTimesAsync(
        int barberId, int serviceId, DateTime date, CancellationToken cancellationToken = default);

    Task<BookingResult> CreateAppointmentAsync(
        int serviceId,
        int barberId,
        DateTime appointmentDate,
        string clientName,
        string clientPhone,
        string? clientEmail,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<Appointment?> GetAppointmentAsync(int id, CancellationToken cancellationToken = default);

    Task<Client?> GetClientByIdAsync(int clientId, CancellationToken cancellationToken = default);

    Task<Client?> GetClientByPhoneAsync(string phone, CancellationToken cancellationToken = default);

    Task<Barber?> GetBarberByIdAsync(int barberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetBarberAppointmentsAsync(int barberId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Appointment>> GetClientAppointmentsAsync(int clientId, CancellationToken cancellationToken = default);

    /// <summary>Отменя час, само ако принадлежи на подадения клиент.</summary>
    Task<CancelResult> CancelAppointmentAsync(int appointmentId, int clientId, CancellationToken cancellationToken = default);

    /// <summary>Генерира и записва (хеширан) код. Върнатият код е за изпращане по SMS.</summary>
    Task<(OtpRequestResult Result, string? Code)> RequestOtpAsync(string phone, CancellationToken cancellationToken = default);

    /// <summary>При успех връща идентификатора на клиента.</summary>
    Task<(OtpVerifyResult Result, int? ClientId)> VerifyOtpAsync(string phone, string code, CancellationToken cancellationToken = default);
}
