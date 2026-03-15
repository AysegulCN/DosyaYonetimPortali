using System.ComponentModel.DataAnnotations;

namespace DosyaYonetimPortali.API.DTOs
{
    public class UserLoginDto
    {
        [Required(ErrorMessage = "Email alanı zorunludur.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")]
        public string Password { get; set; }
    }
}