 using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Web.ViewModels;

public class AppointmentListItemViewModel
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string BarberName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public AppointmentStatus Status { get; set; }
    public decimal Price { get; set; }
}
