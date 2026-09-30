using BarbershopReservationsUni.Services;
using Microsoft.AspNetCore.Mvc;

namespace BarbershopReservationsUni.Web.Controllers
{
    public class BarbersController : Controller
    {
        private readonly IBookingService bookings;

        public BarbersController(IBookingService bookings) => this.bookings = bookings;

        public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
            View(await bookings.GetActiveBarbersAsync(cancellationToken));
    }
}
