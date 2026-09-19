using BarbershopReservationsUni.Web.Models;
using BarbershopReservationsUni.Web.ViewModels;
using BarbershopReservationsUni.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BarbershopReservationsUni.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly BarbershopReservationsUniDbContext _db;

        public HomeController(BarbershopReservationsUniDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            var viewModel = new HomeViewModel
            {
                Services = _db.Services.OrderBy(s => s.Id).ToList(),
                Barbers = _db.Barbers.Where(b => b.IsActive).OrderBy(b => b.Id).ToList(),
                CompletedAppointments = _db.Appointments.Count()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
