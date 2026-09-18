using BarbershopReservationsUni.Data;
using BarbershopReservationsUni.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BarbershopReservationsUni.Services;

public class BookingService : IBookingService
{
    private static readonly TimeSpan OpeningTime = new(9, 0, 0);
    private static readonly TimeSpan ClosingTime = new(19, 0, 0);
    private readonly BarbershopReservationsUniDbContext db;

    public BookingService(BarbershopReservationsUniDbContext db) => this.db = db;

    public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        await db.Services.AsNoTracking().OrderBy(s => s.Price).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Barber>> GetActiveBarbersAsync(CancellationToken cancellationToken = default) =>
        await db.Barbers.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.FullName).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<string>> GetAvailableTimesAsync(
        int barberId, int serviceId, DateTime date, CancellationToken cancellationToken = default)
    {
        var service = await db.Services.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null || date.Date < DateTime.Today) return [];

        var start = date.Date.Add(OpeningTime);
        var lastStart = date.Date.Add(ClosingTime).Subtract(TimeSpan.FromMinutes(service.DurationMinutes));
        var existing = await db.Appointments.AsNoTracking()
            .Where(a => a.BarberId == barberId &&
                        a.AppointmentDate >= date.Date.Add(OpeningTime) &&
                        a.AppointmentDate < date.Date.AddDays(1) &&
                        a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.AppointmentDate, Duration = a.Service!.DurationMinutes })
            .ToListAsync(cancellationToken);

        var now = DateTime.Now;
        var slots = new List<string>();

        for (var slot = start; slot <= lastStart; slot = slot.AddMinutes(30))
        {
            if (slot <= now && date.Date == now.Date) continue;

            var slotEnd = slot.AddMinutes(service.DurationMinutes);
            var overlaps = existing.Any(a =>
            {
                var existingEnd = a.AppointmentDate.AddMinutes(a.Duration);
                return slot < existingEnd && slotEnd > a.AppointmentDate;
            });

            if (!overlaps) slots.Add(slot.ToString("HH:mm"));
        }

        return slots;
    }

    public async Task<Appointment?> CreateAppointmentAsync(
        int serviceId, int barberId, DateTime appointmentDate, string clientName,
        string clientPhone, string? clientEmail, string? notes,
        CancellationToken cancellationToken = default)
    {
        var service = await db.Services.FindAsync([serviceId], cancellationToken);
        var barber = await db.Barbers.FirstOrDefaultAsync(b => b.Id == barberId && b.IsActive, cancellationToken);
        if (service is null || barber is null || appointmentDate < DateTime.Now) return null;

        var available = await GetAvailableTimesAsync(barberId, serviceId, appointmentDate.Date, cancellationToken);
        if (!available.Contains(appointmentDate.ToString("HH:mm"))) return null;

        var normalizedPhone = clientPhone.Trim();
        var client = await db.Clients.FirstOrDefaultAsync(c => c.PhoneNumber == normalizedPhone, cancellationToken);

        if (client is null)
        {
            client = new Client
            {
                FullName = clientName.Trim(),
                PhoneNumber = normalizedPhone,
                Email = string.IsNullOrWhiteSpace(clientEmail) ? null : clientEmail.Trim(),
                RegisteredOn = DateTime.Now
            };
            db.Clients.Add(client);
        }
        else
        {
            client.FullName = clientName.Trim();
            client.Email = string.IsNullOrWhiteSpace(clientEmail) ? client.Email : clientEmail.Trim();
        }

        var appointment = new Appointment
        {
            Client = client,
            BarberId = barberId,
            ServiceId = serviceId,
            AppointmentDate = appointmentDate,
            Status = AppointmentStatus.Confirmed,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedOn = DateTime.Now
        };

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync(cancellationToken);

        return await db.Appointments
            .Include(a => a.Client)
            .Include(a => a.Barber)
            .Include(a => a.Service)
            .FirstAsync(a => a.Id == appointment.Id, cancellationToken);
    }

    public Task<Appointment?> GetAppointmentAsync(int id, CancellationToken cancellationToken = default) =>
        db.Appointments.AsNoTracking()
            .Include(a => a.Client).Include(a => a.Barber).Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetClientAppointmentsAsync(string phone, CancellationToken cancellationToken = default) =>
        await db.Appointments.AsNoTracking()
            .Include(a => a.Barber).Include(a => a.Service)
            .Where(a => a.Client!.PhoneNumber == phone.Trim())
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

    public async Task<bool> CancelAppointmentAsync(int id, string phone, CancellationToken cancellationToken = default)
    {
        var appointment = await db.Appointments
            .Include(a => a.Client)
            .FirstOrDefaultAsync(a => a.Id == id && a.Client!.PhoneNumber == phone.Trim(), cancellationToken);

        if (appointment is null || appointment.AppointmentDate <= DateTime.Now ||
            appointment.Status == AppointmentStatus.Cancelled) return false;

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
