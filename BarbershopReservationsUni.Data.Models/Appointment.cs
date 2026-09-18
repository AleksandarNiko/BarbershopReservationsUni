using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BarbershopReservationsUni.Data.Models
{
    public enum AppointmentStatus
    {
        [Display(Name = "Потвърдена")]
        Confirmed = 0,

        [Display(Name = "Изчаква")]
        Pending = 1,

        [Display(Name = "Отказана")]
        Cancelled = 2,

        [Display(Name = "Завършена")]
        Completed = 3
    }
    public class Appointment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Изберете клиент.")]
        [Display(Name = "Клиент")]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        [Required(ErrorMessage = "Изберете бръснар.")]
        [Display(Name = "Бръснар")]
        public int BarberId { get; set; }

        [ForeignKey(nameof(BarberId))]
        public Barber? Barber { get; set; }

        [Required(ErrorMessage = "Изберете услуга.")]
        [Display(Name = "Услуга")]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service? Service { get; set; }

        [Required(ErrorMessage = "Датата и часът са задължителни.")]
        [Display(Name = "Дата и час")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; } = DateTime.Now.AddHours(1);

        [Required]
        [Display(Name = "Статус")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        [StringLength(300)]
        [Display(Name = "Бележка")]
        public string? Notes { get; set; }

        [Display(Name = "Дата на създаване")]
        public DateTime CreatedOn { get; set; } = DateTime.Now;
    }
}
