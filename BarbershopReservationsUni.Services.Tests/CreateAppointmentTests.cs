using BarbershopReservationsUni.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BarbershopReservationsUni.Services.Tests;

public class CreateAppointmentTests : TestBase
{
    [Fact]
    public async Task ValidBooking_IsConfirmed_WithCorrectEnd()
    {
        var result = await Book(At(21, 10), serviceId: 3); // 45 мин.

        Assert.True(result.Succeeded);
        var a = result.Appointment!;
        Assert.Equal(AppointmentStatus.Confirmed, a.Status);
        Assert.Equal(At(21, 10), a.AppointmentDate);
        Assert.Equal(At(21, 10, 45), a.EndDate);
    }

    [Fact]
    public async Task SameSlot_SameBarber_SecondBookingIsRejected()
    {
        Assert.True((await Book(At(21, 10))).Succeeded);

        var second = await Book(At(21, 10), phone: "0899 000 111");

        Assert.False(second.Succeeded);
        Assert.Equal(BookingError.SlotTaken, second.Error);
        Assert.Equal(1, await Db.Appointments.CountAsync());
    }

    [Fact]
    public async Task OverlappingBooking_IsRejected()
    {
        await Book(At(21, 10), serviceId: 3);                     // 10:00–10:45
        var overlap = await Book(At(21, 10, 30), phone: "0899 000 111"); // 10:30–11:00

        Assert.Equal(BookingError.SlotTaken, overlap.Error);
    }

    [Fact]
    public async Task BackToBackBookings_AreAllowed()
    {
        Assert.True((await Book(At(21, 10))).Succeeded);                          // 10:00–10:30
        Assert.True((await Book(At(21, 10, 30), phone: "0899 000 111")).Succeeded); // 10:30–11:00
    }

    [Fact]
    public async Task SameSlot_DifferentBarber_IsAllowed()
    {
        Assert.True((await Book(At(21, 10), barberId: 1)).Succeeded);
        Assert.True((await Book(At(21, 10), barberId: 2, phone: "0899 000 111")).Succeeded);
    }

    [Fact]
    public async Task CancelledSlot_CanBeBookedAgain()
    {
        var first = await Book(At(21, 10));
        await Sut.CancelAppointmentAsync(first.Appointment!.Id, first.Appointment.ClientId);

        Assert.True((await Book(At(21, 10), phone: "0899 000 111")).Succeeded);
    }

    [Theory]
    [InlineData(21, 8, 30)]   // преди отваряне
    [InlineData(21, 19, 0)]   // в момента на затваряне
    [InlineData(21, 18, 45)]  // край след затваряне (30 мин. услуга → 19:15)
    [InlineData(21, 10, 15)]  // не е на граница на слот
    [InlineData(27, 10, 0)]   // неделя
    public async Task InvalidTime_IsRejected(int day, int hour, int minute)
    {
        var result = await Book(At(day, hour, minute));
        Assert.Equal(BookingError.InvalidTime, result.Error);
    }

    [Fact]
    public async Task LongServiceEndingAfterClosing_IsRejected()
    {
        var result = await Book(At(21, 18, 30), serviceId: 3); // 45 мин. → 19:15
        Assert.Equal(BookingError.InvalidTime, result.Error);
    }

    [Fact]
    public async Task PastTime_IsRejected()
    {
        Time.SetUtcNow(new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero));
        var result = await Book(At(21, 11));
        Assert.Equal(BookingError.InPast, result.Error);
    }

    [Fact]
    public async Task UnknownService_IsRejected() =>
        Assert.Equal(BookingError.ServiceNotFound, (await Book(At(21, 10), serviceId: 999)).Error);

    [Fact]
    public async Task UnknownOrInactiveBarber_IsRejected()
    {
        Assert.Equal(BookingError.BarberNotFound, (await Book(At(21, 10), barberId: 999)).Error);

        Db.Barbers.Find(1)!.IsActive = false;
        await Db.SaveChangesAsync();
        Assert.Equal(BookingError.BarberNotFound, (await Book(At(21, 10), barberId: 1)).Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("123")]
    [InlineData("0888")]
    public async Task InvalidPhone_IsRejected(string phone) =>
        Assert.Equal(BookingError.InvalidPhone, (await Book(At(21, 10), phone: phone)).Error);

    [Fact]
    public async Task SamePhoneInDifferentFormats_ReusesOneClient()
    {
        await Book(At(21, 10), phone: "0888 123 456");
        await Book(At(21, 11), phone: "+359 888 123 456", name: "Петър П.");
        await Book(At(21, 12), phone: "0888-123-456");

        Assert.Equal(1, await Db.Clients.CountAsync());
        Assert.Equal(3, await Db.Appointments.CountAsync());
        Assert.Equal("0888123456", (await Db.Clients.SingleAsync()).PhoneNumber);
    }

    [Fact]
    public async Task ExistingClient_KeepsEmail_WhenNewBookingHasNone()
    {
        await Sut.CreateAppointmentAsync(1, 1, At(21, 10), "Иван", "0888123456", "ivan@example.com", null);
        await Sut.CreateAppointmentAsync(1, 1, At(21, 11), "Иван", "0888123456", null, null);

        Assert.Equal("ivan@example.com", (await Db.Clients.SingleAsync()).Email);
    }

    [Fact]
    public async Task ConflictDoesNotCreateAClient()
    {
        await Book(At(21, 10));
        await Book(At(21, 10), phone: "0899 000 111", name: "Друг");

        Assert.Equal(1, await Db.Clients.CountAsync());
    }
}
