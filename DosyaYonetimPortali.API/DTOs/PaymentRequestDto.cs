using System.ComponentModel.DataAnnotations;

namespace DosyaYonetimPortali.API.DTOs
{
    public class PaymentRequestDto
    {
        [Required(ErrorMessage = "Paket seçimi zorunludur.")]
        public int PackageId { get; set; }

        [Required(ErrorMessage = "Kart numarası zorunludur.")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Kart numarası 16 haneli olmalıdır.")]
        public string CardNumber { get; set; }

        public string CardHolderName { get; set; }
        public string ExpirationDate { get; set; } 
        public string Cvv { get; set; }
    }
}