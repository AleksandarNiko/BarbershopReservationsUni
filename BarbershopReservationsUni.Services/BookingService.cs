using System.Data;
using BarbershopReservationsUni.Data;
using BarbershopReservationsUni.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BarbershopReservationsUni.Services;

public class BookingService : IBookingService
{
    private readonly BarbershopReservationsUniDbContext db;
    private readonly BookingOptions options;
    private readonly TimeProvider time;

    public BookingService(
        BarbershopReservationsUniDbContext db,
        IOptions<BookingOptions> options,
        TimeProvider? timeProvider = null)
    {
        this.db = db;
        this.options = options.Value;
        this.time = timeProvider ?? TimeProvider.System;
    }

    /// <summary>Текущо локално време на салона (без зависимост от часовата зона на сървъра).</summary>
    private DateTime Now => time.GetLocalNow().DateTime;

    // ------------------------------------------------------------------ каталог

    public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default) =>
        await db.Services.AsNoTracking().OrderBy(s => s.Id).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Barber>> GetActiveBarbersAsync(CancellationToken cancellationToken = default) =>
        await db.Barbers.AsNoTracking().Where(b => b.IsActive).OrderBy(b => b.Id).ToListAsync(cancellationToken);

    // ------------------------------------------------------------------ свободни часове

    public async Task<IReadOnlyList<string>> GetAvailableTimesAsync(
        int barberId, int serviceId, DateTime date, CancellationToken cancellationToken = default)
    {
        var day = date.Date;
        if (!IsBookableDay(day)) return [];

        var service = await db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null) return [];

        var barberActive = await db.Barbers.AsNoTracking()
            .AnyAsync(b => b.Id == barberId && b.IsActive, cancellationToken);
        if (!barberActive) return [];

        var busy = await db.Appointments.AsNoTracking()
            .Where(a => a.BarberId == barberId
                        && a.Status != AppointmentStatus.Cancelled
                        && a.AppointmentDate < day.AddDays(1)
                        && a.EndDate > day)
            .Select(a => new { a.AppointmentDate, a.EndDate })
            .ToListAsync(cancellationToken);

        var now = Now;
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        var closing = day.Add(options.ClosingTime);
        var slots = new List<string>();

        for (var start = day.Add(options.OpeningTime);
             start + duration <= closing;
             start = start.AddMinutes(options.SlotMinutes))
        {
            if (start <= now) continue;

            var end = start + duration;
            var overlaps = busy.Any(b => b.AppointmentDate < end && b.EndDate > start);
            if (!overlaps) slots.Add(start.ToString("HH:mm"));
        }

        return slots;
    }

    // ------------------------------------------------------------------ създаване

    public async Task<BookingResult> CreateAppointmentAsync(
        int serviceId, int barberId, DateTime appointmentDate, string clientName,
        string clientPhone, string? clientEmail, string? notes,
        CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizer.Normalize(clientPhone);
        if (!PhoneNormalizer.IsValid(phone)) return BookingResult.Fail(BookingError.InvalidPhone);

        var service = await db.Services.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken);
        if (service is null) return BookingResult.Fail(BookingError.ServiceNotFound);

        var barberExists = await db.Barbers.AsNoTracking()
            .AnyAsync(b => b.Id == barberId && b.IsActive, cancellationToken);
        if (!barberExists) return BookingResult.Fail(BookingError.BarberNotFound);

        var start = appointmentDate;
        if (start <= Now) return BookingResult.Fail(BookingError.InPast);
        if (!IsValidStart(start, service.DurationMinutes)) return BookingResult.Fail(BookingError.InvalidTime);

        var end = start.AddMinutes(service.DurationMinutes);

        // Транзакцията е SERIALIZABLE: проверката за припокриване и записът стават атомарно,
        // така че две едновременни заявки за един и същ час не могат и двете да успеят.
        var strategy = db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // При повторен опит (EnableRetryOnFailure) обектите от предишния опит са още проследени.
            // Изчистваме ги, за да не се вмъкнат два пъти клиент/резервация.
            db.ChangeTracker.Clear();

            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var taken = await db.Appointments.AnyAsync(a =>
                a.BarberId == barberId
                && a.Status != AppointmentStatus.Cancelled
                && a.AppointmentDate < end
                && a.EndDate > start, cancellationToken);
            if (taken) return BookingResult.Fail(BookingError.SlotTaken);

            var client = await db.Clients.FirstOrDefaultAsync(c => c.PhoneNumber == phone, cancellationToken);
            var name = clientName.Trim();
            var email = string.IsNullOrWhiteSpace(clientEmail) ? null : clientEmail.Trim();

            if (client is null)
            {
                client = new Client { FullName = name, PhoneNumber = phone, Email = email };
                db.Clients.Add(client);
            }
            else
            {
                // Името и имейлът се обновяват, но само ако са подадени.
                if (!string.IsNullOrWhiteSpace(name)) client.FullName = name;
                if (email is not null) client.Email = email;
            }

            var appointment = new Appointment
            {
                Client = client,
                BarberId = barberId,
                ServiceId = serviceId,
                AppointmentDate = start,
                EndDate = end,
                Status = AppointmentStatus.Confirmed,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
            };
            db.Appointments.Add(appointment);

            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            appointment.Service = service;
            return BookingResult.Ok(appointment);
        });
    }

    // ------------------------------------------------------------------ четене

    public Task<Appointment?> GetAppointmentAsync(int id, CancellationToken cancellationToken = default) =>
        db.Appointments.AsNoTracking()
            .Include(a => a.Client).Include(a => a.Barber).Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<Client?> GetClientByIdAsync(int clientId, CancellationToken cancellationToken = default) =>
        db.Clients.AsNoTracking().FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);

    public Task<Client?> GetClientByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        return db.Clients.AsNoTracking()
            .FirstOrDefaultAsync(c => c.PhoneNumber == normalized, cancellationToken);
    }

    public Task<Barber?> GetBarberByIdAsync(int barberId, CancellationToken cancellationToken = default) =>
        db.Barbers.AsNoTracking().FirstOrDefaultAsync(b => b.Id == barberId && b.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetBarberAppointmentsAsync(
        int barberId, CancellationToken cancellationToken = default) =>
        await db.Appointments.AsNoTracking()
            .Include(a => a.Client).Include(a => a.Service)
            .Where(a => a.BarberId == barberId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetClientAppointmentsAsync(
        int clientId, CancellationToken cancellationToken = default) =>
        await db.Appointments.AsNoTracking()
            .Include(a => a.Barber).Include(a => a.Service)
            .Where(a => a.ClientId == clientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToListAsync(cancellationToken);

    // ------------------------------------------------------------------ отмяна

    public async Task<CancelResult> CancelAppointmentAsync(
        int appointmentId, int clientId, CancellationToken cancellationToken = default)
    {
        var appointment = await db.Appointments
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.ClientId == clientId, cancellationToken);

        if (appointment is null) return CancelResult.NotFound;
        if (appointment.Status == AppointmentStatus.Cancelled) return CancelResult.AlreadyCancelled;
        if (appointment.Status == AppointmentStatus.Completed || appointment.AppointmentDate <= Now)
            return CancelResult.InPast;

        appointment.Status = AppointmentStatus.Cancelled;
        await db.SaveChangesAsync(cancellationToken);
        return CancelResult.Cancelled;
    }

    // ------------------------------------------------------------------ еднократни кодове

    public async Task<(OtpRequestResult Result, string? Code)> RequestOtpAsync(
        string phone, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        if (!PhoneNormalizer.IsValid(normalized)) return (OtpRequestResult.UnknownPhone, null);

        var clientExists = await db.Clients.AsNoTracking()
            .AnyAsync(c => c.PhoneNumber == normalized, cancellationToken);
        if (!clientExists) return (OtpRequestResult.UnknownPhone, null);

        var nowUtc = time.GetUtcNow().UtcDateTime;
        var windowStart = nowUtc.AddMinutes(-options.OtpRequestWindowMinutes);
        var recent = await db.OneTimeCodes.CountAsync(
            o => o.PhoneNumber == normalized && o.CreatedOn >= windowStart, cancellationToken);
        if (recent >= options.OtpMaxRequestsPerWindow) return (OtpRequestResult.TooManyRequests, null);

        // Новият код анулира всички предишни активни за този номер.
        var active = await db.OneTimeCodes
            .Where(o => o.PhoneNumber == normalized && !o.Used && o.ExpiresAt > nowUtc)
            .ToListAsync(cancellationToken);
        foreach (var old in active) old.Used = true;

        var code = OtpHasher.GenerateCode();
        db.OneTimeCodes.Add(new OneTimeCode
        {
            PhoneNumber = normalized,
            CodeHash = OtpHasher.Hash(options.OtpSecret, normalized, code),
            ExpiresAt = nowUtc.AddMinutes(options.OtpValidMinutes),
            CreatedOn = nowUtc
        });

        // Изчистване на стари записи, за да не расте таблицата безкрайно.
        var stale = await db.OneTimeCodes
            .Where(o => o.CreatedOn < nowUtc.AddDays(-1))
            .ToListAsync(cancellationToken);
        db.OneTimeCodes.RemoveRange(stale);

        await db.SaveChangesAsync(cancellationToken);
        return (OtpRequestResult.Sent, code);
    }

    public async Task<(OtpVerifyResult Result, int? ClientId)> VerifyOtpAsync(
        string phone, string code, CancellationToken cancellationToken = default)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        var nowUtc = time.GetUtcNow().UtcDateTime;

        var otp = await db.OneTimeCodes
            .Where(o => o.PhoneNumber == normalized && !o.Used)
            .OrderByDescending(o => o.CreatedOn)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null) return (OtpVerifyResult.InvalidCode, null);

        if (otp.ExpiresAt <= nowUtc)
        {
            otp.Used = true;
            await db.SaveChangesAsync(cancellationToken);
            return (OtpVerifyResult.Expired, null);
        }

        if (otp.FailedAttempts >= options.OtpMaxFailedAttempts)
        {
            otp.Used = true;
            await db.SaveChangesAsync(cancellationToken);
            return (OtpVerifyResult.TooManyAttempts, null);
        }

        if (!OtpHasher.Verify(options.OtpSecret, normalized, code.Trim(), otp.CodeHash))
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= options.OtpMaxFailedAttempts) otp.Used = true;
            await db.SaveChangesAsync(cancellationToken);
            return (otp.Used ? OtpVerifyResult.TooManyAttempts : OtpVerifyResult.InvalidCode, null);
        }

        otp.Used = true;
        await db.SaveChangesAsync(cancellationToken);

        var clientId = await db.Clients.AsNoTracking()
            .Where(c => c.PhoneNumber == normalized)
            .Select(c => (int?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return clientId is null
            ? (OtpVerifyResult.InvalidCode, null)
            : (OtpVerifyResult.Success, clientId);
    }

    // ------------------------------------------------------------------ правила за график

    private bool IsBookableDay(DateTime day)
    {
        var today = Now.Date;
        if (day < today || day > today.AddDays(options.MaxDaysAhead)) return false;
        return !options.ClosedDays.Contains(day.DayOfWeek);
    }

    /// <summary>Часът е в работния ден, на кратна на слота граница и завършва до затваряне.</summary>
    private bool IsValidStart(DateTime start, int durationMinutes)
    {
        var day = start.Date;
        if (!IsBookableDay(day)) return false;

        var open = day.Add(options.OpeningTime);
        var close = day.Add(options.ClosingTime);
        if (start < open || start.AddMinutes(durationMinutes) > close) return false;
        if (start.Second != 0 || start.Millisecond != 0) return false;

        var minutesFromOpen = (start - open).TotalMinutes;
        return minutesFromOpen % options.SlotMinutes == 0;
    }
}
