using System.ComponentModel.DataAnnotations;

namespace BarbershopReservationsUni.Data.Models
{
    
    public class Barber
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на бръснаря е задължително.")]
        [StringLength(100)]
        [Display(Name = "Име")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Специализация")]
        public string? Specialization { get; set; }

        [Phone(ErrorMessage = "Невалиден телефонен номер.")]
        [StringLength(20)]
        [Display(Name = "Телефон")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Активен")]
        public bool IsActive { get; set; } = true;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
