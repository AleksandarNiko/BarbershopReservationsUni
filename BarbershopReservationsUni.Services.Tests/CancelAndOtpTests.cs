using BarbershopReservationsUni.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BarbershopReservationsUni.Services.Tests;

public class CancelTests : TestBase
{
    [Fact]
    public async Task Owner_CanCancelFutureAppointment()
    {
        var booked = await Book(At(21, 10));
        var result = await Sut.CancelAppointmentAsync(booked.Appointment!.Id, booked.Appointment.ClientId);

        Assert.Equal(CancelResult.Cancelled, result);
        Assert.Equal(AppointmentStatus.Cancelled, (await Db.Appointments.SingleAsync()).Status);
    }

    [Fact]
    public async Task AnotherClient_CannotCancel()
    {
        var mine = await Book(At(21, 10), phone: "0888 123 456");
        var other = await Book(At(21, 11), phone: "0899 000 111");

        var result = await Sut.CancelAppointmentAsync(mine.Appointment!.Id, other.Appointment!.ClientId);

        Assert.Equal(CancelResult.NotFound, result);
        Assert.Equal(AppointmentStatus.Confirmed, (await Db.Appointments.FindAsync(mine.Appointment.Id))!.Status);
    }

    [Fact]
    public async Task CancelTwice_ReportsAlreadyCancelled()
    {
        var booked = await Book(At(21, 10));
        await Sut.CancelAppointmentAsync(booked.Appointment!.Id, booked.Appointment.ClientId);

        Assert.Equal(CancelResult.AlreadyCancelled,
            await Sut.CancelAppointmentAsync(booked.Appointment.Id, booked.Appointment.ClientId));
    }

    [Fact]
    public async Task PastAppointment_CannotBeCancelled()
    {
        var booked = await Book(At(21, 10));
        Time.SetUtcNow(new DateTimeOffset(2026, 9, 21, 11, 0, 0, TimeSpan.Zero));

        Assert.Equal(CancelResult.InPast,
            await Sut.CancelAppointmentAsync(booked.Appointment!.Id, booked.Appointment.ClientId));
    }

    [Fact]
    public async Task UnknownAppointment_IsNotFound() =>
        Assert.Equal(CancelResult.NotFound, await Sut.CancelAppointmentAsync(999, 1));
}

public class OtpTests : TestBase
{
    private const string Phone = "0888 123 456";

    private async Task SeedClient() => await Book(At(21, 10), phone: Phone);

    [Fact]
    public async Task UnknownPhone_GetsNoCode()
    {
        var (result, code) = await Sut.RequestOtpAsync("0899 999 999");

        Assert.Equal(OtpRequestResult.UnknownPhone, result);
        Assert.Null(code);
        Assert.Empty(Db.OneTimeCodes);
    }

    [Fact]
    public async Task Code_IsSixDigits_AndStoredOnlyAsHash()
    {
        await SeedClient();
        var (result, code) = await Sut.RequestOtpAsync(Phone);

        Assert.Equal(OtpRequestResult.Sent, result);
        Assert.Matches(@"^\d{6}$", code!);

        var stored = await Db.OneTimeCodes.SingleAsync();
        Assert.DoesNotContain(code!, stored.CodeHash);
        Assert.Equal(64, stored.CodeHash.Length);
    }

    [Fact]
    public async Task CorrectCode_SignsInTheRightClient()
    {
        await SeedClient();
        var (_, code) = await Sut.RequestOtpAsync(Phone);

        var (result, clientId) = await Sut.VerifyOtpAsync("+359 888 123 456", code!); // друг формат на същия номер

        Assert.Equal(OtpVerifyResult.Success, result);
        Assert.Equal((await Db.Clients.SingleAsync()).Id, clientId);
    }

    [Fact]
    public async Task Code_CanOnlyBeUsedOnce()
    {
        await SeedClient();
        var (_, code) = await Sut.RequestOtpAsync(Phone);

        await Sut.VerifyOtpAsync(Phone, code!);
        var (again, _) = await Sut.VerifyOtpAsync(Phone, code!);

        Assert.Equal(OtpVerifyResult.InvalidCode, again);
    }

    [Fact]
    public async Task WrongCode_IsRejected()
    {
        await SeedClient();
        var (_, code) = await Sut.RequestOtpAsync(Phone);
        var wrong = code == "000000" ? "111111" : "000000";

        var (result, clientId) = await Sut.VerifyOtpAsync(Phone, wrong);

        Assert.Equal(OtpVerifyResult.InvalidCode, result);
        Assert.Null(clientId);
    }

    [Fact]
    public async Task ExpiredCode_IsRejected()
    {
        await SeedClient();
        var (_, code) = await Sut.RequestOtpAsync(Phone);
        Time.Advance(TimeSpan.FromMinutes(Options.OtpValidMinutes + 1));

        Assert.Equal(OtpVerifyResult.Expired, (await Sut.VerifyOtpAsync(Phone, code!)).Result);
    }

    [Fact]
    public async Task TooManyWrongAttempts_BurnTheCode_EvenForTheCorrectOne()
    {
        await SeedClient();
        var (_, code) = await Sut.RequestOtpAsync(Phone);
        var wrong = code == "000000" ? "111111" : "000000";

        for (var i = 0; i < Options.OtpMaxFailedAttempts; i++)
            await Sut.VerifyOtpAsync(Phone, wrong);

        var (result, _) = await Sut.VerifyOtpAsync(Phone, code!);
        Assert.NotEqual(OtpVerifyResult.Success, result);
    }

    [Fact]
    public async Task NewCode_InvalidatesThePreviousOne()
    {
        await SeedClient();
        var (_, first) = await Sut.RequestOtpAsync(Phone);
        var (_, second) = await Sut.RequestOtpAsync(Phone);

        if (first != second)
            Assert.NotEqual(OtpVerifyResult.Success, (await Sut.VerifyOtpAsync(Phone, first!)).Result);
    }

    [Fact]
    public async Task RequestsAreRateLimited_PerPhone()
    {
        await SeedClient();

        for (var i = 0; i < Options.OtpMaxRequestsPerWindow; i++)
            Assert.Equal(OtpRequestResult.Sent, (await Sut.RequestOtpAsync(Phone)).Result);

        Assert.Equal(OtpRequestResult.TooManyRequests, (await Sut.RequestOtpAsync(Phone)).Result);

        Time.Advance(TimeSpan.FromMinutes(Options.OtpRequestWindowMinutes + 1));
        Assert.Equal(OtpRequestResult.Sent, (await Sut.RequestOtpAsync(Phone)).Result);
    }

    [Fact]
    public async Task CodeForOnePhone_DoesNotWorkForAnother()
    {
        await SeedClient();
        await Book(At(21, 11), phone: "0899 000 111");
        var (_, code) = await Sut.RequestOtpAsync(Phone);

        var (result, _) = await Sut.VerifyOtpAsync("0899 000 111", code!);
        Assert.NotEqual(OtpVerifyResult.Success, result);
    }
}
