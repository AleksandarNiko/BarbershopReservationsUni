using BarbershopReservationsUni.Data;
using BarbershopReservationsUni.Data.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace BarbershopReservationsUni.Services.Tests;

/// <summary>SQLite in-memory база + управляемо време. Всеки тест получава собствена, изолирана база.</summary>
public abstract class TestBase : IDisposable
{
    private readonly SqliteConnection connection;

    protected readonly FakeTimeProvider Time;
    protected readonly BookingOptions Options;
    protected readonly BarbershopReservationsUniDbContext Db;
    protected readonly BookingService Sut;

    /// <summary>Понеделник, 21.09.2026, 08:00 – преди отваряне на салона.</summary>
    protected static readonly DateTimeOffset StartOfWeek = new(2026, 9, 21, 8, 0, 0, TimeSpan.Zero);

    protected TestBase()
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var dbOptions = new DbContextOptionsBuilder<BarbershopReservationsUniDbContext>()
            .UseSqlite(connection)
            .Options;

        Db = new BarbershopReservationsUniDbContext(dbOptions);
        Db.Database.EnsureCreated(); // включва HasData: 4 услуги и 2 бръснари

        Time = new FakeTimeProvider(StartOfWeek);
        Time.SetLocalTimeZone(TimeZoneInfo.Utc);

        Options = new BookingOptions { OtpSecret = new string('x', 40) };
        Sut = new BookingService(Db, Microsoft.Extensions.Options.Options.Create(Options), Time);
    }

    protected static DateTime At(int day, int hour, int minute = 0) => new(2026, 9, day, hour, minute, 0);

    protected Task<BookingResult> Book(
        DateTime start, int serviceId = 1, int barberId = 1,
        string name = "Петър Петров", string phone = "0888 123 456") =>
        Sut.CreateAppointmentAsync(serviceId, barberId, start, name, phone, null, null);

    public void Dispose()
    {
        Db.Dispose();
        connection.Dispose();
        GC.SuppressFinalize(this);
    }
}
