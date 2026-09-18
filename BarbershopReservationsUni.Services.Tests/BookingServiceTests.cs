using BarbershopReservationsUni.Data.Models;
using Xunit;

namespace BarbershopReservationsUni.Services.Tests;

public class BookingServiceTests
{
    [Fact]
    public void AppointmentStatus_DefaultsToPending()
    {
        var appointment = new Appointment();
        Assert.Equal(AppointmentStatus.Pending, appointment.Status);
    }

    [Fact]
    public void NewClient_GetsRegistrationDate()
    {
        var before = DateTime.Now;
        var client = new Client();
        var after = DateTime.Now;

        Assert.InRange(client.RegisteredOn, before, after);
    }
}
