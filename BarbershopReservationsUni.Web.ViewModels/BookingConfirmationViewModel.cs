using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Web.ViewModels;

public class BookingConfirmationViewModel
{
    public int AppointmentId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string BarberName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public decimal Price { get; set; }
    public AppointmentStatus Status { get; set; }
}
