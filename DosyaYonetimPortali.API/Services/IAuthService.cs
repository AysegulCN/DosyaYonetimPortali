using DosyaYonetimPortali.API.DTOs;

namespace DosyaYonetimPortali.API.Services
{
    public interface IAuthService
    {
        // Kayıt Ol
        Task<AuthResponseDto> RegisterAsync(UserRegisterDto request);

        // Giriş Yap
        Task<AuthResponseDto> LoginAsync(UserLoginDto request);

        // Sistemi ilk kurduğumuzda Admin, User, Premium rollerini otomatik oluşturmak için
        Task CreateDefaultRolesAsync();
    }
}