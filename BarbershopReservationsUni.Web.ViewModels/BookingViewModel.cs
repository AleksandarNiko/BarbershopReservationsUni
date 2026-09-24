using System.ComponentModel.DataAnnotations;
using BarbershopReservationsUni.Data.Models;

namespace BarbershopReservationsUni.Web.ViewModels;

public class BookingViewModel : IValidatableObject
{
    [Range(1, int.MaxValue, ErrorMessage = "Изберете услуга.")]
    [Display(Name = "Услуга")]
    public int ServiceId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Изберете бръснар.")]
    [Display(Name = "Бръснар")]
    public int BarberId { get; set; }

    [Required(ErrorMessage = "Изберете дата.")]
    [DataType(DataType.Date)]
    [Display(Name = "Дата")]
    public DateTime Date { get; set; } = DateTime.Today.AddDays(1);

    [Required(ErrorMessage = "Изберете час.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Часът трябва да е във формат ЧЧ:ММ.")]
    [Display(Name = "Час")]
    public string Time { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Името трябва да е между 2 и 100 символа.")]
    [Display(Name = "Име и фамилия")]
    public string ClientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете телефон.")]
    [StringLength(20, ErrorMessage = "Телефонът е твърде дълъг.")]
    [Display(Name = "Телефон")]
    public string ClientPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Въведете валиден имейл.")]
    [StringLength(150)]
    [Display(Name = "Имейл")]
    public string? ClientEmail { get; set; }

    [StringLength(300, ErrorMessage = "Бележката е най-много 300 символа.")]
    [Display(Name = "Бележка")]
    public string? Notes { get; set; }

    // Само за визуализация – не се обвързват от формата.
    public IEnumerable<Service> Services { get; set; } = [];
    public IEnumerable<Barber> Barbers { get; set; } = [];
    public IEnumerable<string> AvailableTimes { get; set; } = [];

    /// <summary>Часът като TimeSpan; null, ако не е валиден.</summary>
    public bool TryGetTime(out TimeSpan time) =>
        TimeSpan.TryParseExact(Time, @"hh\:mm", null, out time);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!TryGetTime(out _))
            yield return new ValidationResult("Изберете валиден час.", [nameof(Time)]);
    }
}
