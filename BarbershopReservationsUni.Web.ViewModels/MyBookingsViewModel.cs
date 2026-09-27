namespace BarbershopReservationsUni.Web.ViewModels;

public class MyBookingsViewModel
{
    public string Phone { get; set; } = string.Empty;
    public bool IsSignedIn { get; set; }
    public bool IsBarber { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string BarberName { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public List<AppointmentListItemViewModel> Bookings { get; set; } = [];
    public string? Message { get; set; }
}
