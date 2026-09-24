namespace BarbershopReservationsUni.Web.ViewModels;

public class MyBookingsViewModel
{
    /// <summary>Телефон, въведен във формата за вход (за връщане в полето при грешка).</summary>
    public string Phone { get; set; } = string.Empty;

    public bool IsSignedIn { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;

    public List<AppointmentListItemViewModel> Bookings { get; set; } = [];

    public string? Message { get; set; }
}
