using BarbershopReservationsUni.Data.Models;
using Xunit;

namespace BarbershopReservationsUni.Services.Tests;

public class AvailabilityTests : TestBase
{
    [Fact]
    public async Task OpenDay_StartsAtOpening_AndLastSlotEndsByClosing()
    {
        // Услуга 1 = 30 мин. → последен старт 18:30 (краят е 19:00)
        var slots = await Sut.GetAvailableTimesAsync(1, 1, At(21, 0));

        Assert.Equal("09:00", slots.First());
        Assert.Equal("18:30", slots.Last());
    }

    [Fact]
    public async Task LongService_DoesNotOfferSlotsThatEndAfterClosing()
    {
        // Услуга 3 = 45 мин.: 18:00 → 18:45 е ок, 18:30 → 19:15 е след затварянето
        var slots = await Sut.GetAvailableTimesAsync(1, 3, At(21, 0));

        Assert.Contains("18:00", slots);
        Assert.DoesNotContain("18:30", slots);
    }

    [Fact]
    public async Task Sunday_IsClosed()
    {
        var slots = await Sut.GetAvailableTimesAsync(1, 1, At(27, 0)); // 27.09.2026 е неделя
        Assert.Empty(slots);
    }

    [Fact]
    public async Task PastDay_HasNoSlots()
    {
        Time.SetUtcNow(new DateTimeOffset(2026, 9, 23, 8, 0, 0, TimeSpan.Zero));
        var slots = await Sut.GetAvailableTimesAsync(1, 1, At(22, 0));
        Assert.Empty(slots);
    }

    [Fact]
    public async Task Today_HidesSlotsAlreadyPassed()
    {
        Time.SetUtcNow(new DateTimeOffset(2026, 9, 21, 12, 10, 0, TimeSpan.Zero));
        var slots = await Sut.GetAvailableTimesAsync(1, 1, At(21, 0));

        Assert.DoesNotContain("12:00", slots);
        Assert.Equal("12:30", slots.First());
    }

    [Fact]
    public async Task BeyondMaxDaysAhead_HasNoSlots()
    {
        var slots = await Sut.GetAvailableTimesAsync(1, 1, StartOfWeek.Date.AddDays(Options.MaxDaysAhead + 1));
        Assert.Empty(slots);
    }

    [Fact]
    public async Task BookedSlot_IsRemoved_ForSameBarberOnly()
    {
        await Book(At(21, 10));

        var barber1 = await Sut.GetAvailableTimesAsync(1, 1, At(21, 0));
        var barber2 = await Sut.GetAvailableTimesAsync(2, 1, At(21, 0));

        Assert.DoesNotContain("10:00", barber1);
        Assert.Contains("10:00", barber2);
    }

    [Fact]
    public async Task LongBooking_BlocksEverySlotItOverlaps()
    {
        await Book(At(21, 10), serviceId: 3); // 10:00 – 10:45

        var slots = await Sut.GetAvailableTimesAsync(1, 1, At(21, 0));

        Assert.DoesNotContain("10:00", slots);
        Assert.DoesNotContain("10:30", slots); // 10:30–11:00 се припокрива до 10:45
        Assert.Contains("11:00", slots);
        Assert.Contains("09:30", slots);       // 09:30–10:00 приключва точно когато започва другият
    }

    [Fact]
    public async Task NewLongService_CannotStartRightBeforeExistingBooking()
    {
        await Book(At(21, 10), serviceId: 1); // 10:00 – 10:30

        // 45-мин. услуга в 09:30 би стигнала до 10:15 → припокрива се
        var slots = await Sut.GetAvailableTimesAsync(1, 3, At(21, 0));

        Assert.DoesNotContain("09:30", slots);
        Assert.Contains("09:00", slots);
    }

    [Fact]
    public async Task CancelledAppointment_FreesTheSlot()
    {
        var booked = await Book(At(21, 10));
        var client = booked.Appointment!.ClientId;
        await Sut.CancelAppointmentAsync(booked.Appointment.Id, client);

        var slots = await Sut.GetAvailableTimesAsync(1, 1, At(21, 0));
        Assert.Contains("10:00", slots);
    }

    [Fact]
    public async Task UnknownServiceOrInactiveBarber_HasNoSlots()
    {
        Assert.Empty(await Sut.GetAvailableTimesAsync(1, 999, At(21, 0)));
        Assert.Empty(await Sut.GetAvailableTimesAsync(999, 1, At(21, 0)));

        Db.Barbers.Find(2)!.IsActive = false;
        await Db.SaveChangesAsync();
        Assert.Empty(await Sut.GetAvailableTimesAsync(2, 1, At(21, 0)));
    }
}
