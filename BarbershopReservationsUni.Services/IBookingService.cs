using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Services;

public interface IBookingService
{
    Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Barber>> GetActiveBarbersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAvailableTimesAsync(int barberId, int serviceId, DateTime date, CancellationToken cancellationToken = default);
    Task<Appointment?> CreateAppointmentAsync(
        int serviceId,
        int barberId,
        DateTime appointmentDate,
        string clientName,
        string clientPhone,
        string? clientEmail,
        string? notes,
        CancellationToken cancellationToken = default);
    Task<Appointment?> GetAppointmentAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetClientAppointmentsAsync(string phone, CancellationToken cancellationToken = default);
    Task<bool> CancelAppointmentAsync(int id, string phone, CancellationToken cancellationToken = default);
}
