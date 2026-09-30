using BarbershopReservationsUni.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarbershopReservationsUni.Web.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IBookingService bookings;

        public ServicesController(IBookingService bookings) => this.bookings = bookings;

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await bookings.GetServicesAsync(cancellationToken));
    }
}
