using System.Diagnostics;
using BarbershopReservationsUni.Services;
using BarbershopReservationsUni.Web.Models;
using BarbershopReservationsUni.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BarbershopReservationsUni.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IBookingService bookings;

        public HomeController(IBookingService bookings) => this.bookings = bookings;

        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var viewModel = new HomeViewModel
            {
                Services = await bookings.GetServicesAsync(cancellationToken),
                Barbers = await bookings.GetActiveBarbersAsync(cancellationToken)
            };

            return View(viewModel);
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
