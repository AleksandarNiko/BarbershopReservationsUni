using System.ComponentModel.DataAnnotations;
using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Web.ViewModels;

public class BookingViewModel
{
    [Required(ErrorMessage = "Изберете услуга.")]
    [Display(Name = "Услуга")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Изберете бръснар.")]
    [Display(Name = "Бръснар")]
    public int BarberId { get; set; }

    [Required(ErrorMessage = "Изберете дата.")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата")]
    public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Изберете час.")]
    [Display(Name = "Час")]
    public string Time { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(100)]
    [Display(Name = "Име и фамилия")]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете телефон.")]
    [Phone(ErrorMessage = "Въведете валиден телефон.")]
    [StringLength(20)]
    [Display(Name = "Телефон")]
    public string ClientPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Въведете валиден имейл.")]
    [StringLength(150)]
    [Display(Name = "Имейл")]
    public string? ClientEmail { get; set; }

    [StringLength(300)]
    [Display(Name = "Бележка")]
    public string? Notes { get; set; }

    public IEnumerable<Service> Services { get; set; } = [];
    public IEnumerable<Barber> Barbers { get; set; } = [];
    public IEnumerable<string> AvailableTimes { get; set; } = [];
}
