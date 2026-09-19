using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Web.ViewModels;

public class HomeViewModel
{
    public IReadOnlyList<Service> Services { get; init; } = [];
    public IReadOnlyList<Barber> Barbers { get; init; } = [];
    public int CompletedAppointments { get; init; }
}
