using BarbershopReservationsUni.Data.Models;
using BarbershopReservationsUni.Services;
using BarbershopReservationsUni.Web.Services;
using BarbershopReservationsUni.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace BarbershopReservationsUni.Web.Controllers;

public class MyBookingsController : Controller
{
    private readonly IBookingService bookings;
    private readonly ClientSession clientSession;
    private readonly BarberSession barberSession;
    private readonly TimeProvider time;

    public MyBookingsController(IBookingService bookings, ClientSession clientSession, BarberSession barberSession, TimeProvider time)
    {
        this.bookings = bookings;
        this.clientSession = clientSession;
        this.barberSession = barberSession;
        this.time = time;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var vm = new MyBookingsViewModel();
        if (TempData["Message"] is string message) vm.Message = message;

        var barberId = barberSession.GetBarberId(HttpContext);
        if (barberId is not null)
        {
            var barber = await bookings.GetBarberByIdAsync(barberId.Value, cancellationToken);
            if (barber is null) { barberSession.SignOut(HttpContext); return View(vm); }

            var appointments = await bookings.GetBarberAppointmentsAsync(barber.Id, cancellationToken);
            vm.IsSignedIn = true;
            vm.IsBarber = true;
            vm.BarberName = barber.FullName;
            vm.Bookings = appointments.Select(a => new AppointmentListItemViewModel
            {
                Id = a.Id,
                ServiceName = a.Service?.Name ?? string.Empty,
                BarberName = barber.FullName,
                ClientName = a.Client?.FullName ?? string.Empty,
                ClientPhone = a.Client?.PhoneNumber ?? string.Empty,
                AppointmentDate = a.AppointmentDate,
                Status = a.Status,
                Price = a.Service?.Price ?? 0m
            }).ToList();
            return View(vm);
        }

        var clientId = clientSession.GetClientId(HttpContext);
        if (clientId is null) return View(vm);

        var client = await bookings.GetClientByIdAsync(clientId.Value, cancellationToken);
        if (client is null) { clientSession.SignOut(HttpContext); return View(vm); }

        var clientAppointments = await bookings.GetClientAppointmentsAsync(client.Id, cancellationToken);
        var now = time.GetLocalNow().DateTime;
        vm.IsSignedIn = true;
        vm.ClientName = client.FullName;
        vm.MaskedPhone = PhoneNormalizer.Mask(client.PhoneNumber);
        vm.Bookings = clientAppointments.Select(a => new AppointmentListItemViewModel
        {
            Id = a.Id,
            ServiceName = a.Service?.Name ?? string.Empty,
            BarberName = a.Barber?.FullName ?? string.Empty,
            AppointmentDate = a.AppointmentDate,
            Status = a.Status,
            Price = a.Service?.Price ?? 0m,
            CanCancel = a.AppointmentDate > now && (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending)
        }).ToList();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string? phone, CancellationToken cancellationToken)
    {
        var value = phone?.Trim() ?? string.Empty;
        var vm = new MyBookingsViewModel { Phone = value };

        // Същото поле служи и за вход на двамата бръснари.
        if (value == "1000000000" || value == "2000000000")
        {
            var barberId = value == "1000000000" ? 1 : 2;
            var barber = await bookings.GetBarberByIdAsync(barberId, cancellationToken);
            if (barber is null) { vm.Message = "Профилът на бръснаря не е намерен."; return View(nameof(Index), vm); }
            clientSession.SignOut(HttpContext);
            barberSession.SignIn(HttpContext, barberId);
            return RedirectToAction(nameof(Index));
        }

        if (!PhoneNormalizer.IsValid(value))
        {
            vm.Message = "Въведете валиден телефонен номер или парола за бръснар.";
            return View(nameof(Index), vm);
        }

        var client = await bookings.GetClientByPhoneAsync(value, cancellationToken);
        if (client is null)
        {
            vm.Message = "Няма намерени резервации за този телефонен номер.";
            return View(nameof(Index), vm);
        }

        barberSession.SignOut(HttpContext);
        clientSession.SignIn(HttpContext, client.Id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var clientId = clientSession.GetClientId(HttpContext);
        if (clientId is null) return RedirectToAction(nameof(Index));
        var result = await bookings.CancelAppointmentAsync(id, clientId.Value, cancellationToken);
        TempData["Message"] = result switch
        {
            CancelResult.Cancelled => "Часът е отменен.",
            CancelResult.AlreadyCancelled => "Този час вече е отменен.",
            CancelResult.InPast => "Не можете да отмените час, който вече е минал.",
            _ => "Часът не е намерен."
        };
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        clientSession.SignOut(HttpContext);
        barberSession.SignOut(HttpContext);
        return RedirectToAction(nameof(Index));
    }
}
