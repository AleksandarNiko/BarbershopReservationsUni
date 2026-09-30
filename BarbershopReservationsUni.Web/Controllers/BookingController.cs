using System.Globalization;
using BarbershopReservationsUni.Services;
using BarbershopReservationsUni.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BarbershopReservationsUni.Web.Controllers
{
    public class BookingController : Controller
    {
        private readonly IBookingService bookings;

        public BookingController(IBookingService bookings) => this.bookings = bookings;

        // GET: /Booking/AvailableTimes?barberId=1&serviceId=2&date=2026-09-19
        [HttpGet]
        public async Task<IActionResult> AvailableTimes(
            int barberId, int serviceId, string? date, CancellationToken cancellationToken)
        {
            if (barberId <= 0 || serviceId <= 0 ||
                !DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                return Json(Array.Empty<string>());
            }

            var times = await bookings.GetAvailableTimesAsync(barberId, serviceId, day, cancellationToken);
            return Json(times);
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? serviceId, int? barberId, CancellationToken cancellationToken)
        {
            var model = new BookingViewModel
            {
                ServiceId = serviceId ?? 0,
                BarberId = barberId ?? 0
            };

            await FillListsAsync(model, cancellationToken);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingViewModel model, CancellationToken cancellationToken)
        {
            if (ModelState.IsValid && model.TryGetTime(out var time))
            {
                var result = await bookings.CreateAppointmentAsync(
                    model.ServiceId,
                    model.BarberId,
                    model.Date.Date + time,
                    model.ClientName,
                    model.ClientPhone,
                    model.ClientEmail,
                    model.Notes,
                    cancellationToken);

                if (result.Succeeded)
                {
                    // Post/Redirect/Get – презареждането на страницата не създава втора резервация.
                    // ID-то се предава през TempData, а не през URL, за да не може да се обхождат чужди резервации.
                    TempData["JustBookedId"] = result.Appointment!.Id;
                    return RedirectToAction(nameof(Confirmation));
                }

                ModelState.AddModelError(string.Empty, Describe(result.Error));
            }

            await FillListsAsync(model, cancellationToken);
            return View(nameof(Index), model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(CancellationToken cancellationToken)
        {
            if (TempData["JustBookedId"] is not int appointmentId)
            {
                // Няма току-що направена резервация (директно отваряне на адреса, презареждане, чужд линк).
                return RedirectToAction(nameof(Index));
            }

            // Пазим ID-то и за едно презареждане на страницата.
            TempData.Keep("JustBookedId");

            var appointment = await bookings.GetAppointmentAsync(appointmentId, cancellationToken);
            if (appointment is null) return RedirectToAction(nameof(Index));

            return View(new BookingConfirmationViewModel
            {
                AppointmentId = appointment.Id,
                ClientName = appointment.Client?.FullName ?? string.Empty,
                BarberName = appointment.Barber?.FullName ?? string.Empty,
                ServiceName = appointment.Service?.Name ?? string.Empty,
                AppointmentDate = appointment.AppointmentDate,
                Price = appointment.Service?.Price ?? 0m,
                Status = appointment.Status
            });
        }

        private async Task FillListsAsync(BookingViewModel model, CancellationToken cancellationToken)
        {
            model.Services = await bookings.GetServicesAsync(cancellationToken);
            model.Barbers = await bookings.GetActiveBarbersAsync(cancellationToken);

            // Ако вече има избрани услуга, бръснар и дата (напр. след грешка), показваме реалните свободни часове.
            model.AvailableTimes = model.ServiceId > 0 && model.BarberId > 0
                ? await bookings.GetAvailableTimesAsync(model.BarberId, model.ServiceId, model.Date, cancellationToken)
                : [];
        }

        private static string Describe(BookingError error) => error switch
        {
            BookingError.SlotTaken => "Този час току-що беше зает. Моля, изберете друг час или бръснар.",
            BookingError.InPast => "Избраният час вече е минал. Моля, изберете бъдещ час.",
            BookingError.InvalidTime => "Избраният час е извън работното време или не е валиден слот.",
            BookingError.ServiceNotFound => "Избраната услуга не съществува.",
            BookingError.BarberNotFound => "Избраният бръснар не е наличен.",
            BookingError.InvalidPhone => "Въведете валиден български телефонен номер (напр. 0888 123 456).",
            _ => "Резервацията не можа да бъде създадена. Опитайте отново."
        };
    }
}
