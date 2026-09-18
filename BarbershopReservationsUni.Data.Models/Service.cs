using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarbershopReservationsUni.Data.Models
{
    public class Service
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Името на услугата е задължително.")]
        [StringLength(100)]
        [Display(Name = "Наименование")]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        [Display(Name = "Описание")]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(8,2)")]
        [Range(0, 9999, ErrorMessage = "Цената трябва да е положително число.")]
        [Display(Name = "Цена (лв.)")]
        public decimal Price { get; set; }

        [Required]
        [Range(5, 480, ErrorMessage = "Продължителността трябва да е между 5 и 480 минути.")]
        [Display(Name = "Продължителност (мин.)")]
        public int DurationMinutes { get; set; }

        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
