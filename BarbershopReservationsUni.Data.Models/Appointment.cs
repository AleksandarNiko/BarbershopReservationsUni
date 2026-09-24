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

        [Display(Name = "Клиент")]
        public int ClientId { get; set; }

        [ForeignKey(nameof(ClientId))]
        public Client? Client { get; set; }

        [Display(Name = "Бръснар")]
        public int BarberId { get; set; }

        [ForeignKey(nameof(BarberId))]
        public Barber? Barber { get; set; }

        [Display(Name = "Услуга")]
        public int ServiceId { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public Service? Service { get; set; }

        /// <summary>Начало на часа (локално време на салона).</summary>
        [Display(Name = "Дата и час")]
        [DataType(DataType.DateTime)]
        public DateTime AppointmentDate { get; set; }

        /// <summary>
        /// Край на часа. Съхранява се заедно с началото, за да може припокриването
        /// да се проверява директно в SQL, без да се зареждат всички резервации.
        /// </summary>
        [Display(Name = "Край")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Статус")]
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;

        [StringLength(300)]
        [Display(Name = "Бележка")]
        public string? Notes { get; set; }

        [Display(Name = "Дата на създаване")]
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
