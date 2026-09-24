using System.ComponentModel.DataAnnotations;

namespace BarbershopReservationsUni.Data.Models
{
    /// <summary>
    /// Еднократен код за потвърждение на телефонен номер (вход в „Моите часове“).
    /// Кодът се съхранява само като хеш – никога в чист вид.
    /// </summary>
    public class OneTimeCode
    {
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>HMAC-SHA256 (hex) на кода. Виж <c>OtpHasher</c>.</summary>
        [Required]
        [StringLength(64)]
        public string CodeHash { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool Used { get; set; }

        /// <summary>Брой неуспешни опити за въвеждане. При достигане на лимита кодът се анулира.</summary>
        public int FailedAttempts { get; set; }

        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    }
}
