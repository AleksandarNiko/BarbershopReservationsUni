using System.ComponentModel.DataAnnotations;

namespace BarbershopReservationsUni.Data.Models
{
    public class Client
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името е задължително.")]
        [StringLength(100)]
        [Display(Name = "Име и фамилия")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Телефонният номер е задължителен.")]
        [Phone(ErrorMessage = "Невалиден телефонен номер.")]
        [StringLength(20)]
        [Display(Name = "Телефон")]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Невалиден имейл адрес.")]
        [StringLength(150)]
        [Display(Name = "Имейл")]
        public string? Email { get; set; }

        [Display(Name = "Дата на регистрация")]
        public DateTime RegisteredOn { get; set; } = DateTime.UtcNow;

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
